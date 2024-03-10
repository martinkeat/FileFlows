using FileFlows.Managers;
using FileFlows.Server.Helpers;
using FileFlows.Server.Hubs;
using FileFlows.ServerShared.Models;
using FileFlows.ServerShared.Services;
using FileFlows.ServerShared.Workers;
using FileFlows.Shared.Models;
using Humanizer;

namespace FileFlows.Server.Services;

public class LibraryFileService : ILibraryFileService
{
    private static SemaphoreSlim NextFileSemaphore = new SemaphoreSlim(1);
    
    /// <summary>
    /// Constructs a next library file result
    /// </summary>
    /// <param name="status">the status of the call</param>
    /// <param name="file">the library file to process</param>
    /// <returns>the next library file result</returns>
    private NextLibraryFileResult NextFileResult(NextLibraryFileStatus? status = null, LibraryFile file = null)
    {
        NextLibraryFileResult result = new();
        if (status != null)
            result.Status = status.Value;
        result.File = file;
        return result;
    }
    

    /// <summary>
    /// Get all matching files
    /// </summary>
    /// <param name="status">the status to get</param>
    /// <param name="skip">how many files to skip</param>
    /// <param name="rows">the rows to get</param>
    /// <param name="filter">a text filter</param>
    /// <param name="allLibraries">all libraries in the systemZ</param>
    /// <returns>the matching files</returns>
    public async Task<List<LibraryFile>> GetAll(FileStatus? status = null, int skip = 0, int rows = 0, string filter = null, List<Library>? allLibraries = null)
    {
        allLibraries ??= (await ServiceLoader.Load<LibraryService>().GetAllAsync());
        
        var sysInfo = new LibraryFilterSystemInfo()
        {
            AllLibraries = allLibraries.ToDictionary(x => x.Uid, x => x),
            Executors = FlowRunnerService.Executors.Values.ToList(),
            LicensedForProcessingOrder = LicenseHelper.IsLicensed(LicenseFlags.ProcessingOrder)
        };
        return await new LibraryFileManager().GetAll(new()
        {
            Status = status,
            Skip = skip,
            Rows = rows,
            Filter = filter,
            SysInfo = sysInfo
        });
    }

    /// <summary>
    /// Gets the total items matching the filter
    /// </summary>
    /// <param name="status">the status</param>
    /// <param name="filter">the filter</param>
    /// <returns>the total number of items matching</returns>
    public async Task<int> GetTotalMatchingItems(FileStatus? status, string filter)
    {
        var allLibraries = await ServiceLoader.Load<LibraryService>().GetAllAsync();
        return await new LibraryFileManager().GetTotalMatchingItems(allLibraries, status, filter);
    }

    /// <inheritdoc />
    public async Task<NextLibraryFileResult> GetNext(string nodeName, Guid nodeUid, string nodeVersion, Guid workerUid)
    {
        var nodeService = ServiceLoader.Load<NodeService>();
        await nodeService.UpdateLastSeen(nodeUid);
        
        if (UpdaterWorker.UpdatePending)
            return NextFileResult (NextLibraryFileStatus.UpdatePending); // if an update is pending, stop providing new files to process

        var settings = await SettingsService.Load().Get();
        if (settings.IsPaused)
            return NextFileResult(NextLibraryFileStatus.SystemPaused);

        var node = await nodeService.GetByUidAsync(nodeUid);
        if (node != null && node.Version != nodeVersion)
        {
            node.Version = nodeVersion;
            await nodeService.UpdateVersion(node.Uid, nodeVersion);
        }
        
        if (nodeUid != Globals.InternalNodeUid) // dont test version number for internal processing node
        {
            if (Version.TryParse(nodeVersion, out var nVersion) == false)
                return NextFileResult(NextLibraryFileStatus.InvalidVersion);

            if (nVersion < Globals.MinimumNodeVersion)
            {
                Logger.Instance.ILog(
                    $"Node '{nodeName}' version '{nVersion}' is less than minimum supported version '{Globals.MinimumNodeVersion}'");
                return NextFileResult(NextLibraryFileStatus.VersionMismatch);
            }
        }

        if (await NodeEnabled(node) == false)
            return NextFileResult(NextLibraryFileStatus.NodeNotEnabled);

        var file = await GetNextLibraryFile(node, workerUid);
        if(file == null)
            return NextFileResult(NextLibraryFileStatus.NoFile, file);
    
        #region reset the file for processing
        try
        {
            // try to delete a log file for this library file if one already exists (in case the flow was cancelled and now its being re-run)                
            LibraryFileLogHelper.DeleteLogs(file.Uid);
        }
        catch (Exception)
        {
        }

        Logger.Instance.ILog("Resetting file info for: " + file.Name);
        file.FinalSize = 0;
        file.FailureReason = string.Empty;
        file.OutputPath = string.Empty;
        file.ProcessOnNodeUid = null;
        file.ProcessingEnded = DateTime.MinValue;
        file.ExecutedNodes = new();
        file.FinalMetadata = new();
        file.OriginalMetadata= new();
        await new LibraryFileManager().ResetFileInfoForProcessing(file.Uid);
        #endregion
        
        return NextFileResult(NextLibraryFileStatus.Success, file);
    }

