using FileFlows.Server.Helpers;
using FileFlows.ServerShared.Models;
using FileFlows.Server.Controllers;
using FileFlows.ServerShared.Workers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// Service for communicating with FileFlows server for library files
/// </summary>
public partial class LibraryFileService 
{
    /// <summary>
    /// Gets the next library file queued for processing
    /// </summary>
    /// <param name="nodeName">The name of the node requesting a library file</param>
    /// <param name="nodeUid">The UID of the node</param>
    /// <param name="nodeVersion">the version of the node</param>
    /// <param name="workerUid">The UID of the worker on the node</param>
    /// <returns>If found, the next library file to process, otherwise null</returns>
    public async Task<NextLibraryFileResult> GetNext(string nodeName, Guid nodeUid, string nodeVersion, Guid workerUid)
    {
        _ = new NodeController().UpdateLastSeen(nodeUid);
        
        if (UpdaterWorker.UpdatePending)
            return NextFileResult (NextLibraryFileStatus.UpdatePending); // if an update is pending, stop providing new files to process

        var settings = await SettingsService.Load().Get();
        if (settings.IsPaused)
            return NextFileResult(NextLibraryFileStatus.SystemPaused);

        if (Version.TryParse(nodeVersion, out var nVersion) == false)
            return NextFileResult(NextLibraryFileStatus.InvalidVersion);

        if (nVersion < Globals.MinimumNodeVersion)
        {
            Logger.Instance.ILog($"Node '{nodeName}' version '{nVersion}' is less than minimum supported version '{Globals.MinimumNodeVersion}'");
            return NextFileResult(NextLibraryFileStatus.VersionMismatch);
        }

        var node = (await NodeService.Load().GetByUidAsync(nodeUid));
        if (node != null && node.Version != nodeVersion)
        {
            node.Version = nodeVersion;
            new NodeService().Update(node);
        }

        if (await NodeEnabled(node) == false)
            return NextFileResult(NextLibraryFileStatus.NodeNotEnabled);

        var file = await GetNextLibraryFile(node, workerUid);
        if(file == null)
            return NextFileResult(NextLibraryFileStatus.NoFile, file);
        return NextFileResult(NextLibraryFileStatus.Success, file);
    }
    
    private async Task<bool> NodeEnabled(ProcessingNode node)
    {
        #if(DEBUG)
        if (Globals.IsUnitTesting)
            return await Task.FromResult(true);
        #endif
        var licensedNodes = LicenseHelper.GetLicensedProcessingNodes();
        var allNodes = new NodeService().GetAll();
        var enabledNodes = allNodes.Where(x => x.Enabled).OrderBy(x => x.Name).Take(licensedNodes).ToArray();
        var enabledNodeUids = enabledNodes.Select(x => x.Uid).ToArray();
        return enabledNodeUids.Contains(node.Uid);
    }
    
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
    /// Gets all matching library files
    /// </summary>
    /// <param name="status">the status</param>
    /// <param name="skip">the amount to skip</param>
    /// <param name="rows">the number to fetch</param>
    /// <param name="filter">[Optional] filter text</param>
    /// <param name="allowedLibraries">[Optional] list of libraries to include</param>
    /// <param name="maxSizeMBs">[Optional] maximum file size to include</param>
    /// <param name="exclusionUids">[Optional] list of UIDs to exclude</param>
    /// <returns>a list of matching library files</returns>
    public async Task<IEnumerable<LibraryFile>> GetAll(FileStatus? status, int skip = 0, int rows = 0,
        string filter = null, List<Guid> allowedLibraries = null, long? maxSizeMBs = null,
        List<Guid> exclusionUids = null)
    {
        var query = await ConstructQuery(status, allowedLibraries, maxSizeMBs, exclusionUids);
        if (string.IsNullOrWhiteSpace(filter) == false)
        {
            filter = filter.ToLowerInvariant();
            query = query.Where(x => x.Name.ToLowerInvariant().Contains(filter));
        }

        if (skip > 0)
            query = query.Skip(skip);
        if (rows > 0)
            query = query.Take(rows);
        return query.ToList();
    }
    
