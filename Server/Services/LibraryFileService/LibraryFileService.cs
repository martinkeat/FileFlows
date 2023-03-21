using FileFlows.Server.Helpers;
using FileFlows.Server.Controllers;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// Service for communicating with FileFlows server for library files
/// </summary>
public partial class LibraryFileService : ILibraryFileService
{

    /// <summary>
    /// Saves the full library file log
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    /// <param name="log">The full plain text log to save</param>
    /// <returns>If it was successfully saved or not</returns>
    public Task<bool> SaveFullLog(Guid uid, string log) 
        => new LibraryFileController().SaveFullLog(uid, log);


    /// <summary>
    /// Adds a library file
    /// </summary>
    /// <param name="file">the library file to add</param>
    /// <returns>the added library file</returns>
    public async Task<LibraryFile> Add(LibraryFile file)
    {
        if(file.Uid == Guid.Empty)
            file.Uid = Guid.NewGuid();
        
        if(file.DateCreated < new DateTime(2000, 1,1))
            file.DateCreated = DateTime.Now;
        if (file.DateModified < new DateTime(2000, 1, 1))
            file.DateModified = DateTime.Now;
        file.ExecutedNodes ??= new ();
        file.OriginalMetadata ??= new ();
        file.FinalMetadata ??= new ();
        await Database_Insert(file);
        AddFile(file);
        return await Get(file.Uid);
    }
    
    // /// <summary>
    // /// Adds a many library file
    // /// </summary>
    // /// <param name="files">the library files to add</param>
    // /// <returns>an awaited task</returns>
    // public async Task AddMany(params LibraryFile[] files)
    // {
    //     if (files?.Any() != true)
    //         return;
    //
    //     foreach (var file in files)
    //     {
    //         if (file == null)
    //             continue;
    //         if(file.Uid == Guid.Empty)
    //             file.Uid = Guid.NewGuid();
    //         if(file.DateCreated < new DateTime(2000, 1,1))
    //             file.DateCreated = DateTime.Now;
    //         if (file.DateModified < new DateTime(2000, 1, 1))
    //             file.DateModified = DateTime.Now;
    //         file.ExecutedNodes ??= new ();
    //         file.OriginalMetadata ??= new ();
    //         file.FinalMetadata ??= new ();
    //         AddFile(file);
    //     }
    //
    //     await Database_AddMany(files);
    // }
    
    /// <summary>
    /// Updates a library file
    /// </summary>
    /// <param name="file">The library file to update</param>
    /// <returns>The newly updated library file</returns>
    public Task<LibraryFile> Update(LibraryFile file)
    {
        file.DateModified = DateTime.Now;
        file.ExecutedNodes ??= new ();
        file.OriginalMetadata ??= new ();
        file.FinalMetadata ??= new ();
        if (file.Status == FileStatus.Processed || file.Status == FileStatus.ProcessingFailed)
            file.Flags = LibraryFileFlags.None;
        Database_Update(file);
        UpdateFile(file);
        return Task.FromResult(file);
    }

    /// <summary>
    /// Special case used by the flow runner to update a processing library file
    /// </summary>
    /// <param name="file">the processing library file</param>
    public async Task UpdateWork(LibraryFile file)
    {
        if (file == null)
            return;
        
        string sql = "update LibraryFile set " +
                     $" Status = {((int)file.Status)}, " + 
                     $" FinalSize = {file.FinalSize}, " +
                     (file.Node == null ? "" : (
                         $" NodeUid = '{file.NodeUid}', NodeName = '{file.NodeName.Replace("'", "''")}', "
                     )) +
                     $" WorkerUid = '{file.WorkerUid}', " +
                     $" ProcessingStarted = @0, " +
                     $" ProcessingEnded = @1, " + 
                     $" ExecutedNodes = @2 " +
                     (file.Status != FileStatus.Processing ? ", Flags = 0 " : string.Empty) + // clear flags on processed files
                     $" where Uid = '{file.Uid}'";
        
        string executedJson = file.ExecutedNodes?.Any() != true
            ? string.Empty
            : JsonSerializer.Serialize(file.ExecutedNodes, CustomDbMapper.JsonOptions);
        await Database_Execute(sql, file.ProcessingStarted, file.ProcessingEnded, executedJson);
        UpdateFile(file);
    }

    /// <summary>
    /// Deletes library files
    /// </summary>
    /// <param name="uids">a list of UIDs to delete</param>
    /// <returns>an awaited task</returns>
    public async Task Delete(params Guid[] uids)
    {
        if (uids?.Any() != true)
            return;
        string inStr = string.Join(",", uids.Select(x => $"'{x}'"));
        await Database_Execute($"delete from LibraryFile where Uid in ({inStr})", null);
        Remove(uids);
    }

