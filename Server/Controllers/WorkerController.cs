using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using FileFlows.Shared.Models;
using FileFlows.Server.Helpers;
using FileFlows.Server.Hubs;
using FileFlows.Server.Services;
using FileFlows.Shared.Helpers;

namespace FileFlows.Server.Controllers;


/// <summary>
/// This controller will be responsible for knowing about the workers and the nodes
/// When a worker starts, this needs to be informed, when its finished, it needs to be told too
/// This needs to be able to kill a worker running on any node
/// </summary>
[Route("/api/worker")]
public class WorkerController : Controller
{
    private readonly static Dictionary<Guid, FlowExecutorInfo> Executors = new();
    private readonly Queue<Guid> CompletedExecutors = new Queue<Guid>(50);

    private static CacheStore LibraryFileCacheStore = new();
    private IHubContext<FlowHub> Context;

    public WorkerController(IHubContext<FlowHub> context)
    {
        this.Context = context;
    }

    /// <summary>
    /// Start work, tells the server work has started on a flow runner
    /// </summary>
    /// <param name="info">The info about the work starting</param>
    /// <returns>the updated info</returns>
    [HttpPost("work/start")]
    public FlowExecutorInfo StartWork([FromBody] FlowExecutorInfo info)
    {
        _ = new NodeController().UpdateLastSeen(info.NodeUid);
        
        try
        {
            // try to delete a log file for this library file if one already exists (in case the flow was cancelled and now its being re-run)                
            LibraryFileLogHelper.DeleteLogs(info.LibraryFile.Uid);
        }
        catch (Exception) { }

        info.LibraryFile?.ExecutedNodes?.Clear();
        if (info.Uid == Guid.Empty)
            throw new Exception("No UID specified for flow execution info");
        info.LastUpdate = DateTime.Now;
        lock (Executors)
        {
            Logger.Instance.ILog($"Adding executor: {info.Uid} = {info.LibraryFile.Name}");
            Executors.Add(info.Uid, info);
        }
        Logger.Instance.ILog($"Starting processing on {info.NodeName}: {info.LibraryFile.Name}");
        if (info.LibraryFile != null)
        {
            var lf = info.LibraryFile;
            if (lf.OriginalSize > 0)
                _ = new LibraryFileService().UpdateOriginalSize(lf.Uid, lf.OriginalSize);
            if (lf.LibraryUid != null)
            {
                var library = new LibraryService().GetByUid(lf.LibraryUid.Value);
                if (library != null)
                    SystemEvents.TriggerLibraryFileProcessingStarted(lf, library);
            }
        }
        return info;
    }

