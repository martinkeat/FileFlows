using FileFlows.Server.Controllers;
using FileFlows.Server.Database.Managers;
using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Database;

/// <summary>
/// Caches the stores all information about files in memory for quicker access
/// </summary>
public static class LibraryFileCache
{
    private static Dictionary<Guid, LibraryFile> Data = new Dictionary<Guid, LibraryFile>();

    /// <summary>
    /// Adds a file to the data
    /// </summary>
    /// <param name="file">the file being added</param>
    public static void AddFile(LibraryFile file)
        => Data.TryAdd(file.Uid, file);

    /// <summary>
    /// Updates a file 
    /// </summary>
    /// <param name="file">the file being updated</param>
    public static void UpdateFile(LibraryFile file)
    {
        lock (Data)
        {
            if (Data.ContainsKey(file.Uid))
                Data[file.Uid] = file;
            else
                Data.Add(file.Uid, file);
        };
    }


    /// <summary>
    /// Deletes a file to the data
    /// </summary>
    /// <param name="file">the file being deleted</param>
    public static void DeleteFile(LibraryFile file)
    {
        lock (Data)
        {
            if (Data.ContainsKey(file.Uid))
                Data.Remove(file.Uid);
        }
    }

    private static async Task<FlowDbConnection> GetDbWithMappings()
    {
        var db = await DbHelper.GetDbManager().GetDb();
        db.Db.Mappers.Add(new CustomDbMapper());
        return db;
    }
    
    /// <summary>
    /// Refreshes the data
    /// </summary>
    public static async Task Refresh()
    {
        using var db = await GetDbWithMappings();
        var data = await db.Db.FetchAsync<LibraryFile>("select * from LibraryFile");
        var dict = data.ToDictionary(x => x.Uid, x => x);
        Data = dict;
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
    public static async Task<IEnumerable<LibraryFile>> GetAll(FileStatus? status, int skip = 0, int rows = 0,
        string filter = null, List<Guid> allowedLibraries = null, long? maxSizeMBs = null,
        List<Guid> exclusionUids = null)
    {
        var query = await ConstructQuery(status, allowedLibraries, maxSizeMBs, exclusionUids);
        if (string.IsNullOrWhiteSpace(filter))
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
    private async static Task<IEnumerable<LibraryFile>> ConstructQuery(FileStatus? status, List<Guid> allowedLibraries = null, long? maxSizeMBs = null,
            List<Guid> exclusionUids = null)
    {
        try
        {
            IEnumerable<LibraryFile>? query = null;
            if(status == null)
                return Data.Select(x => x.Value);
            
            if ((int)status > 0)
            {
                // the status in the db is correct and not a computed status
                string orderBy = "";
                    orderBy = "ProcessingEnded desc,";

                query = Data.Where(x =>  x.Value.Status == status.Value).Select(x => x.Value);

                if (status is FileStatus.Processed or FileStatus.ProcessingFailed)
                    query = query.OrderByDescending(x => x.ProcessingEnded)
                        .ThenBy(x => x.DateModified);
                else
                    query = query.OrderBy(x => x.DateModified);
                return query;
            }

            var libraries = (await new LibraryController().GetAll()).ToDictionary(x => x.Uid, x => x);

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
            if (allowedLibraries?.Any() == true)
                query = query.Where(x => allowedLibraries.Contains(x.LibraryUid.Value));
            
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
            }).ThenByDescending(x =>
            {
                var library = libraries[x.LibraryUid!.Value]; // cant be null due to previous checks
                if (library.ProcessingOrder == ProcessingOrder.Random)
                    return random.Next();

                var milliseconds = now.Subtract(x.DateCreated).TotalMilliseconds;

                if (library.ProcessingOrder == ProcessingOrder.AsFound)
                    return milliseconds;

                if (library.ProcessingOrder == ProcessingOrder.LargestFirst)
                    return x.OriginalSize * -1;

                if (library.ProcessingOrder == ProcessingOrder.SmallestFirst)
                    return x.OriginalSize;


                if (library.ProcessingOrder == ProcessingOrder.OldestFirst)
                    return milliseconds * -1;
                return milliseconds;
            });

            return query;
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Failed GetAll Files: " + ex.Message + "\n" + ex.StackTrace);
            return new LibraryFile[] { };
        }
    }
    
