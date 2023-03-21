using System.Reflection.Metadata.Ecma335;
using Microsoft.AspNetCore.Mvc;
using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;
using FileFlows.Server.Helpers.ModelHelpers;
using FileFlows.Server.Services;
using FileFlows.ServerShared.Models;
using FileFlows.Shared.Helpers;

namespace FileFlows.Server.Controllers;

/// <summary>
/// Library files controller
/// </summary>
[Route("/api/library-file")]
public class LibraryFileController : Controller //ControllerStore<LibraryFile>
{
    private static CacheStore CacheStore = new();

    /// <summary>
    /// Gets the next library file for processing, and puts it into progress
    /// </summary>
    /// <param name="args">The arguments for the call</param>
    /// <returns>the next library file to process</returns>
    [HttpPost("next-file")]
    public async Task<NextLibraryFileResult> GetNext([FromBody] NextLibraryFileArgs args)
    {
        var result = await new LibraryFileService().GetNext(args.NodeName, args.NodeUid, args.NodeVersion, args.WorkerUid);
        if (result == null)
            return result;
        Logger.Instance.ILog($"GetNextFile for ['{args.NodeName}']({args.NodeUid}): {result.Status}");
        return result;
    }

    /// <summary>
    /// Lists all of the library files, only intended for the UI
    /// </summary>
    /// <param name="status">The status to list</param>
    /// <param name="page">The page to get</param>
    /// <param name="pageSize">The number of items to fetch</param>
    /// <param name="filter">[Optional] filter text</param>
    /// <returns>a slimmed down list of files with only needed information</returns>
    [HttpGet("list-all")]
    public async Task<LibraryFileDatalistModel> ListAll([FromQuery] FileStatus status, [FromQuery] int page = 0, [FromQuery] int pageSize = 0, [FromQuery] string filter = null)
    {
        var service = new LibraryFileService();
        var lfStatus = service.GetStatus();
        var files = await service.GetAll(status, page * pageSize, pageSize, filter);
        if (string.IsNullOrWhiteSpace(filter) == false)
        {
            // need to get total number of items matching filter aswell
            int total = await service.GetTotalMatchingItems(status, filter);
            HttpContext?.Response?.Headers?.TryAdd("x-total-items", total.ToString());
        }

        var libraries = new LibraryService().GetAll();
        return new()
        {
            Status = lfStatus,
            LibraryFiles = LibaryFileListModelHelper.ConvertToListModel(files, status, libraries)
        };
    }

    /// <summary>
    /// Gets all library files in the system
    /// </summary>
    /// <param name="status">The status to get, if missing will return all library files</param>
    /// <param name="skip">The amount of items to skip</param>
    /// <param name="top">The amount of items to grab, 0 to grab all</param>
    /// <returns>A list of library files</returns>
    [HttpGet]
    public Task<IEnumerable<LibraryFile>> GetAll([FromQuery] FileStatus? status, [FromQuery] int skip = 0, [FromQuery] int top = 0)
        => new LibraryFileService().GetAll(status, skip, top);
    
    /// <summary>
    /// Get next 10 upcoming files to process
    /// </summary>
    /// <returns>a list of upcoming files to process</returns>
    [HttpGet("upcoming")]
    public Task<IEnumerable<LibraryFile>> Upcoming()
        => new LibraryFileService().GetAll(FileStatus.Unprocessed, rows: 10);
    
    /// <summary>
    /// Gets the last 10 successfully processed files
    /// </summary>
    /// <returns>the last successfully processed files</returns>
    [HttpGet("recently-finished")]
    public Task<IEnumerable<LibraryFile>> RecentlyFinished() 
        => new LibraryFileService().GetAll(FileStatus.Processed, rows: 10);
        
    /// <summary>
    /// Gets the library status overview
    /// </summary>
    /// <returns>the library status overview</returns>
    [HttpGet("status")]
    public IEnumerable<LibraryStatus> GetStatus()
        => new LibraryFileService().GetStatus();