    /// <inheritdoc />
    public Task<LibraryFile?> Get(Guid uid)
        => new LibraryFileManager().Get(uid);

    /// <inheritdoc />
    public Task Delete(params Guid[] uids)
        => new LibraryFileManager().Delete(uids);

    /// <inheritdoc />
    public async Task<LibraryFile> Update(LibraryFile libraryFile)
    {
        await new LibraryFileManager().UpdateFile(libraryFile);
        return libraryFile;
    }


    /// <summary>
    /// Gets the library status overview
    /// </summary>
    /// <returns>the library status overview</returns>
    public Task<List<LibraryStatus>> GetStatus()
        => new LibraryFileManager().GetStatus();
    
    /// <summary>
    /// Saves the full log for a library file
    /// Call this after processing has completed for a library file
    /// </summary>
    /// <param name="uid">The uid of the library file</param>
    /// <param name="log">the log</param>
    /// <returns>true if successfully saved log</returns>
    public async Task<bool> SaveFullLog(Guid uid, string log)
    {
        try
        {
            await LibraryFileLogHelper.SaveLog(uid, log, saveHtml: true);
            return true;
        }
        catch (Exception) { }
        return false;
    }

    /// <summary>
    /// Tests if a library file exists on server.
    /// This is used to test if a mapping issue exists on the node, and will be called if a Node cannot find the library file
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    public async Task<bool> ExistsOnServer(Guid uid)
    {
        var libFile = await Get(uid);
        if (libFile == null)
            return false;
        bool result = false;
        
        if (libFile.IsDirectory)
        {
            Logger.Instance.ILog("Checking Folder exists on server: " + libFile.Name);
            try
            {
                result = System.IO.Directory.Exists(libFile.Name);
            }
            catch (Exception) {  }
        }
        else
        {
            try
            {
                result = System.IO.File.Exists(libFile.Name);
            }
            catch (Exception)
            {
            }
        }

        Logger.Instance.ILog((libFile.IsDirectory ? "Directory" : "File") +
                             (result == false ? " does not exist" : "exists") +
                             " on server: " + libFile.Name);
        
        return result;
    }
    
    
    
    
    /// <summary>
    /// Checks if the node is enabled
    /// </summary>
    /// <param name="node">the processing node</param>
    /// <returns>true if enabled, otherwise false</returns>
    private async Task<bool> NodeEnabled(ProcessingNode node)
    {
#if(DEBUG)
        if (Globals.IsUnitTesting)
            return await Task.FromResult(true);
#endif
        var licensedNodes = LicenseHelper.GetLicensedProcessingNodes();
        var allNodes = await ServiceLoader.Load<NodeService>().GetAllAsync();
        var enabledNodes = allNodes.Where(x => x.Enabled).OrderBy(x => x.Name).Take(licensedNodes).ToArray();
        var enabledNodeUids = enabledNodes.Select(x => x.Uid).ToArray();
        return enabledNodeUids.Contains(node.Uid);
    }
    
    
    /// <summary>
    /// Gets the next library file queued for processing
    /// </summary>
    /// <param name="node">The node doing the processing</param>
    /// <param name="workerUid">The UID of the worker on the node</param>
    /// <returns>If found, the next library file to process, otherwise null</returns>
    public async Task<LibraryFile?> GetNextLibraryFile(ProcessingNode node, Guid workerUid)
    {
        var nodeLibraries = node?.Libraries?.Select(x => x.Uid)?.ToList() ?? new List<Guid>();

        var outOfSchedule = TimeHelper.InSchedule(node.Schedule) == false;

        var allLibraries = (await ServiceLoader.Load<LibraryService>().GetAllAsync());

        var sysInfo = new LibraryFilterSystemInfo()
        {
            AllLibraries = allLibraries.ToDictionary(x => x.Uid, x => x),
            Executors = FlowRunnerService.Executors.Values.ToList(),
            LicensedForProcessingOrder = LicenseHelper.IsLicensed(LicenseFlags.ProcessingOrder)
        };

        var canProcess = allLibraries.Where(x =>
        {
            if (node.AllLibraries == ProcessingLibraries.All)
                return true;
            if (node.AllLibraries == ProcessingLibraries.Only)
                return nodeLibraries.Contains(x.Uid);
            return nodeLibraries.Contains(x.Uid) == false;
        }).Select(x => x.Uid).ToList();
        var executing = FlowRunnerService.ExecutingLibraryFiles()?.ToList() ?? new List<Guid>();

        await NextFileSemaphore.WaitAsync();
        try
        {
            var manager = new LibraryFileManager();
            var waitingForReprocess = outOfSchedule
                ? null
                : (await manager.GetAll(new () { Status = FileStatus.ReprocessByFlow, SysInfo = sysInfo})).FirstOrDefault(x =>
                    x.ProcessOnNodeUid == node.Uid);
            
            if(waitingForReprocess != null)
                Logger.Instance.ILog($"File waiting for reprocessing [{node.Name}]: " + waitingForReprocess.Name);

            var nextFile = waitingForReprocess ?? (await manager.GetAll(new ()
                {
                    Status = FileStatus.Unprocessed, Skip = 0, Rows = 1, AllowedLibraries = canProcess,
                    MaxSizeMBs = node.MaxFileSizeMb, ExclusionUids =  executing, ForcedOnly = outOfSchedule,
                    SysInfo = sysInfo
                
                }
            )).FirstOrDefault();

            if (nextFile == null)
                return nextFile;

            if (waitingForReprocess == null && await HigherPriorityWaiting(node, nextFile, allLibraries))
            {
                Logger.Instance.ILog("Higher priority node waiting to process file");
                return null; // a higher priority node should process this file
            }

            nextFile.Status = FileStatus.Processing;
            nextFile.WorkerUid = workerUid;
            nextFile.ProcessingStarted = DateTime.UtcNow;
            nextFile.NodeUid = node.Uid;
            nextFile.NodeName = node.Name;
            
            #if(DEBUG)
            if (Globals.IsUnitTesting)
                return nextFile;
            #endif

            await manager.StartProcessing(nextFile.Uid, node.Uid,node.Name, workerUid);
            return nextFile;
        }
        finally
        {
            NextFileSemaphore.Release();
        }
    }
    
    