    /// <summary>
    /// Constructs the query of the cached data
    /// </summary>
    /// <param name="status">the status of the data</param>
    /// <param name="allowedLibraries"></param>
    /// <param name="maxSizeMBs"></param>
    /// <param name="exclusionUids"></param>
    /// <returns></returns>
    private async Task<IEnumerable<LibraryFile>> ConstructQuery(FileStatus? status, List<Guid> allowedLibraries = null,
        long? maxSizeMBs = null, List<Guid> exclusionUids = null)
    {
        try
        {
            if (allowedLibraries is { Count: 0 })
                return new LibraryFile[] { }; // no libraries allowed 
            
            IEnumerable<LibraryFile>? query = null;
            if(status == null)
                return Data.Select(x => x.Value);
            
            if ((int)status > 0)
            {
                // the status in the db is correct and not a computed status

                query = Data.Where(x =>  x.Value.Status == status.Value).Select(x => x.Value);

                if (status is FileStatus.Processed or FileStatus.ProcessingFailed)
                    query = query.OrderByDescending(x => x.ProcessingEnded)
                        .ThenBy(x => x.DateModified);
                else
                    query = query.OrderBy(x => x.DateModified);
                return query;
            }

            var libraries = (await LibraryService.Load().GetAllAsync()).ToDictionary(x => x.Uid, x => x);

            var disabled = libraries.Values.Where(x => x.Enabled == false).Select(x => x.Uid).ToList();
            if (status == FileStatus.Disabled && disabled?.Any() == false)
                return new List<LibraryFile>(); // no disabled libraries, therefore no disabled files

            int quarter = TimeHelper.GetCurrentQuarter();
            var outOfSchedule = libraries.Values.Where(x => x.Schedule?.Length != 672 || x.Schedule[quarter] == '0')
                .Select(x => x.Uid).ToList();
            if (status == FileStatus.OutOfSchedule && outOfSchedule.Any() == false)
                return new List<LibraryFile>(); // no out of schedule libraries, therefore no data

            query = Data.Where(x =>
                {
                    if (x.Value.LibraryUid == null)
                        return false; // shouldn't happen
                    if (libraries.ContainsKey(x.Value.LibraryUid.Value) == false)
                        return false; // also shouldn't happen
                    
                    if (x.Value.Status != FileStatus.Unprocessed)
                        return false;
                    if (maxSizeMBs is > 0)
                    {
                        if (x.Value.OriginalSize > maxSizeMBs * 1_000_000)
                            return false;
                    }

                    bool forced = (x.Value.Flags & LibraryFileFlags.ForceProcessing) ==
                                  LibraryFileFlags.ForceProcessing;
                    
                    bool inDisabledLibrary = disabled.Contains(x.Value.LibraryUid.Value) && forced == false;
                    if (status == FileStatus.Disabled && inDisabledLibrary == false)
                        return false; // we only want disabled files
                    if (status != FileStatus.Disabled && inDisabledLibrary)
                        return false; // we dont want disabled files
                    
                    bool isOutOfScheduleLibrary = outOfSchedule.Contains(x.Value.LibraryUid.Value) && forced == false;
                    if (status == FileStatus.OutOfSchedule && isOutOfScheduleLibrary == false)
                        return false; // we only want out of schedule files
                    if (status != FileStatus.OutOfSchedule && isOutOfScheduleLibrary)
                        return false; // we dont want out of schedule files

                    bool onHold = x.Value.HoldUntil > DateTime.Now;
                    if (status == FileStatus.OnHold && onHold == false)
                        return false; // we only want on hold files
                    if (status != FileStatus.OnHold && onHold)
                        return false; // we dont want on hold files
                    
                    return true;
                })
                .Select(x => x.Value);
            if (exclusionUids?.Any() == true)
                query = query.Where(x => exclusionUids.Contains(x.Uid) == false);
            if (allowedLibraries != null)
                query = query.Where(x => x.LibraryUid != null && allowedLibraries.Contains(x.LibraryUid.Value));
            
            if (status == FileStatus.Disabled || status == FileStatus.OutOfSchedule)
                return query.OrderBy(x => x.DateModified);
            
            // add on hold condition
            if (status == FileStatus.OnHold)
                return query.OrderBy(x => x.HoldUntil).ThenBy(x => x.DateModified);


            var random = new Random(DateTime.Now.Millisecond);
            DateTime now = DateTime.Now;
            query = query.OrderBy(x =>
            {

                if (x.Order > 0)
                    return x.Order;
                return 1_000_000_000;
            }).ThenByDescending(x =>
            {
                var library = libraries[x.LibraryUid!.Value]; // cant be null due to previous checks
                return library.Priority;
            }).ThenBy(x =>
            {
                var library = libraries[x.LibraryUid!.Value]; // cant be null due to previous checks
                if (library.ProcessingOrder == ProcessingOrder.Random)
                    return random.Next();

                if (library.ProcessingOrder == ProcessingOrder.LargestFirst)
                    return x.OriginalSize * -1;

                if (library.ProcessingOrder == ProcessingOrder.SmallestFirst)
                    return x.OriginalSize;

                if (library.ProcessingOrder == ProcessingOrder.OldestFirst)
                    return x.CreationTime.Ticks;
                
                if (library.ProcessingOrder == ProcessingOrder.NewestFirst)
                    return x.CreationTime.Ticks * -1;

                // as found
                return x.DateCreated.Ticks;
            });

            return query;
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Failed GetAll Files: " + ex.Message + "\n" + ex.StackTrace);
            return new LibraryFile[] { };
        }
    }