    /// <summary>
    /// Get a specific library file
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    /// <returns>the library file instance</returns>
    [HttpGet("{uid}")]
    public async Task<LibraryFile> Get(Guid uid)
    {
        var result = await new LibraryFileService().Get(uid);
        // if(DbHelper.UseMemoryCache == false && result != null)
        //     CacheStore.Store(result.Uid, result);
        if(result != null && (result.Status == FileStatus.ProcessingFailed || result.Status == FileStatus.Processed))
        {
            if (LibraryFileLogHelper.HtmlLogExists(uid))
                return result;
            LibraryFileLogHelper.CreateHtmlOfLog(uid);
        }
        return result;
    }


    /// <summary>
    /// Update a library file
    /// </summary>
    /// <param name="file">The library file to update</param>
    /// <returns>The updated library file</returns>
    [HttpPut]
    public async Task<LibraryFile> Update([FromBody] LibraryFile file)
    {
        var existing = await new LibraryFileService().Get(file.Uid);

        if (existing == null)
            throw new Exception("Not found");

        if (existing.Status == FileStatus.Processed && file.Status == FileStatus.Processing)
        {
            // already finished and reported finished in the Worker controller.  So ignore this out of date update
            return existing;
        }

        if (existing.Status != file.Status)
        {
            Logger.Instance?.ILog($"Setting library file status to: {file.Status} - {file.Name}");
            existing.Status = file.Status;
        }

        existing.Node = file.Node;
        if(existing.FinalSize == 0 || file.FinalSize > 0)
            existing.FinalSize = file.FinalSize;
        if(file.OriginalSize > 0)
            existing.OriginalSize = file.OriginalSize;
        if(string.IsNullOrEmpty(file.OutputPath))
            existing.OutputPath = file.OutputPath;
        existing.Flow = file.Flow;
        if(file.Library != null && file.Library.Uid == existing.Library.Uid)
            existing.Library = file.Library; // name may have changed and is being updated
        
        existing.DateCreated = file.DateCreated; // this can be changed if library file is unheld
        existing.ProcessingEnded = file.ProcessingEnded;
        existing.ProcessingStarted = file.ProcessingStarted;
        existing.WorkerUid = file.WorkerUid;
        existing.CreationTime = file.CreationTime;
        existing.LastWriteTime = file.LastWriteTime;
        existing.HoldUntil = file.HoldUntil;
        existing.Order = file.Order;
        existing.Fingerprint = file.Fingerprint;
        existing.FinalFingerprint = file.FinalFingerprint;
        existing.OriginalSize = file.OriginalSize;
        existing.ExecutedNodes = file.ExecutedNodes ?? new List<ExecutedNode>();
        if (file.OriginalMetadata?.Any() == true)
            existing.OriginalMetadata = file.OriginalMetadata;
        if (file.FinalMetadata?.Any() == true)
            existing.FinalMetadata = file.FinalMetadata;
        
        var updated = await new LibraryFileService().Update(existing);
        
        return updated;
    }


    /// <summary>
    /// Downloads a  log of a library file
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    /// <returns>The download action result</returns>
    [HttpGet("{uid}/log/download")]
    public IActionResult GetLog([FromRoute] Guid uid)
    {     
        string log = LibraryFileLogHelper.GetLog(uid);
        byte[] data = System.Text.Encoding.UTF8.GetBytes(log);
        return File(data, "application/octet-stream", uid + ".log");
    }
    
    /// <summary>
    /// Get the log of a library file
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    /// <param name="lines">Optional number of lines to fetch</param>
    /// <param name="html">if the log should be html if possible</param>
    /// <returns>The log of the library file</returns>
    [HttpGet("{uid}/log")]
    public string GetLog([FromRoute] Guid uid, [FromQuery] int lines = 0, [FromQuery] bool html = true)
    {
        try
        {
            return html ? LibraryFileLogHelper.GetHtmlLog(uid, lines) : LibraryFileLogHelper.GetLog(uid);
        }
        catch (Exception ex)
        {
            return "Error opening log: " + ex.Message;
        }
    }

