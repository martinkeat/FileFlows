using System.Text.RegularExpressions;
using FileFlows.DataLayer;
using FileFlows.Plugin;
using FileFlows.Server.Helpers;
using FileFlows.Server.Hubs;

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

    /// <summary>
    /// The semaphore that locks the executors list
    /// </summary>
    private static FairSemaphore executorsSempahore = new(1);

    /// <inheritdoc />
    public async Task<int> GetFileCheckInterval()
    {
        var settings = await ServiceLoader.Load<SettingsService>().Get();
        return settings.ProcessFileCheckInterval;
    }

    /// <inheritdoc />
    public Task<bool> IsLicensed()
        => Task.FromResult(LicenseHelper.IsLicensed());

    /// <summary>
    /// Called when the flow execution has completed
    /// </summary>
    /// <param name="info">The information about the flow execution</param>
    /// <returns>a completed task</returns>
    public async Task<FlowExecutorInfo> Start(FlowExecutorInfo info)
    {
        await ServiceLoader.Load<NodeService>().UpdateLastSeen(info.NodeUid);

        await ServiceLoader.Load<StatisticService>().RecordFileStarted();

        if (info.Uid == Guid.Empty)
            throw new Exception("No UID specified for flow execution info");

        if (new SettingsService().Get()?.Result?.HideProcessingStartedNotifications != true)
            ClientServiceManager.Instance.SendToast(LogType.Info, "Started processing: " +
                                                                  ServiceLoader.Load<FileDisplayNameService>().GetDisplayName(
                                                                      info.LibraryFile));
        ClientServiceManager.Instance.StartProcessing(info.LibraryFile);
        await ClientServiceManager.Instance.UpdateFileStatus();

        try
        {
            // try to delete a log file for this library file if one already exists (in case the flow was cancelled and now its being re-run)                
            LibraryFileLogHelper.DeleteLogs(info.LibraryFile.Uid);
        }
        catch (Exception)
        {
        }

        info.LastUpdate = DateTime.UtcNow;
        Logger.Instance.ILog($"Adding executor: {info.Uid} = {info.LibraryFile.Name}");
        await executorsSempahore.WaitAsync();
        try
        {
            Executors[info.Uid] = info;
        }
        finally
        {
            executorsSempahore.Release();
        }

        await ClientServiceManager.Instance.UpdateExecutors(Executors);

        Logger.Instance.ILog($"Starting processing on {info.NodeName}: {info.LibraryFile.Name}");
        if (info.LibraryFile != null)
        {
            var service = ServiceLoader.Load<LibraryFileService>();
            var lf = info.LibraryFile;
            if (lf.LibraryUid != null)
            {
                var library = await ServiceLoader.Load<LibraryService>().GetByUidAsync(lf.LibraryUid.Value);
                if (library != null)
                {
                    SystemEvents.TriggerLibraryFileProcessingStarted(lf, library);
                    await service.ResetFileInfoForProcessing(info.LibraryFile.Uid, library.Flow?.Uid,
                        library.Flow?.Name);
                }
            }
            else
            {
                await service.ResetFileInfoForProcessing(info.LibraryFile.Uid, null, string.Empty);
            }
        }

        return info;
    }


    /// <summary>
    /// Called to update the status of the flow execution on the server
    /// </summary>
    /// <param name="info">The information about the flow execution</param>
    /// <returns>a completed task</returns>
    public async Task Update(FlowExecutorInfo info)
    {
        await executorsSempahore.WaitAsync();
        try
        {
            info.LastUpdate = DateTime.UtcNow;
            Executors[info.Uid] = info;
        }
        finally
        {
            executorsSempahore.Release();
        }
        await ClientServiceManager.Instance.UpdateExecutors(Executors);
    }

    
    /// <summary>
    /// Called when a flow execution starts
    /// </summary>
    /// <param name="info">The information about the flow execution</param>
    /// <returns>The updated information</returns>
    public async Task Finish(FlowExecutorInfo info)
    {
        // await ServiceLoader.Load<NodeService>().UpdateLastSeen(info.NodeUid);
        
        Logger.Instance.ILog($"Finishing executor: {info.Uid} = {info.LibraryFile?.Name ?? string.Empty}");
        
        await executorsSempahore.WaitAsync();
        try
        {
            Executors.Remove(info.Uid);
        }
        finally
        {
            executorsSempahore.Release();
        }
        await ClientServiceManager.Instance.UpdateExecutors(Executors);

        if (info.LibraryFile != null)
        {
            var updated = info.LibraryFile;
            ClientServiceManager.Instance.FinishProcessing(updated);
            var lfService= ServiceLoader.Load<LibraryFileService>();
            var existing = await lfService.Get(updated.Uid);
            if (existing == null)
                return;
        
            existing.OutputPath = updated.OutputPath?.EmptyAsNull() ?? existing.OutputPath;
            Logger.Instance.ILog(
                $"Recording final size for '{updated.FinalSize}' for '{updated.Name}' status: {updated.Status}");
            if (updated.FinalSize > 0)
                existing.FinalSize = updated.FinalSize;
            if (updated.OriginalSize > 0)
                existing.OriginalSize = updated.OriginalSize;
            
            if (info.WorkingFile == existing.Name)
            {
                var file = new FileInfo(info.WorkingFile);
                if (file.Exists)
                {
                    // if file replaced original update the creation time to match
                    if (existing.CreationTime != file.CreationTimeUtc)
                        existing.CreationTime = file.CreationTimeUtc;
                    if (existing.LastWriteTime != file.LastWriteTimeUtc)
                        existing.LastWriteTime = file.LastWriteTimeUtc;
                }
            }


            existing.NoLongerExistsAfterProcessing = new FileInfo(existing.Name).Exists == false;
            if (updated.FinalSize > 0)
                existing.FinalSize = updated.FinalSize;
            existing.OutputPath = updated.OutputPath;

            if (string.IsNullOrWhiteSpace(existing.OutputPath) == false)
            {
                if (existing.Name.StartsWith("/"))
                {
                    // start file was a linux file
                    // check if libfile.OutputPath is using \ instead of / for linux filenames
                    if (existing.OutputPath.StartsWith(@"\\") == false)
                    {
                        existing.OutputPath = existing.OutputPath.Replace(@"\", "/");
                    }
                }
                else if (Regex.IsMatch(existing.Name, "^[a-zA-Z]:") || existing.Name.StartsWith(@"\\")
                                                                   || existing.Name.StartsWith(@"//"))
                {
                    // Windows-style path in Name or UNC path
                    existing.OutputPath = existing.OutputPath.Replace("/", @"\");
                }
            }

            existing.ProcessingEnded = updated.ProcessingEnded;
            existing.Fingerprint = updated.Fingerprint;
            existing.FinalFingerprint = updated.FinalFingerprint;
            existing.ExecutedNodes = updated.ExecutedNodes ?? new List<ExecutedNode>();
            Logger.Instance.DLog("FinishWork: Executed flow elements: " +
                                 string.Join(", ", existing.ExecutedNodes.Select(x => x.NodeUid)));
            
            if (updated.OriginalMetadata != null)
                existing.OriginalMetadata = updated.OriginalMetadata;
            if (updated.FinalMetadata != null)
                existing.FinalMetadata = updated.FinalMetadata;
            existing.Status = updated.Status;
            if (updated.ProcessingStarted > new DateTime(2020, 1, 1))
                existing.ProcessingStarted = updated.ProcessingStarted;
            if (updated.ProcessingEnded > new DateTime(2020, 1, 1))
                existing.ProcessingEnded = updated.ProcessingEnded;
            if (existing.ProcessingEnded < new DateTime(2020, 1, 1))
                existing.ProcessingEnded = DateTime.UtcNow; // this avoid a "2022 years ago" issue
            if(existing.ProcessingEnded > DateTime.UtcNow)
                existing.ProcessingEnded = DateTime.UtcNow;
            
            if (updated.Flow?.Uid != null && updated.Flow.Uid != Guid.Empty &&
                updated.Flow.Uid != existing.Flow?.Uid)
                existing.Flow = updated.Flow;
            else if(string.IsNullOrWhiteSpace(existing.Flow?.Name))
                existing.Flow = updated.Flow;
            
            await lfService.Update(existing);
            var library = await ServiceLoader.Load<LibraryService>().GetByUidAsync(existing.Library.Uid);
            
            if (existing.Status == FileStatus.ProcessingFailed)
            {
                SystemEvents.TriggerLibraryFileProcessedFailed(existing, library);
                
                if(new SettingsService().Get()?.Result?.HideProcessingFinishedNotifications != true)
                    ClientServiceManager.Instance.SendToast(LogType.Error, "Failed processing: " + ServiceLoader.Load<FileDisplayNameService>().GetDisplayName(info.LibraryFile));
            }
            else
            {
                SystemEvents.TriggerLibraryFileProcessedSuccess(existing, library);
                if(new SettingsService().Get()?.Result?.HideProcessingFinishedNotifications != true)
                    ClientServiceManager.Instance.SendToast(LogType.Info, "Finished processing: " + ServiceLoader.Load<FileDisplayNameService>().GetDisplayName(info.LibraryFile));
            }

            SystemEvents.TriggerLibraryFileProcessed(existing, library);

            await ServiceLoader.Load<StatisticService>()
                .RecordStorageSaved(library.Name, existing.OriginalSize, existing.FinalSize);
        }
        
        await ClientServiceManager.Instance.UpdateFileStatus();
    }

    /// <summary>
    /// Clear all workers from a node.  Intended for clean up in case a node restarts.  
    /// This is called when a node first starts.
    /// </summary>
    /// <param name="nodeUid">The UID of the processing node</param>
    /// <returns>an awaited task</returns>
    public async Task Clear(Guid nodeUid)
    {
        Logger.Instance.ILog("Clearing workers");
        await executorsSempahore.WaitAsync();
        try
        {
            var toRemove = Executors.Where(x => x.Value.NodeUid == nodeUid).ToArray();
            foreach (var item in toRemove)
                Executors.Remove(item.Key);
        }
        finally
        {
            executorsSempahore.Release();
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
        await executorsSempahore.WaitAsync();
        try
        {
            executorId = Executors.Where(x => x.Value?.LibraryFile?.Uid == uid).Select(x => x.Key).FirstOrDefault();
            if (executorId == Guid.Empty)
                executorId = Executors.Where(x => x.Value == null).Select(x => x.Key).FirstOrDefault();
        }
        finally
        {
            executorsSempahore.Release();
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
                
                await ServiceLoader.Load<LibraryFileService>().Update(libFile);
                //await libfileController.Update(libFile);
            }
            if(executorId == Guid.Empty)
                return;
        }
        await Abort(executorId, uid);
        await ClientServiceManager.Instance.UpdateExecutors(Executors);
    }

    /// <summary>
    /// Tries to find the runner UID for a given library file
    /// </summary>
    /// <param name="libraryFileUid">the UID of the library file</param>
    /// <returns>The Runners UID if found, otherwise a failure result</returns>
    public async Task<Result<Guid>> FindRunner(Guid libraryFileUid)
    {
        await executorsSempahore.WaitAsync();
        try
        {
            foreach (Guid key in Executors.Keys)
            {
                if (Executors[key].LibraryFile?.Uid == libraryFileUid)
                {
                    return key;
                }
            }

            return Result<Guid>.Fail("Not found");
        }
        finally
        {
            executorsSempahore.Release();
        }
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
                var service = ServiceLoader.Load<LibraryFileService>();
                var libfile = await service.Get(flowinfo.LibraryFile.Uid);
                if (libfile != null)
                {
                    Logger.Instance?.DLog("Current library file processing status: " + libfile.Status);
                    if (libfile.Status == FileStatus.Processing)
                    {
                        libfile.Status = FileStatus.ProcessingFailed;
                        Logger.Instance?.ILog("Library file setting status to failed: " + libfile.Status + " => " +
                                              libfile.RelativePath);
                        await service.Update(libfile);
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
            await executorsSempahore.WaitAsync();
            try
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
            finally
            {
                executorsSempahore.Release();
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
    internal async Task<bool> Hello(Guid runnerUid, FlowExecutorInfo info)
    {
        await executorsSempahore.WaitAsync();
        try
        {
            Executors.TryAdd(runnerUid, info);
            Executors[runnerUid].LastUpdate = DateTime.UtcNow;
        }
        finally
        {
            executorsSempahore.Release();
        }
        _ = ClientServiceManager.Instance.UpdateExecutors(Executors);
        return true;
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
    internal async Task AbortDisconnectedRunners()
    {
        FlowExecutorInfo[] executors;
        await executorsSempahore.WaitAsync();
        try
        {
            executors = Executors?.Select(x => x.Value)?.ToArray() ?? new FlowExecutorInfo[] { };
        }
        finally
        {
            executorsSempahore.Release();
        }

        foreach (var executor in executors ?? new FlowExecutorInfo[] {})
        {
            if (executor != null && executor.LastUpdate < DateTime.UtcNow.AddSeconds(-120))
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
    internal static async Task<List<Guid>> ExecutingLibraryFiles()
    {
        await executorsSempahore.WaitAsync();
        try
        {
            return Executors?.Select(x => x.Value?.LibraryFile?.Uid)?
                       .Where(x => x != null)?.Select(x => x!.Value)?.ToList() ??
                   new List<Guid>();
        }
        finally
        {
            executorsSempahore.Release();
        }
    }

    /// <summary>
    /// Tries and gets a file from the running executor list
    /// </summary>
    /// <param name="libraryFileUid">the UID of the library file to get</param>
    /// <returns>true if it is currently executing, otherwise false</returns>
    public async Task<LibraryFile?> TryGetFile(Guid libraryFileUid)
    {
        await executorsSempahore.WaitAsync();
        try
        {
            return Executors
                .Where(x => x.Value?.LibraryFile?.Uid == libraryFileUid)
                .Select(x => x.Value?.LibraryFile)
                .FirstOrDefault();
        }
        finally
        {
            executorsSempahore.Release();
        }
    }
}