    /// <summary>
    /// Checks if another enabled processing node is enabled, in-schedule and not all runners are in use.
    /// If so, then will return false, so another higher priority node can processing a file
    /// </summary>
    /// <param name="node">the node to check</param>
    /// <param name="file">the next file that should be processed</param>
    /// <param name="allLibraries">all the libraries in the system</param>
    /// <returns>true if another higher priority node should be used instead</returns>
    private async Task<bool> HigherPriorityWaiting(ProcessingNode node, LibraryFile file, List<Library> allLibraries)
    {
        var allNodes = (await ServiceLoader.Load<NodeService>().GetAllAsync()).Where(x => 
            x.Uid != node.Uid && x.Priority > node.Priority && x.Enabled);
        var allLibrariesUids = allLibraries.Select(x => x.Uid).ToList();
        var executors = FlowRunnerService.Executors.Values.GroupBy(x => x.NodeUid)
            .ToDictionary(x => x.Key, x => x.Count());
        foreach (var other in allNodes)
        {
            // first check if its in schedule
            if (TimeHelper.InSchedule(other.Schedule) == false)
                continue;
            
            // check if this node is maxed out
            if (executors.ContainsKey(other.Uid) && executors[other.Uid] >= other.FlowRunners)
                continue; // its maxed out
            
            // check if other can process this library
            var nodeLibraries = node.Libraries?.Select(x => x.Uid)?.ToList() ?? new();
            List<Guid> allowedLibraries = node.AllLibraries switch
            {
                ProcessingLibraries.Only => nodeLibraries,
                ProcessingLibraries.AllExcept => allLibrariesUids.Where(x => nodeLibraries.Contains(x) == false).ToList(),
                _ => allLibrariesUids,
            };
            if (allowedLibraries.Contains(file.LibraryUid!.Value) == false)
            {
                Logger.Instance.DLog($"Node '{other.Name}' cannot process the file due to library restrictions: {file.Name}");
                continue;
            }

            // check the last time this node was seen to make sure its not disconnected
            if (other.LastSeen < DateTime.UtcNow.AddMinutes(10))
            {
                string lastSeen = DateTime.UtcNow.Subtract(other.LastSeen)+ " ago";
                try
                {
                    lastSeen = other.LastSeen.Humanize() + " ago";
                }
                catch (Exception)
                {
                    // this can throw
                }

                Logger.Instance.ILog("Higher priority node is offline: " + other.Name + ", last seen: " + lastSeen);
                continue; // 10 minute cut off, give it some grace period
            }
            
            // the "other" node is higher priority, its not maxed out, its in-schedule, so we dont want the "node"
            // processing this file
            Logger.Instance.ILog($"Higher priority node '{other.Name}' can process file, skipping node: '{node.Name}': {file.Name}");
            return true;
        }
        // no other node is higher priority, this node can process this file
        return false;
    }