    /// <summary>
    /// Gets the next library file queued for processing
    /// </summary>
    /// <param name="nodeName">The name of the node requesting a library file</param>
    /// <param name="nodeUid">The UID of the node</param>
    /// <param name="workerUid">The UID of the worker on the node</param>
    /// <returns>If found, the next library file to process, otherwise null</returns>
    private async Task<LibraryFile> GetNextLibraryFile(string nodeName, Guid nodeUid, Guid workerUid)
    {
        var node = await new NodeController().Get(nodeUid);
        var nodeLibraries = node.Libraries?.Select(x => x.Uid)?.ToList() ?? new List<Guid>();
        var libraries = (await new LibraryController().GetAll()).ToArray();
        int quarter = TimeHelper.GetCurrentQuarter();
        var canProcess = libraries.Where(x =>
        {
            if (x.Enabled == false)
                return false;
            if (x.Schedule?.Length == 672 && x.Schedule[quarter] == '0')
                return false;
            if (node.AllLibraries == ProcessingLibraries.All)
                return true;
            if (node.AllLibraries == ProcessingLibraries.Only)
                return nodeLibraries.Contains(x.Uid);
            return nodeLibraries.Contains(x.Uid) == false;
        }).ToArray();
        
        var canForceProcess = libraries.Where(x =>
        {
            if (node.AllLibraries == ProcessingLibraries.All)
                return true;
            if (node.AllLibraries == ProcessingLibraries.Only)
                return nodeLibraries.Contains(x.Uid);
            return nodeLibraries.Contains(x.Uid) == false;
        }).ToArray();
        
        if (canProcess.Any() != true && canForceProcess.Any() != true)
            return null;

        // string libraryUids = string.Join(",", canProcess.Select(x => "'" + x.Uid + "'"));
        // string forceLibraryUids  = string.Join(",", canForceProcess.Select(x => "'" + x.Uid + "'"));
        
        await GetNextSemaphore.WaitAsync();
        try
        {
            // var next = GetAll(FileStatus.Unprocessed,
            //     0, 1,
            //     allowedLibraries: canProcess.Select(x => x.Uid).ToList(),
            //     maxSizeMBs: node.MaxFileSizeMb).Result;
            //     
            var executing = WorkerController.ExecutingLibraryFiles()?.ToList() ?? new List<Guid>();
            
            var libFile = Data.Where(x => x.Value.FileStatus.Unprocessed, 0, 1,
                allowedLibraries: libraries.Select(x => x.Uid).ToList(),
                exclusionUids: executing,
                maxSizeMBs: node.MaxFileSizeMb).Result?.FirstOrDefault();
            
            // var execAndWhere = executing?.Any() != true
            //     ? string.Empty
            //     : (" and LibraryFile.Uid not in (" + string.Join(",", executing.Select(x => "'" + x + "'")) + ") ");
            //
            // if (node.MaxFileSizeMb > 0)
            //     execAndWhere += $" and OriginalSize <= {node.MaxFileSizeMb * (1_000_000)} ";
            //
            // string sql = $"select * from LibraryFile {LIBRARY_JOIN} where Status = 0 and HoldUntil <= " +
            //              SqlHelper.Now() + " and (";
            //
            // if (canProcess.Any())
            //     sql += $" LibraryUId in ({libraryUids}) or ";
            //
            // sql += $" ( LibraryUid in ({forceLibraryUids}) and (Flags & 1) = 1 )";
            //
            // sql += ") " + execAndWhere + " order by " + UNPROCESSED_ORDER_BY;
            //
            // var libFile = await Database_Get<LibraryFile>(SqlHelper.Limit(sql, 1));
            // if (libFile == null)
            //     return null;
            //
            // // check the library this file belongs, we may have to grab a different file from this library
            // var library = libraries.FirstOrDefault(x => x.Uid == libFile.LibraryUid);
            // if (libFile.Order < 1 && library != null && library.ProcessingOrder != ProcessingOrder.AsFound)
            // {
            //     // need to change the order up
            //     bool orderGood = true;
            //     sql = $"select * from LibraryFile where Status = 0 and HoldUntil <= " + SqlHelper.Now() +
            //           execAndWhere +
            //           $" and LibraryUid = '{library.Uid}' order by ";
            //     if (library.ProcessingOrder == ProcessingOrder.Random)
            //         sql += " " + SqlHelper.Random() + " ";
            //     else if (library.ProcessingOrder == ProcessingOrder.LargestFirst)
            //         sql += " OriginalSize desc ";
            //     else if (library.ProcessingOrder == ProcessingOrder.SmallestFirst)
            //         sql += " OriginalSize ";
            //     else if (library.ProcessingOrder == ProcessingOrder.NewestFirst)
            //         sql += " LibraryFile.DateCreated desc ";
            //     else if (library.ProcessingOrder == ProcessingOrder.OldestFirst)
            //         sql += " LibraryFile.DateCreated asc ";
            //     else
            //         orderGood = false;
            //
            //     if (orderGood)
            //     {
            //         libFile = await Database_Get<LibraryFile>(SqlHelper.Limit(sql, 1));
            //         if (libFile == null)
            //             return null;
            //     }
            // }

            if (libFile == null)
                return null;

            await Database_Execute("update LibraryFile set NodeUid = @0 , NodeName = @1 , WorkerUid = @2 " +
                                   $" , Status = @3 , ProcessingStarted = @4, OriginalMetadata = '', FinalMetadata = '', ExecutedNodes = '' where Uid = @5",
                nodeUid, nodeName, workerUid, (int)FileStatus.Processing, DateTime.Now, libFile.Uid);

            return libFile;
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Failed getting next file for processing: " + ex.Message);
            throw;
        }
        finally
        {
            GetNextSemaphore.Release();
        }
    }
    
}