    /// <summary>
    /// Saves the full log for a library file
    /// Call this after processing has completed for a library file
    /// </summary>
    /// <param name="uid">The uid of the library file</param>
    /// <param name="log">the log</param>
    /// <returns>true if successfully saved log</returns>
    [HttpPut("{uid}/full-log")]
    public async Task<bool> SaveFullLog([FromRoute] Guid uid, [FromBody] string log)
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
    /// A reference model of library files to move to the top of the processing queue
    /// </summary>
    /// <param name="model">The reference model of items in order to move</param>
    /// <returns>an awaited task</returns>
    [HttpPost("move-to-top")]
    public async Task MoveToTop([FromBody] ReferenceModel<Guid> model)
    {
        if (model == null || model.Uids?.Any() != true)
            return; // nothing to delete

        var list = model.Uids.ToArray();
        await new LibraryFileService().MoveToTop(list);
    }

    /// <summary>
    /// Adds a library file into the system
    /// </summary>
    /// <param name="libraryFile">the file to add</param>
    /// <returns>the newly added file</returns>
    internal Task<LibraryFile> Add(LibraryFile libraryFile) =>
        new LibraryFileService().Add(libraryFile);


    /// <summary>
    /// Checks if a library file exists on the server
    /// </summary>
    /// <param name="uid">The Uid of the library file to check</param>
    /// <returns>true if exists, otherwise false</returns>
    [HttpGet("exists-on-server/{uid}")]
    public async Task<bool> ExistsOnServer([FromRoute] Guid uid)
    {
        var libFile = await Get(uid);
        if (libFile == null)
            return false;
        try
        {
            return System.IO.File.Exists(libFile.Name);
        }
        catch (Exception) { return false; }
    }

    /// <summary>
    /// Delete library files from the system
    /// </summary>
    /// <param name="model">A reference model containing UIDs to delete</param>
    /// <returns>an awaited task</returns>
    [HttpDelete]
    public Task Delete([FromBody] ReferenceModel<Guid> model)
        => new LibraryFileService().Delete(model?.Uids);

    /// <summary>
    /// Delete library files from disk
    /// </summary>
    /// <param name="model">A reference model containing UIDs to delete</param>
    /// <returns>an awaited task</returns>
    [HttpDelete("delete-files")]
    public async Task<string> DeleteFiles([FromBody] ReferenceModel<Guid> model)
    {
        List<Guid> deleted = new();
        bool failed = false;
        foreach (var uid in model.Uids)
        {
            var lf = await Get(uid);
            if (System.IO.File.Exists(lf.Name) == false)
                continue;
            if (DeleteFile(lf.Name) == false)
            {
                failed = true;
                continue;
            }

            deleted.Add(lf.Uid);
        }

        if (deleted.Any())
            await new LibraryFileService().Delete(deleted.ToArray());

        return failed ? Translater.Instant("ErrorMessages.NotAllFilesCouldBeDeleted") : string.Empty;

        bool DeleteFile(string file)
        {
            try
            {
                System.IO.File.Delete(file);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.WLog("Failed to delete file: " + ex.Message);
                return false;
            }
        }
    }
    
    /// <summary>
    /// Reprocess library files
    /// </summary>
    /// <param name="model">A reference model containing UIDs to reprocess</param>
    /// <returns>an awaited task</returns>
    [HttpPost("reprocess")]
    public Task Reprocess([FromBody] ReferenceModel<Guid> model)
        => new LibraryFileService().Reprocess(model.Uids);

    /// <summary>
    /// Unhold library files
    /// </summary>
    /// <param name="model">A reference model containing UIDs to reprocess</param>
    /// <returns>an awaited task</returns>
    [HttpPost("unhold")]
    public Task Unhold([FromBody] ReferenceModel<Guid> model)
        => new LibraryFileService().Unhold(model?.Uids ?? new Guid[]{});


    /// <summary>
    /// Force processing of files
    /// Used to force files that are currently out of schedule to be processed
    /// </summary>
    /// <param name="model">the items to process</param>
    /// <returns>an awaited task</returns>
    [HttpPost("force-processing")]
    public Task ForceProcessing([FromBody] ReferenceModel<Guid> model)
        => new LibraryFileService().ForceProcessing(model?.Uids ?? new Guid[]{});