    private static SemaphoreSlim NextFileSemaphore = new SemaphoreSlim(1);
    
    /// <summary>
    /// Gets the next library file queued for processing
    /// </summary>
    /// <param name="node">The node doing the processing</param>
    /// <param name="workerUid">The UID of the worker on the node</param>
    /// <returns>If found, the next library file to process, otherwise null</returns>
    public async Task<LibraryFile?> GetNextLibraryFile(ProcessingNode node, Guid workerUid)
    {
        var nodeLibraries = node?.Libraries?.Select(x => x.Uid)?.ToList() ?? new List<Guid>();

        var canProcess = new LibraryService().GetAll().Where(x =>
        {
            if (node.AllLibraries == ProcessingLibraries.All)
                return true;
            if (node.AllLibraries == ProcessingLibraries.Only)
                return nodeLibraries.Contains(x.Uid);
            return nodeLibraries.Contains(x.Uid) == false;
        }).Select(x => x.Uid).ToList();
        var executing = WorkerController.ExecutingLibraryFiles()?.ToList() ?? new List<Guid>();

        await NextFileSemaphore.WaitAsync();
        try
        {
            var nextFile = (await GetAll(FileStatus.Unprocessed, skip: 0, rows: 1, allowedLibraries: canProcess,
                maxSizeMBs: node.MaxFileSizeMb, exclusionUids: executing)).FirstOrDefault();

            if (nextFile == null)
                return nextFile;

            nextFile.Status = FileStatus.Processing;
            nextFile.WorkerUid = workerUid;
            nextFile.ProcessingStarted = DateTime.Now;
            nextFile.NodeUid = node.Uid;
            nextFile.NodeName = node.Name;
            
            #if(DEBUG)
            if (Globals.IsUnitTesting)
                return nextFile;
            #endif
            
            await DbHelper.Execute("update LibraryFile set NodeUid = @0 , NodeName = @1 , WorkerUid = @2 " +
                                   $" , Status = @3 , ProcessingStarted = @4, OriginalMetadata = '', FinalMetadata = '', " +
                                   $" ExecutedNodes = '' where Uid = @5",
                nextFile.NodeUid, nextFile.NodeName, nextFile.WorkerUid, (int)FileStatus.Processing, 
                nextFile.ProcessingStarted, nextFile.Uid);
            return nextFile;
        }
        finally
        {
            NextFileSemaphore.Release();
        }
    }

}