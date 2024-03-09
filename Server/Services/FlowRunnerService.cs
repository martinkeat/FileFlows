using System.Text.RegularExpressions;
using FileFlows.Plugin;
using FileFlows.Server.Helpers;
using FileFlows.Server.Hubs;
using FileFlows.Shared.Helpers;

namespace FileFlows.Server.Services;

using FileFlows.Server.Controllers;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Models;
using System;
using System.Threading.Tasks;

/// <summary>
/// A flow runner which is responsible for executing a flow and processing files
/// </summary>
public class FlowRunnerService : IFlowRunnerService
{
    /// <summary>
    /// The running executors
    /// </summary>
    internal readonly static Dictionary<Guid, FlowExecutorInfo> Executors = new();
    private static readonly Queue<Guid> CompletedExecutors = new (50);
    
    /// <summary>
    /// Called when the flow execution has completed
    /// </summary>
    /// <param name="info">The information about the flow execution</param>
    /// <returns>a completed task</returns>
    public async Task<FlowExecutorInfo> Start(FlowExecutorInfo info)
    {
        _ = ServiceLoader.Load<NodeService>().UpdateLastSeen(info.NodeUid);
        
        if (new SettingsService().Get()?.Result?.HideProcessingStartedNotifications != true)
            ClientServiceManager.Instance.SendToast(LogType.Info, "Started processing: " +
                                                                  FileDisplayNameService.GetDisplayName(info.LibraryFile));
        ClientServiceManager.Instance.StartProcessing(info.LibraryFile);
        ClientServiceManager.Instance.UpdateFileStatus();
        
        try
        {
            // try to delete a log file for this library file if one already exists (in case the flow was cancelled and now its being re-run)                
            LibraryFileLogHelper.DeleteLogs(info.LibraryFile.Uid);
        }
        catch (Exception) { }

        if (info.Uid == Guid.Empty)
            throw new Exception("No UID specified for flow execution info");
        info.LastUpdate = DateTime.UtcNow;
        lock (Executors)
        {
            Logger.Instance.ILog($"Adding executor: {info.Uid} = {info.LibraryFile.Name}");
            Executors.Add(info.Uid, info);
        }
        await ClientServiceManager.Instance.UpdateExecutors(Executors);
        Logger.Instance.ILog($"Starting processing on {info.NodeName}: {info.LibraryFile.Name}");
        if (info.LibraryFile != null)
        {
            var lf = info.LibraryFile;
            var service = ServiceLoader.Load<LibraryFileService>();
            await service.ResetFileInfoForProcessing(lf.Uid);
            
            if (lf.ExecutedNodes?.Any() == true)
            {
                lf.ExecutedNodes.Clear();
            }
            // delete the old log file
            LibraryFileLogHelper.DeleteLogs(lf.Uid);

            if (lf.OriginalSize > 0)
                _ = service.UpdateOriginalSize(lf.Uid, lf.OriginalSize);
            if (lf.LibraryUid != null)
            {
                var library = await ServiceLoader.Load<LibraryService>().GetByUidAsync(lf.LibraryUid.Value);
                if (library != null)
                    SystemEvents.TriggerLibraryFileProcessingStarted(lf, library);
            }
        }
        return info;
        
    }
    
    
    /// <summary>
    /// Called when a flow execution starts
    /// </summary>
    /// <param name="info">The information about the flow execution</param>
    /// <param name="log">The full log for the file</param>
    /// <returns>The updated information</returns>
    public async Task Complete(FlowExecutorInfo info, string log)
    {
        _ = ServiceLoader.Load<NodeService>().UpdateLastSeen(info.NodeUid);
        
        Logger.Instance.ILog($"Finishing executor: {info.Uid} = {info.LibraryFile?.Name ?? string.Empty}");
        
        if (string.IsNullOrEmpty(log) == false)
        {
            // this contains the full log file, save it in case a message was lost or received out of order during processing
            try
            {
                _ = LibraryFileLogHelper.SaveLog(info.LibraryFile.Uid, log, saveHtml: true);
            }
            catch (Exception) { }
        }

        lock (Executors)
        {
            CompletedExecutors.Append(info.Uid);
            if (Executors.ContainsKey(info.Uid))
                Executors.Remove(info.Uid);
            else if (string.IsNullOrEmpty(info.LibraryFile?.Name) == false)
            {
                var fileExecutor = Executors.Where(x => 
                    x.Value.LibraryFile.Name == info.LibraryFile.Name)
                    .Select(x => x.Key).FirstOrDefault();
                if (Executors.ContainsKey(fileExecutor)) // could be Guid.Empty if default
                {
                    Executors.Remove(fileExecutor);
                }
                else
                {
                    Logger.Instance?.DLog("Could not remove as not in list of Executors [1]: " + info.Uid + ", file: " + info.LibraryFile.Name);
                }
            }
            else
            {   
                Logger.Instance?.DLog("Could not remove as not in list of Executors [2]: " + info.Uid + ", file: " + info.LibraryFile.Name);
            }
        }
        ClientServiceManager.Instance.UpdateExecutors(Executors);
        ClientServiceManager.Instance.UpdateFileStatus();

        if (info.LibraryFile != null)
        {
            ClientServiceManager.Instance.FinishProcessing(info.LibraryFile);
            var lfService= ServiceLoader.Load<LibraryFileService>();
            var libfile = await lfService.Get(info.LibraryFile.Uid);
            if (libfile != null)
            {
                libfile.OutputPath = info.LibraryFile.OutputPath?.EmptyAsNull() ?? libfile.OutputPath;
                Logger.Instance.ILog(
                    $"Recording final size for '{info.LibraryFile.FinalSize}' for '{info.LibraryFile.Name}' status: {info.LibraryFile.Status}");
                if (info.LibraryFile.FinalSize > 0)
                    libfile.FinalSize = info.LibraryFile.FinalSize;

                if (info.WorkingFile == libfile.Name)
                {
                    var file = new FileInfo(info.WorkingFile);
                    if (file.Exists)
                    {
                        // if file replaced original update the creation time to match
                        if (libfile.CreationTime != file.CreationTime)
                            libfile.CreationTime = file.CreationTime;
                        if (libfile.LastWriteTime != file.LastWriteTime)
                            libfile.LastWriteTime = file.LastWriteTime;
                    }
                }


                libfile.NoLongerExistsAfterProcessing = new FileInfo(libfile.Name).Exists == false;
                if (info.LibraryFile.FinalSize > 0)
                    libfile.FinalSize = info.LibraryFile.FinalSize;
                libfile.OutputPath = info.LibraryFile.OutputPath;

                if (string.IsNullOrWhiteSpace(libfile.OutputPath) == false)
                {
                    if (libfile.Name.StartsWith("/"))
                    {
                        // start file was a linux file
                        // check if libfile.OutputPath is using \ instead of / for linux filenames
                        if (libfile.OutputPath.StartsWith(@"\\") == false)
                        {
                            libfile.OutputPath = libfile.OutputPath.Replace(@"\", "/");
                        }
                    }
                    else if (Regex.IsMatch(libfile.Name, "^[a-zA-Z]:") || libfile.Name.StartsWith(@"\\")
                                                                       || libfile.Name.StartsWith(@"//"))
                    {
                        // Windows-style path in Name or UNC path
                        libfile.OutputPath = libfile.OutputPath.Replace("/", @"\");
                    }
                }

                libfile.Fingerprint = info.LibraryFile.Fingerprint;
                libfile.FinalFingerprint = info.LibraryFile.FinalFingerprint;
                libfile.ExecutedNodes = info.LibraryFile.ExecutedNodes ?? new List<ExecutedNode>();
                Logger.Instance.DLog("WorkerController.FinishWork: Executed flow elements: " +
                                     string.Join(", ", libfile.ExecutedNodes.Select(x => x.NodeUid)));
                
                if (info.LibraryFile.OriginalMetadata != null)
                    libfile.OriginalMetadata = info.LibraryFile.OriginalMetadata;
                if (info.LibraryFile.FinalMetadata != null)
                    libfile.FinalMetadata = info.LibraryFile.FinalMetadata;
                libfile.Status = info.LibraryFile.Status;
                if (info.LibraryFile.ProcessingStarted > new DateTime(2020, 1, 1))
                    libfile.ProcessingStarted = info.LibraryFile.ProcessingStarted;
                if (info.LibraryFile.ProcessingEnded > new DateTime(2020, 1, 1))
                    libfile.ProcessingEnded = info.LibraryFile.ProcessingEnded;
                if (libfile.ProcessingEnded < new DateTime(2020, 1, 1))
                    libfile.ProcessingEnded = DateTime.UtcNow; // this avoid a "2022 years ago" issue
                if(libfile.Flow == null)
                    libfile.Flow = info.LibraryFile.Flow;
                await lfService.Update(libfile);
                var library = await ServiceLoader.Load<LibraryService>().GetByUidAsync(libfile.Library.Uid);
                if (libfile.Status == FileStatus.ProcessingFailed)
                {
                    SystemEvents.TriggerLibraryFileProcessedFailed(libfile, library);
                    
                    if(new SettingsService().Get()?.Result?.HideProcessingFinishedNotifications != true)
                        ClientServiceManager.Instance.SendToast(LogType.Error, "Failed processing: " + FileDisplayNameService.GetDisplayName(info.LibraryFile));
                }
                else
                {
                    SystemEvents.TriggerLibraryFileProcessedSuccess(libfile, library);
                    if(new SettingsService().Get()?.Result?.HideProcessingFinishedNotifications != true)
                        ClientServiceManager.Instance.SendToast(LogType.Info, "Finished processing: " + FileDisplayNameService.GetDisplayName(info.LibraryFile));
                }

                SystemEvents.TriggerLibraryFileProcessed(libfile, library);
            }
        }
    }


    /// <summary>
    /// Called to update the status of the flow execution on the server
    /// </summary>
    /// <param name="info">The information about the flow execution</param>
    /// <returns>a completed task</returns>
    public async Task Update(FlowExecutorInfo info)
    {
        await new WorkerController(null).UpdateWork(info);
    }

    /// <summary>
    /// Clear all workers from a node.  Intended for clean up in case a node restarts.  
    /// This is called when a node first starts.
    /// </summary>
    /// <param name="nodeUid">The UID of the processing node</param>
    /// <returns>an awaited task</returns>
    public async Task Clear(Guid nodeUid)
    {
        lock (Executors)
        {
            Logger.Instance.ILog("Clearing workers");
            var toRemove = Executors.Where(x => x.Value.NodeUid == nodeUid).ToArray();
            foreach (var item in toRemove)
                Executors.Remove(item.Key);
        }

        await ServiceLoader.Load<LibraryFileService>().ResetProcessingStatus(nodeUid);
        await ClientServiceManager.Instance.UpdateExecutors(Executors);
    }
    
    /// <summary>
    /// Abort work by library file
    /// </summary>
    /// <param name="uid">The UID of the library file to abort</param>
    /// <returns>An awaited task</returns>
    public async Task AbortByFile(Guid uid)
    {
        Guid executorId;
        lock (Executors)
        {
            executorId = Executors.Where(x => x.Value?.LibraryFile?.Uid == uid).Select(x => x.Key).FirstOrDefault();
            if (executorId == Guid.Empty)
                executorId = Executors.Where(x => x.Value == null).Select(x => x.Key).FirstOrDefault();
        }
        if (executorId == Guid.Empty || Executors.TryGetValue(executorId, out FlowExecutorInfo? info) == false || info == null)
        {
            if(executorId == Guid.Empty)
            {
                Logger.Instance?.WLog("Failed to locate Flow executor with library file: " + uid);
                foreach (var executor in Executors)
                    Logger.Instance?.WLog(
                        $"Flow Executor: {executor.Key} = {executor.Value?.LibraryFile?.Uid} = {executor.Value?.LibraryFile?.Name}");
            }
            // may not have an executor, just update the status
            var libfileController = new LibraryFileController();
            var libFile = await libfileController.Get(uid);
            if (libFile is { Status: FileStatus.Processing })
            {
                libFile.Status = FileStatus.ProcessingFailed;
                await libfileController.Update(libFile);
            }
            if(executorId == Guid.Empty)
                return;
        }
        await Abort(executorId, uid);
        ClientServiceManager.Instance.UpdateExecutors(Executors);
    }
    
    
    /// <summary>
    /// Abort work 
    /// </summary>
    /// <param name="uid">The UID of the executor</param>
    /// <param name="libraryFileUid">the UID of the library file</param>
    /// <returns>an awaited task</returns>
    public async Task Abort(Guid uid, Guid libraryFileUid)
    {
        try
        {
            FlowExecutorInfo? flowinfo = null;
            Executors?.TryGetValue(uid, out flowinfo);
            if(flowinfo == null)
            {
                flowinfo = Executors.Values.Where(x => x != null && (x.LibraryFile?.Uid == uid || x.Uid == uid || x.NodeUid == uid)).FirstOrDefault();
                if(flowinfo == null)
                    flowinfo = Executors.Values.Where(x => x == null).FirstOrDefault();
            }
            if(flowinfo == null)
            {
                Logger.Instance?.WLog("Unable to find executor matching: " + uid);
            }


            if (flowinfo?.LibraryFile != null)
            {

                Logger.Instance?.DLog("Getting library file to update processing status");
                var libController = new LibraryFileController();
                var libfile = await libController.Get(flowinfo.LibraryFile.Uid);
                if (libfile != null)
                {
                    Logger.Instance?.DLog("Current library file processing status: " + libfile.Status);
                    if (libfile.Status == FileStatus.Processing)
                    {
                        libfile.Status = FileStatus.ProcessingFailed;
                        Logger.Instance?.ILog("Library file setting status to failed: " + libfile.Status + " => " +
                                              libfile.RelativePath);
                        await libController.Update(libfile);
                    }
                    else
                    {
                        Logger.Instance?.ILog("Library file status doesnt need changing: " + libfile.Status + " => " +
                                              libfile.RelativePath);
                    }
                }
            }
            
            await Task.Delay(6_000);
            Logger.Instance?.DLog("Removing from list of executors: " + uid);
            lock (Executors)
            {
                if (Executors.TryGetValue(uid, out FlowExecutorInfo? info))
                {
                    if (info == null || info.LastUpdate < DateTime.UtcNow.AddMinutes(-1))
                    {
                        // its gone quiet, kill it
                        Executors.Remove(uid);
                    }
                }
            }
            await ClientServiceManager.Instance.UpdateExecutors(Executors);
            Logger.Instance?.DLog("Abortion complete: " + uid);
        }
        catch (Exception ex)
        {
            Logger.Instance.WLog("Error aborting flow: " + ex.Message + Environment.NewLine + ex.StackTrace);
        }
    }
    /// <summary>
    /// Receives a hello from the flow runner, indicating its still alive and executing
    /// </summary>
    /// <param name="runnerUid">the UID of the flow runner</param>
    /// <param name="info">the flow execution info</param>
    internal bool Hello(Guid runnerUid, FlowExecutorInfo info)
    {
        lock (Executors)
        {
            if (Executors.TryGetValue(runnerUid, out var executorInfo) == false)
            {
                Logger.Instance?.WLog("Unable to find executor from helloer: " + runnerUid);
                foreach (var executor in Executors.Values)
                    Logger.Instance?.WLog("Executor: " + executor.Uid + " = " + executor.LibraryFile.Name);
                
                // unknown executor, the server may have restarted
                if(Executors.TryAdd(runnerUid, info) == false)
                    return false; 
            }
            ClientServiceManager.Instance.UpdateExecutors(Executors);

            if(executorInfo != null)
                executorInfo.LastUpdate = DateTime.UtcNow;

            if (info.LibraryFile != null)
            {
                var service = ServiceLoader.Load<LibraryFileService>();
                var current = service.GetFileStatus(info.LibraryFile.Uid).Result;
                if (current != null && current == FileStatus.Unprocessed)
                {
                    // can happen if server was restarted
                    service.UpdateWork(info.LibraryFile).Wait();
                }
            }
            return true;
        }
    }

    /// <summary>
    /// Update work, tells the server about updated work on a flow runner
    /// </summary>
    /// <param name="info">The updated work information</param>
    public async Task UpdateWork(FlowExecutorInfo info)
    {
        _ = ServiceLoader.Load<NodeService>().UpdateLastSeen(info.NodeUid);
        
        if (info.LibraryFile != null)
        {
            var libFileService = ServiceLoader.Load<LibraryFileService>();
            var libFile = await libFileService.Get(info.LibraryFile.Uid);
            var originalStatus = libFile.Status;
            if (info.LibraryFile.Status == FileStatus.Processing && libFile.OriginalMetadata?.Any() != true &&
                info.LibraryFile.OriginalMetadata?.Any() == true)
            {
                // we have the original metadata, update
                libFile.OriginalMetadata = info.LibraryFile.OriginalMetadata;
            }
            if (info.LibraryFile != libFile)
                ObjectHelper.CopyProperties(info.LibraryFile, libFile,
                    nameof(LibraryFile.OriginalSize),
                    nameof(LibraryFile.Fingerprint),
                    nameof(LibraryFile.Library),
                    nameof(LibraryFile.Duplicate),
                    nameof(LibraryFile.Node), // set when runner grabs the file
                    nameof(LibraryFile.CreationTime),
                    nameof(LibraryFile.DuplicateName),
                    nameof(LibraryFile.HoldUntil),
                    nameof(LibraryFile.IsDirectory),
                    nameof(LibraryFile.LibraryName),
                    nameof(LibraryFile.LibraryUid),
                    nameof(LibraryFile.NodeName),
                    nameof(LibraryFile.NodeUid),
                    nameof(LibraryFile.ProcessOnNodeUid),
                    nameof(LibraryFile.OriginalMetadata),
                    nameof(LibraryFile.RelativePath),
                    nameof(LibraryFile.Name),
                    nameof(LibraryFile.Uid));
            
            if (libFile.Status != FileStatus.Processing)
            {
                Logger.Instance.DLog(
                    $"Updating non-processing library file [{info.LibraryFile.Status}]: {info.LibraryFile.Name}");
            }

            if (originalStatus == FileStatus.Unprocessed)
            {
                // this can happen if the server is restarted but the node is still processing, update the status
                await libFileService.Update(libFile);
            }
            if (originalStatus == FileStatus.ProcessingFailed || info.LibraryFile.Status == FileStatus.Processed)
            {
                lock (Executors)
                {
                    CompletedExecutors.Append(info.Uid);
                    Executors.Remove(info.Uid);
                    _ = ClientServiceManager.Instance.UpdateExecutors(Executors);
                    return;
                }
            }
        }

        info.LastUpdate = DateTime.UtcNow;
        lock (Executors)
        {
            if (CompletedExecutors.Contains(info.Uid))
                return; // this call was delayed for some reason

            if (Executors.ContainsKey(info.Uid))
                Executors[info.Uid] = info;
            //else // this is causing a finished executors to stick around.
            //    Executors.Add(info.Uid, info);
        }
        await ClientServiceManager.Instance.UpdateExecutors(Executors);
    }


    /// <summary>
    /// Gets if a library file is executing
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    /// <returns>true if running, otherwise false</returns>
    internal bool IsLibraryFileRunning(Guid uid)
        => Executors?.Any(x => x.Value.LibraryFile?.Uid == uid) == true;


    /// <summary>
    /// Aborts any runners that have stopped communicating
    /// </summary>
    internal void AbortDisconnectedRunners()
    {
        FlowExecutorInfo[] executors;
        lock (Executors)
        {
            executors = Executors?.Select(x => x.Value)?.ToArray() ?? new FlowExecutorInfo[] { };
        }

        foreach (var executor in executors ?? new FlowExecutorInfo[] {})
        {
            if (executor != null && executor.LastUpdate < DateTime.UtcNow.AddSeconds(-60))
            {
                Logger.Instance?.ILog($"Aborting disconnected runner[{executor.NodeName}]: {executor.LibraryFile.Name}");
                Abort(executor.Uid, executor.LibraryFile.Uid).Wait();
            }
        }
    }

    /// <summary>
    /// Get UIDs of executing library files
    /// </summary>
    /// <returns>UIDs of executing library files</returns>
    internal static Guid[] ExecutingLibraryFiles()
        => Executors?.Select(x => x.Value?.LibraryFile?.Uid)?
               .Where(x => x != null)?.Select(x => x!.Value)?.ToArray() ??
           new Guid[] { };
}