    /// <summary>
    /// Sets the status of files
    /// </summary>
    /// <param name="status">the status to set to</param>
    /// <param name="model">the items to set the status on</param>
    /// <returns>an awaited task</returns>
    [HttpPost("set-status/{status}")]
    public Task SetStatus([FromRoute] FileStatus status, [FromBody] ReferenceModel<Guid> model)
        => new LibraryFileService().SetStatus(status, model?.Uids ?? new Guid[]{});



    /// <summary>
    /// Gets the shrinkage data for a bar chart
    /// </summary>
    /// <returns>the bar chart data</returns>
    [HttpGet("shrinkage-bar-chart")]
    public object ShrinkageBarChart()
    {
        var groups = ShrinkageGroups();
        // #if(DEBUG)
        // groups = new Dictionary<string, ShrinkageData>()
        // {
        //     { "Movies", new() { FinalSize = 10_000_000_000, OriginalSize = 25_000_000_000 } },
        //     { "TV", new() { FinalSize = 45_000_000_000, OriginalSize = 75_000_000_000 } },
        //     { "Other", new() { FinalSize = 45_000_000_000, OriginalSize = 40_000_000_000 } },
        //     { "Other2", new() { FinalSize = 15_000_000_000, OriginalSize = 20_000_000_000 } },
        //     { "Other3", new() { FinalSize = 27_000_000_000, OriginalSize = 30_000_000_000 } },
        // };
        // #endif
        return new
        {
            series = new object[]
            {
                //new { name = "Final Size", data = groups.Select(x => (x.Value.OriginalSize - x.Value.FinalSize)).ToArray() },
                new { name = "Final Size", data = groups.Select(x => x.Value.FinalSize).ToArray() },
                //new { name = "Original Size", data = groups.Select(x => x.Value.OriginalSize).ToArray() }
                new { name = "Savings", data = groups.Select(x =>
                {
                    var change = x.Value.OriginalSize - x.Value.FinalSize;
                    if (change > 0)
                        return change;
                    return 0;
                }).ToArray() },
                new { name = "Increase", data = groups.Select(x =>
                {
                    var change = x.Value.OriginalSize - x.Value.FinalSize;
                    if (change > 0)
                        return 0;
                    return change * -1;
                }).ToArray() }
            },
            labels = groups.Select(x => x.Key.Replace("###TOTAL###", "Total")).ToArray(),
            items = groups.Select(x => x.Value.Items).ToArray()
        };
    }

    /// <summary>
    /// Get library file shrinkage grouped by library
    /// </summary>
    /// <returns>the library file shrinkage data</returns>
    [HttpGet("shrinkage-groups")]
    public Dictionary<string, ShrinkageData> ShrinkageGroups()
    {
        var data = new LibraryFileService().GetShrinkageGroups();
        var libraries = data.ToDictionary(x => x.Library, x => x);
        ShrinkageData total = new ShrinkageData();
        foreach (var lib in libraries)
        {
            total.FinalSize += lib.Value.FinalSize;
            total.OriginalSize += lib.Value.OriginalSize;
            total.Items += lib.Value.Items;
        }
        
        if (libraries.Count > 5)
        {
            ShrinkageData other = new ShrinkageData();
            while (libraries.Count > 4)
            {
                List<string> toRemove = new();
                var sd = libraries.MinBy(x => x.Value.Items);
                other.Items += sd.Value.Items;
                other.FinalSize += sd.Value.FinalSize;
                other.OriginalSize += sd.Value.OriginalSize;
                libraries.Remove(sd.Key);
            }

            libraries.Add("Other", other);
        }
        
        if (libraries.ContainsKey("###TOTAL###") ==
            false) // so unlikely, only if they named a library this, but just incase they did
            libraries.Add("###TOTAL###", total);
        return libraries;
    }

    /// <summary>
    /// Performance a search for library files
    /// </summary>
    /// <param name="filter">the search filter</param>
    /// <returns>a list of matching library files</returns>
    [HttpPost("search")]
    public Task<IEnumerable<LibraryFile>> Search([FromBody] LibraryFileSearchModel filter)
        => new LibraryFileService().Search(filter);


    /// <summary>
    /// Get a specific library file using cache
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    /// <returns>the library file instance</returns>
    internal Task<LibraryFile?> GetCached(Guid uid)
        => new LibraryFileService().Get(uid);
}