    /// <summary>
    /// Unholds any held files
    /// </summary>
    /// <param name="uids">the UIDs of the files to unhold</param>
    /// <returns>a task to await</returns>
    public Task Unhold(Guid[] uids)
        => new LibraryFileManager().Unhold(uids);

    /// <summary>
    /// Updates all files with the new flow name if they used this flow
    /// </summary>
    /// <param name="uid">the UID of the flow</param>
    /// <param name="name">the new name of the flow</param>
    /// <returns>a task to await</returns>
    public Task UpdateFlowName(Guid uid, string name)
        => new LibraryFileManager().UpdateFlowName(uid, name);

    /// <summary>
    /// Deletes the files from the given libraries
    /// </summary>
    /// <param name="uids">the UIDs of the libraries</param>
    /// <param name="nonProcessedOnly">if only non processed files should be delete</param>
    /// <returns>a task to await</returns>
    public async Task DeleteByLibrary(Guid[] uids, bool nonProcessedOnly = false)
    {
        if (uids?.Any() != true)
            return;
        await new LibraryFileManager().DeleteByLibrary(uids, nonProcessedOnly);
        await ClientServiceManager.Instance.UpdateFileStatus();
    }

    /// <summary>
    /// Reprocess all files based on library UIDs
    /// </summary>
    /// <param name="uids">an array of UID of the libraries to reprocess</param>
    /// <returns>true if any rows were updated, otherwise false</returns>
    public async Task ReprocessByLibraryUid(Guid[] uids)
    {
        await new LibraryFileManager().ReprocessByLibraryUid(uids);
        await ClientServiceManager.Instance.UpdateFileStatus();
    }

    /// <summary>
    /// Gets all the UIDs for library files in the system
    /// </summary>
    /// <returns>the UIDs of known library files</returns>
    public Task<List<Guid>> GetUids()
        => new LibraryFileManager().GetUids();

    /// <summary>
    /// Gets the processing time for each library file 
    /// </summary>
    /// <returns>the processing time for each library file</returns>
    public Task<List<LibraryFileProcessingTime>> GetLibraryProcessingTimes()
        => new LibraryFileManager().GetLibraryProcessingTimes();

    /// <summary>
    /// Clears the executed nodes, metadata, final size etc for a file
    /// </summary>
    /// <param name="uid">The UID of the file</param>
    /// <returns>true if a row was updated, otherwise false</returns>
    public Task ResetFileInfoForProcessing(Guid uid)
        => new LibraryFileManager().ResetFileInfoForProcessing(uid);

    /// <summary>
    /// Updates the original size of a file
    /// </summary>
    /// <param name="uid">The UID of the file</param>
    /// <param name="size">the size of the file in bytes</param>
    /// <returns>true if a row was updated, otherwise false</returns>
    public Task<bool> UpdateOriginalSize(Guid uid, long size)
        => new LibraryFileManager().UpdateOriginalSize(uid, size);

    /// <summary>
    /// Resets any currently processing library files 
    /// This will happen if a server or node is reset
    /// </summary>
    /// <param name="nodeUid">[Optional] the UID of the node</param>
    public async Task ResetProcessingStatus(Guid? nodeUid)
    {
        await new LibraryFileManager().ResetProcessingStatus(nodeUid);
        await ClientServiceManager.Instance?.UpdateFileStatus();
    }

    /// <summary>
    /// Gets the current status of a file
    /// </summary>
    /// <param name="uid">The UID of the file</param>
    /// <returns>the current status of the file</returns>
    public Task<FileStatus?> GetFileStatus(Guid uid)
        => new LibraryFileManager().GetFileStatus(uid);