    /// <summary>
    /// Finish work, tells the server work has finished on a flow runner
    /// </summary>
    /// <param name="info">Info about the finished work</param>
    [HttpPost("work/finish")]
    public async void FinishWork([FromBody] FlowExecutorInfo info)
    {
        _ = new NodeController().UpdateLastSeen(info.NodeUid);
        
        Logger.Instance.ILog($"Finishing executor: {info.Uid} = {info.LibraryFile?.Name ?? string.Empty}");
        
        if (string.IsNullOrEmpty(info.Log) == false)
        {
            // this contains the full log file, save it in case a message was lost or received out of order during processing
            try
            {
                _ = LibraryFileLogHelper.SaveLog(info.LibraryFile.Uid, info.Log, saveHtml: true);
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

        if (info.LibraryFile != null)
        {
            var lfService= new LibraryFileService();
            var libfile = lfService.GetByUid(info.LibraryFile.Uid);
            if (libfile != null)
            {
                libfile.OutputPath = info.LibraryFile.OutputPath?.EmptyAsNull() ?? libfile.OutputPath;
                Logger.Instance.ILog($"Recording final size for '{info.LibraryFile.FinalSize}' for '{info.LibraryFile.Name}' status: {info.LibraryFile.Status}");
                if(info.LibraryFile.FinalSize > 0)
                    libfile.FinalSize = info.LibraryFile.FinalSize;

                if (info.WorkingFile == libfile.Name)
                {
                    var file = new FileInfo(info.WorkingFile);
                    if (file.Exists)
                    {
                        // if file replaced original update the creation time to match
                        if (libfile.CreationTime != file.CreationTime)
                            libfile.CreationTime = file.CreationTime;
                        if(libfile.LastWriteTime != file.LastWriteTime)
                            libfile.LastWriteTime = file.LastWriteTime;
                    }
                }


                libfile.NoLongerExistsAfterProcessing = new FileInfo(libfile.Name).Exists == false;
                if(info.LibraryFile.FinalSize > 0)
                    libfile.FinalSize = info.LibraryFile.FinalSize;
                libfile.OutputPath = info.LibraryFile.OutputPath;
                libfile.Fingerprint = info.LibraryFile.Fingerprint;
                libfile.FinalFingerprint = info.LibraryFile.FinalFingerprint;
                libfile.ExecutedNodes = info.LibraryFile.ExecutedNodes ?? new List<ExecutedNode>();
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
                    libfile.ProcessingEnded = DateTime.Now; // this avoid a "2022 years ago" issue
                await lfService.Update(libfile);
                var library = new LibraryService().GetByUid(libfile.Library.Uid);
                if (libfile.Status == FileStatus.ProcessingFailed)
                    SystemEvents.TriggerLibraryFileProcessedFailed(libfile, library);
                else
                    SystemEvents.TriggerLibraryFileProcessedSuccess(libfile, library);
                SystemEvents.TriggerLibraryFileProcessed(libfile, library);
            }
        }
    }

    
    /// <summary>
    /// Update work, tells the server about updated work on a flow runner
    /// </summary>
    /// <param name="info">The updated work information</param>
    [HttpPost("work/update")]
    public async Task UpdateWork([FromBody] FlowExecutorInfo info)
    {
        _ = new NodeController().UpdateLastSeen(info.NodeUid);

        
        if (info.LibraryFile != null)
        {
            var libfileService = new LibraryFileService();
            var libFile = libfileService.GetByUid(info.LibraryFile.Uid);
            
            if (libFile.Status != FileStatus.Processing)
            {
                Logger.Instance.DLog(
                    $"Updating non-processing library file [{info.LibraryFile.Status}]: {info.LibraryFile.Name}");
            }

            if (libFile.Status == FileStatus.Unprocessed)
            {
                libFile.Status = info.LibraryFile.Status;
                // this can happen if the server is restarted but the node is still processing, update the status
                await libfileService.Update(libFile);
            }
            // now using memory cache, we dont need to save this info constantly, can save it for the end
            // else if (await LibraryFileHasChanged(libFile))
            // {
            //     await libfileService.UpdateWork(libFile);
            // }

            if (libFile.Status == FileStatus.ProcessingFailed || info.LibraryFile.Status == FileStatus.Processed)
            {
                lock (Executors)
                {
                    CompletedExecutors.Append(info.Uid);
                    if (Executors.ContainsKey(info.Uid))
                        Executors.Remove(info.Uid);
                    return;
                }
            }
        }

        info.LastUpdate = DateTime.Now;
        lock (Executors)
        {
            if (CompletedExecutors.Contains(info.Uid))
                return; // this call was delayed for some reason

            if (Executors.ContainsKey(info.Uid))
                Executors[info.Uid] = info;
            //else // this is causing a finished executors to stick around.
            //    Executors.Add(info.Uid, info);
        }
    }

    private bool LibraryFileHasChanged(LibraryFile file)
    {
        var cached = LibraryFileCacheStore.Get<LibraryFileRecord>(file.Uid);
        LibraryFileCacheStore.Store(file.Uid, new LibraryFileRecord
        {
            Status = file.Status,
            ExecutedNodes = file.ExecutedNodes?.Count ?? 0,
            WorkerUid = file.WorkerUid,
            NodeUid = file.NodeUid,
            FinalSize = file.FinalSize,
            ProcessingStarted = file.ProcessingStarted,
            ProcessingEnded = file.ProcessingEnded
        });
        if (cached == null)
            return true;
        var dbStatus = new LibraryFileService().GetFileStatus(file.Uid);
        if (dbStatus != file.Status)
            return true;
        if (file.Status != cached.Status)
            return true;
        if (file.ExecutedNodes?.Count != cached.ExecutedNodes)
            return true;
        if(file.WorkerUid != cached.WorkerUid)
            return true;
        if(file.Node?.Uid != cached.NodeUid)
            return true;
        if(file.FinalSize != cached.FinalSize)
            return true;
        if(file.ProcessingStarted != cached.ProcessingStarted)
            return true;
        if(file.ProcessingEnded != cached.ProcessingEnded)
            return true;
        return false;
    }

    class LibraryFileRecord
    {
        public FileStatus Status { get; set; }
        public int ExecutedNodes { get; set; }
        public Guid? WorkerUid { get; set; }
        public Guid? NodeUid { get; set; }
        public long FinalSize { get; set; }
        public DateTime ProcessingStarted { get; set; }
        public DateTime ProcessingEnded { get; set; }
    }

    /// <summary>
    /// Clear all workers from a node.  Intended for clean up in case a node restarts.  
    /// This is called when a node first starts.
    /// </summary>
    /// <param name="nodeUid">The UID of the processing node</param>
    /// <returns>an awaited task</returns>
    [HttpPost("clear/{nodeUid}")]
    public async Task Clear([FromRoute] Guid nodeUid)
    {
        lock (Executors)
        {
            Logger.Instance.ILog("Clearing workers");
            var toRemove = Executors.Where(x => x.Value.NodeUid == nodeUid).ToArray();
            foreach (var item in toRemove)
                Executors.Remove(item.Key);
        }
        await new LibraryFileService().ResetProcessingStatus(nodeUid);
    }

    /// <summary>
    /// Get all running flow executors
    /// </summary>
    /// <returns>A list of all running flow executors</returns>
    [HttpGet]
    public async Task<IEnumerable<FlowExecutorInfo>> GetAll()
    {
        if (HttpContext?.Response != null)
        {
            var settings = await new SettingsController().Get();
            if (settings.IsPaused)
            {
                HttpContext.Response.Headers.TryAdd("x-paused", "1");
            }
        }

        // we don't want to return the logs here, too big
        var liveExecutors = Executors.Values.Where(x => x != null).ToList();
        var results = liveExecutors.Select(x => new FlowExecutorInfo
        {
            // have to create a new object, otherwise if we change the log we change the log on the shared object
            LibraryFile = x.LibraryFile,
            CurrentPart = x.CurrentPart,
            CurrentPartName = x.CurrentPartName,
            CurrentPartPercent = x.CurrentPartPercent,
            Library = x.Library,
            NodeUid = x.NodeUid,
            NodeName = x.NodeName,
            RelativeFile = x.RelativeFile,
            StartedAt = x.StartedAt,
            TotalParts = x.TotalParts,
            Uid = x.Uid,
            WorkingFile = x.WorkingFile
        }).ToList();
        #if(DEBUG)
        if (false && results.Any() != true)
        {
            results = Enumerable.Range(1, 2).Select(x => new FlowExecutorInfo
            {
                LibraryFile = new LibraryFile()
                {
                    Name = "/ssssssssssssssssssss/dddddddddddddddwwwwwwwww/wwwwwweeeeeeeeeeeeexxxxxxxxxx/xxxxxxxxdffffffffffffffff/ffffddddddddddxzfdgffdgFile_x." + new string('w', 200)
                },
                CurrentPart = x,
                CurrentPartName = "Part " + x,
                CurrentPartPercent = x * 10,
                Library = new()
                {
                    Name = "Library " + x
                },
                NodeUid = new Guid("00000000-0000-0000-0000-00000000000" + x),
                NodeName = "FFMPEG Builder: Audio Normalization",
                RelativeFile = "/tv/dfgdfgfffffffffffffffffffffff/dfffffffffffffffffff/dfddddddddddddddd/file" + x + ".mkv",
                StartedAt = DateTime.Now.AddMinutes(-x * 5),
                TotalParts = 20,
                Uid = new Guid("00000000-0000-0000-0000-00000000000" + x),
                WorkingFile = "workingfile-" + x
            }).ToList();

        }
        #endif
        return results;
    }

    /// <summary>
    /// Gets the log of a library file
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    /// <param name="lineCount">The number of lines to fetch, 0 to fetch them all</param>
    /// <returns>The log of a library file</returns>
    [HttpGet("{uid}/log")]
    public string Log([FromRoute] Guid uid, [FromQuery] int lineCount = 0) => LibraryFileLogHelper.GetLog(uid);

    /// <summary>
    /// Abort work by library file
    /// </summary>
    /// <param name="uid">The UID of the library file to abort</param>
    /// <returns>An awaited task</returns>
    [HttpDelete("by-file/{uid}")]
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
    }

    /// <summary>
    /// Abort work 
    /// </summary>
    /// <param name="uid">The UID of the executor</param>
    /// <param name="libraryFileUid">the UID of the library file</param>
    /// <returns>an awaited task</returns>
    [HttpDelete("{uid}")]
    public async Task Abort([FromRoute] Guid uid, [FromQuery] Guid libraryFileUid)
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

            Logger.Instance?.ILog("Sending AbortFlow " + uid);
            try
            {
                await this.Context?.Clients?.All?.SendAsync("AbortFlow", uid);
                Logger.Instance?.ILog("Sent AbortFlow " + uid);
            }
            catch (Exception ex)
            {
                Logger.Instance?.WLog("Failed sending AbortFlow " + uid + " => " + ex.Message);
            }

            if (flowinfo?.LibraryFile != null)
            {
                Logger.Instance?.ILog("Sending AbortFlow to library file UID " + flowinfo.LibraryFile.Uid);
                try
                {
                    await this.Context?.Clients?.All?.SendAsync("AbortFlow", flowinfo?.LibraryFile.Uid);
                }
                catch (Exception ex)
                {
                    Logger.Instance?.WLog("Failed sending AbortFlowto library file UID " + uid + " => " + ex.Message);
                }

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
                    if (info == null || info.LastUpdate < DateTime.Now.AddMinutes(-1))
                    {
                        // its gone quiet, kill it
                        Executors.Remove(uid);
                    }
                }
            }
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

            if(executorInfo != null)
                executorInfo.LastUpdate = DateTime.Now;

            if (info.LibraryFile != null)
            {
                var service = new LibraryFileService();
                var current = service.GetFileStatus(info.LibraryFile.Uid);
                if (current == FileStatus.Unprocessed)
                {
                    // can happen if server was restarted
                    service.UpdateWork(info.LibraryFile).Wait();
                }
            }
            return true;
        }
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
            if (executor != null && executor.LastUpdate < DateTime.Now.AddSeconds(-60))
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