    /// <summary>
    /// Deletes library files from libraries
    /// </summary>
    /// <param name="libraryUids">a list of UIDs of libraries delete</param>
    /// <returns>an awaited task</returns>
    public async Task DeleteFromLibraries(params Guid[] libraryUids)
    {
        if (libraryUids?.Any() != true)
            return;
        string inStr = string.Join(",", libraryUids.Select(x => $"'{x}'"));
        await Database_Execute($"delete from LibraryFile where LibraryUid in ({inStr})", null);
        RemoveLibraries(libraryUids);
    }

    /// <summary>
    /// Tests if a library file exists on server.
    /// This is used to test if a mapping issue exists on the node, and will be called if a Node cannot find the library file
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    /// <returns>True if it exists on the server, otherwise false</returns>
    public Task<bool> ExistsOnServer(Guid uid) => new LibraryFileController().ExistsOnServer(uid);

    /// <summary>
    /// Get all the library file UIDs in the database
    /// </summary>
    /// <returns>all the library file UIDs in the database</returns>
    public async Task<IEnumerable<Guid>> GetUids()
        => await Database_Fetch<Guid>("select Uid from LibraryFile");


    /// <summary>
    /// Gets the total items matching the filter
    /// </summary>
    /// <param name="status">the status</param>
    /// <param name="filter">the filter</param>
    /// <returns>the total number of items matching</returns>
    public async Task<int> GetTotalMatchingItems(FileStatus? status, string filter)
    {
        try
        {
            string filterWhere = $"lower(name) like lower('%{filter.Replace("'", "''").Replace(" ", "%")}%')" ;
            if(status == null)
            {
                return await Database_ExecuteScalar<int>("select count(Uid) from LibraryFile where " + filterWhere);
            }
            if ((int)status > 0)
            {
                return await Database_ExecuteScalar<int>($"select count(Uid) from LibraryFile where Status = {(int)status} and {filterWhere}");
            }

            var libraries = new LibraryService().GetAll();
            var disabled = string.Join(", ",
                libraries.Where(x => x.Enabled == false).Select(x => "'" + x.Uid + "'"));
            int quarter = TimeHelper.GetCurrentQuarter();
            var outOfSchedule = string.Join(", ",
                libraries.Where(x => x.Schedule?.Length != 672 || x.Schedule[quarter] == '0').Select(x => "'" + x.Uid + "'"));

            string sql = $"select count(LibraryFile.Uid) from LibraryFile where Status = {(int)FileStatus.Unprocessed} and " + filterWhere;
            
            // add disabled condition
            if(string.IsNullOrEmpty(disabled) == false)
                sql += $" and LibraryUid {(status == FileStatus.Disabled ? "" : "not")} in ({disabled})";
            
            if (status == FileStatus.Disabled)
            {
                if (string.IsNullOrEmpty(disabled))
                    return 0;
                return await Database_ExecuteScalar<int>(sql);
            }
            
            // add out of schedule condition
            if(string.IsNullOrEmpty(outOfSchedule) == false)
                sql += $" and LibraryUid {(status == FileStatus.OutOfSchedule ? "" : "not")} in ({outOfSchedule})";
            
            if (status == FileStatus.OutOfSchedule)
            {
                if (string.IsNullOrEmpty(outOfSchedule))
                    return 0; // no out of schedule libraries
                
                return await Database_ExecuteScalar<int>(sql);
            }
            
            // add on hold condition
            sql += $" and HoldUntil {(status == FileStatus.OnHold ? ">" : "<=")} " + SqlHelper.Now();
            if (status == FileStatus.OnHold)
                return await Database_ExecuteScalar<int>(sql);

            sql = sql.Replace(" from LibraryFile", " from LibraryFile  " + LIBRARY_JOIN);
            return await Database_ExecuteScalar<int>(sql);
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Failed GetTotalMatchingItems Files: " + ex.Message + "\n" + ex.StackTrace);
            return 0;
        }
    }

    /// <summary>
    /// Reset processing for the files
    /// </summary>
    /// <param name="uids">a list of UIDs to reprocess</param>
    public async Task Reprocess(params Guid[] uids)
    {
        if (uids?.Any() != true)
            return;
        string inStr = string.Join(",", uids.Select(x => $"'{x}'"));
        await Database_Execute($"update LibraryFile set Status = 0 where Uid in ({inStr})");
        await SetStatus(FileStatus.Unprocessed, uids);
    }

    /// <summary>
    /// Unhold library files
    /// </summary>
    /// <param name="uids">the UIDs to unhold</param>
    /// <returns>an awaited task</returns>
    public Task Unhold(Guid[] uids)
        => SetStatus(FileStatus.Unprocessed, uids);

    /// <summary>
    /// Resets any currently processing library files 
    /// This will happen if a server or node is reset
    /// </summary>
    /// <param name="nodeUid">[Optional] the UID of the node</param>
    internal async Task ResetProcessingStatus(Guid? nodeUid = null)
    {
        if(nodeUid != null)
            await Database_Execute($"update LibraryFile set Status = 0 where Status = {(int)FileStatus.Processing} and NodeUid = '{nodeUid}'");
        else
            await Database_Execute($"update LibraryFile set Status = 0 where Status = {(int)FileStatus.Processing}");
    }
    
    /// <summary>
    /// Gets a list of all filenames and the file creation times
    /// </summary>
    /// <param name="includeOutput">if output names should be included</param>
    /// <returns>a list of all filenames</returns>
    public Dictionary<string, (DateTime CreationTime, DateTime LastWriteTime)> GetKnownLibraryFilesWithCreationTimes(bool includeOutput = false)
    {
        if (includeOutput == false)
        {
            return Data.DistinctBy(x => x.Value.Name.ToLowerInvariant())
                .ToDictionary(x => x.Value.Name.ToLowerInvariant(), x => (x.Value.CreationTime, x.Value.LastWriteTime));
        }

        var query = Data.Select(x => (x.Value.Name, x.Value.CreationTime, x.Value.LastWriteTime))
            .Union(Data.Where(x => string.IsNullOrEmpty(x.Value.OutputPath) == false)
                .Select(x => (x.Value.Name, x.Value.CreationTime, x.Value.LastWriteTime)));
        
        return query.DistinctBy(x => x.Name.ToLowerInvariant())
            .ToDictionary(x => x.Name.ToLowerInvariant(), x => (x.CreationTime, x.LastWriteTime));
    }

    /// <summary>
    /// Gets the current status of a file
    /// </summary>
    /// <param name="uid">The UID of the file</param>
    /// <returns>the current status of the rfile</returns>
    public FileStatus GetFileStatus(Guid uid)
        => GetByUid(uid)?.Status ?? FileStatus.Unprocessed;

    /// <summary>
    /// Moves the passed in UIDs to the top of the processing order
    /// </summary>
    /// <param name="uids">the UIDs to move</param>
    public async Task MoveToTop(params Guid[] uids)
    {
        if (uids?.Any() != true)
            return;
        string strUids = string.Join(", ", uids.Select(x => "'" + x + "'"));
        // get existing order first so we can shift those if these uids change the order
        // only get status == 0
        List<Guid> indexed = uids.ToList();
        var sorted = await Database_Fetch<LibraryFile>($"select * from LibraryFile where Status = 0 and ( ProcessingOrder > 0 or Uid IN ({strUids}))");
        sorted = sorted.OrderBy(x =>
        {
            int index = indexed.IndexOf(x.Uid);
            if (index < 0)
                return 10000 + x.Order;
            return index;
        }).ToList();

        var commands = new List<string>();
        for(int i=0;i<sorted.Count;i++)
        {
            var file = sorted[i];
            file.Order = i + 1;
            commands.Add($"update LibraryFile set ProcessingOrder = {file.Order} where Uid = '{file.Uid}';");
        }

        await Database_Execute(string.Join("\n", commands));
        Refresh();
    }


    /// <summary>
    /// Aborts a file by setting its status to failed
    /// Only used to abort files that were processing on startup
    /// </summary>
    /// <param name="uid">The UID of the file</param>
    public async Task Abort(Guid uid)
    {
        Logger.Instance.ILog("Aborting file: " + uid);
        await SetStatus(FileStatus.ProcessingFailed, uid);
    }

    /// <summary>
    /// Updates a moved file in the database
    /// </summary>
    /// <param name="file">the file to update</param>
    public Task UpdateMovedFile(LibraryFile file)
        => Database_Execute(
            $"update LibraryFile set Name = @0, RelativePath = @1, OutputPath = @2, CreationTime = @3, LastWriteTime = @4 where Uid = @5",
            file.Name, file.RelativePath, file.OutputPath, 
            file.CreationTime.ToString("yyyy-MM-dd HH:mm:ss"), file.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"), 
            file.Uid);
}