    /// <summary>
    /// Special case used by the flow runner to update a processing library file
    /// </summary>
    /// <param name="file">the processing library file</param>
    public Task UpdateWork(LibraryFile file)
        => new LibraryFileManager().UpdateWork(file);

    /// <summary>
    /// Moves the passed in UIDs to the top of the processing order
    /// </summary>
    /// <param name="uids">the UIDs to move</param>
    public Task MoveToTop(params Guid[] uids)
        => new LibraryFileManager().MoveToTop(uids);

    /// <summary>
    /// Reset processing for the files
    /// </summary>
    /// <param name="uids">a list of UIDs to reprocess</param>
    public async Task Reprocess(params Guid[] uids)
    { 
        await new LibraryFileManager().SetStatus(FileStatus.Unprocessed, uids);
        await ClientServiceManager.Instance.UpdateFileStatus();
    }

    /// <summary>
    /// Toggles a flag on files
    /// </summary>
    /// <param name="flag">the flag to toggle</param>
    /// <param name="uids">the UIDs of the files</param>
    /// <returns>true if any rows were updated, otherwise false</returns>
    public Task<bool> ToggleForce(params Guid[] uids)
        => new LibraryFileManager().ToggleFlag(LibraryFileFlags.ForceProcessing, uids);

    /// <summary>
    /// Force processing a set of files
    /// </summary>
    /// <param name="uids">the UIDs of the files</param>
    /// <returns>true if any rows were updated, otherwise false</returns>
    public Task<bool> ForceProcessing(Guid[] uids)
        => new LibraryFileManager().ForceProcessing(uids);

    /// <summary>
    /// Sets a status on a file
    /// </summary>
    /// <param name="status">The status to set</param>
    /// <param name="uids">the UIDs of the files</param>
    /// <returns>true if any rows were updated, otherwise false</returns>
    public Task<bool> SetStatus(FileStatus status, params Guid[] uids)
        => new LibraryFileManager().SetStatus(status, uids);

    /// <summary>
    /// Adds a files 
    /// </summary>
    /// <param name="files">the files being added</param>
    public Task Insert(params LibraryFile[] files)
        => new LibraryFileManager().Insert(files);

    /// <summary>
    /// Gets a library file if it is known
    /// </summary>
    /// <param name="path">the path of the library file</param>
    /// <returns>the library file if it is known</returns>
    public Task<LibraryFile?>GetFileIfKnown(string path)
        => new LibraryFileManager().GetFileIfKnown(path);

    /// <summary>
    /// Gets a library file if it is known by its fingerprint
    /// </summary>
    /// <param name="fingerprint">the fingerprint of the library file</param>
    /// <returns>the library file if it is known</returns>
    public Task<LibraryFile?>  GetFileByFingerprint(string fingerprint)
        => new LibraryFileManager().GetFileByFingerprint(fingerprint);

    /// <summary>
    /// Updates a moved file in the database
    /// </summary>
    /// <param name="file">the file to update</param>
    /// <returns>true if any files were updated</returns>
    public Task<bool> UpdateMovedFile(LibraryFile file)
        => new LibraryFileManager().UpdateMovedFile(file);

    /// <summary>
    /// Gets a list of all filenames and the file creation times
    /// </summary>
    /// <param name="includeOutput">if output names should be included</param>
    /// <returns>a list of all filenames</returns>
    public async Task<Dictionary<string, KnownFileInfo>> GetKnownLibraryFilesWithCreationTimes(bool includeOutput = false)
    {
        var data = await new LibraryFileManager().GetKnownLibraryFilesWithCreationTimes(includeOutput);
        return data.ToDictionary(x => x.Name, x => x);
    }

    /// <summary>
    /// Gets the shrinkage groups for the files
    /// </summary>
    /// <returns>the shrinkage groups</returns>
    public Task<List<ShrinkageData>> GetShrinkageGroups()
        => new LibraryFileManager().GetShrinkageGroups();

    /// <summary>
    /// Updates all files with the new library name if they used this library
    /// </summary>
    /// <param name="uid">the UID of the library</param>
    /// <param name="name">the new name of the library</param>
    /// <returns>a task to await</returns>
    public Task UpdateLibraryName(Guid uid, string name)
        => new LibraryFileManager().UpdateLibraryName(uid, name);

    /// <summary>
    /// Gets the total storage saved
    /// </summary>
    /// <returns>the total storage saved</returns>
    public Task<long> GetTotalStorageSaved()
        => new LibraryFileManager().GetTotalStorageSaved();
}