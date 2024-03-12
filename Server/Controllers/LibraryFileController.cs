using Microsoft.AspNetCore.Mvc;
using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;
using FileFlows.Server.Helpers.ModelHelpers;
using FileFlows.Server.Services;
using FileFlows.Server.Workers;
using FileFlows.ServerShared.Models;
using FileFlows.Shared.Helpers;
using Humanizer;
using LibraryFileService = FileFlows.Server.Services.LibraryFileService;
using LibraryService = FileFlows.Server.Services.LibraryService;
using NodeService = FileFlows.Server.Services.NodeService;

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
        var service = ServiceLoader.Load<LibraryFileService>();
        var result = await service.GetNext(args.NodeName, args.NodeUid, args.NodeVersion, args.WorkerUid);
        if (result == null)
            return result;
        
        // don't add any logic here to clear the file etc.  
        // the internal processing node bypasses this call and call the service directly (as does debug testing)
        // only remote processing nodes make this call

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
        var service = ServiceLoader.Load<LibraryFileService>();
        var lfStatus = await service.GetStatus();
        var libraries = await ServiceLoader.Load<LibraryService>().GetAllAsync();
        List<LibraryFile> files = await service.GetAll(status, page * pageSize, pageSize, filter, allLibraries: libraries);
        if (string.IsNullOrWhiteSpace(filter) == false)
        {
            // need to get total number of items matching filter as well
            int total = await service.GetTotalMatchingItems(status, filter);
            HttpContext?.Response?.Headers?.TryAdd("x-total-items", total.ToString());
        }

        var nodeNames = (await ServiceLoader.Load<NodeService>().GetAllAsync()).ToDictionary(x => x.Uid, x => x.Name);
        return new()
        {
            Status = lfStatus,
            LibraryFiles = LibaryFileListModelHelper.ConvertToListModel(files, status, libraries, nodeNames)
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
    public Task<List<LibraryFile>> GetAll([FromQuery] FileStatus? status, [FromQuery] int skip = 0, [FromQuery] int top = 0)
        => ServiceLoader.Load<LibraryFileService>().GetAll(status, skip: skip, rows: top);

    /// <summary>
    /// Get next 10 upcoming files to process
    /// </summary>
    /// <returns>a list of upcoming files to process</returns>
    [HttpGet("upcoming")]
    public async Task<IActionResult> Upcoming()
    {
        var data = await ServiceLoader.Load<LibraryFileService>().GetAll(FileStatus.Unprocessed, rows: 10);
        var results = data.Select(x => new
        {
            x.Uid,
            x.Name,
            DisplayName = FileDisplayNameService.GetDisplayName(x.Name, x.RelativePath, x.LibraryName)
        });
        return Ok(results);
    }

    /// <summary>
    /// Gets the last 10 successfully processed files
    /// </summary>
    /// <returns>the last successfully processed files</returns>
    [HttpGet("recently-finished")]
    public async Task<IActionResult> RecentlyFinished()
    {
        var service = ServiceLoader.Load<LibraryFileService>();
        var libraries = await ServiceLoader.Load<LibraryService>().GetAllAsync();
        var processed = await service.GetAll(FileStatus.Processed, rows: 10, allLibraries: libraries);
        var failed = await service.GetAll(FileStatus.ProcessingFailed, rows: 10, allLibraries: libraries);
        var mapping = await service.GetAll(FileStatus.MappingIssue, rows: 10, allLibraries: libraries);
        var minDate = new DateTime(2020, 1, 1);
        var all = processed.Union(failed).Union(mapping).OrderByDescending(x => x.ProcessingEnded < minDate ? x.ProcessingStarted : x.ProcessingEnded).ToArray();
        if (all.Any() == false)
            return Ok(new object[] { });
        if (all.Length > 10)
            all = all.Take(10).ToArray();
        var data = all.Select(x =>
        {
            var when = x.ProcessingEnded.ToLocalTime().Humanize(false, DateTime.UtcNow);
            return new
            {
                x.Uid,
                DisplayName = FileDisplayNameService.GetDisplayName(x.Name, x.RelativePath, x.LibraryName),
                x.RelativePath,
                x.ProcessingEnded,
                When = when,
                x.OriginalSize,
                x.FinalSize,
                Status = (int)x.Status
            };
        });
        return Ok(data);
    }

    /// <summary>
    /// Gets the library status overview
    /// </summary>
    /// <returns>the library status overview</returns>
    [HttpGet("status")]
    public Task<List<LibraryStatus>> GetStatus()
        => ServiceLoader.Load<LibraryFileService>().GetStatus();


    /// <summary>
    /// Get a specific library file
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    /// <returns>the library file instance</returns>
    [HttpGet("{uid}")]
    public async Task<LibraryFile> Get(Guid uid)
    {
        // first see if the file is currently processing, if it is, return that in memory 
        var file = await ServiceLoader.Load<FlowRunnerService>().TryGetFile(uid) ?? 
                   await ServiceLoader.Load<LibraryFileService>().Get(uid);
        
        if(file != null && (file.Status == FileStatus.ProcessingFailed || file.Status == FileStatus.Processed))
        {
            if (LibraryFileLogHelper.HtmlLogExists(uid))
                return file;
            LibraryFileLogHelper.CreateHtmlOfLog(uid);
        }
        return file;
    }


    /// <summary>
    /// Update a library file
    /// </summary>
    /// <param name="file">The library file to update</param>
    /// <returns>The updated library file</returns>
    [HttpPut]
    public async Task<LibraryFile> Update([FromBody] LibraryFile file)
    {
        throw new Exception("Obsolete method");
        var existing = await ServiceLoader.Load<LibraryFileService>().Get(file.Uid);

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
        if(file.Flow != null && file.Flow.Uid != Guid.Empty && string.IsNullOrEmpty(file.Flow.Name) == false)
            existing.Flow = file.Flow;
        if(file.Library != null && file.Library.Uid == existing.Library.Uid)
            existing.Library = file.Library; // name may have changed and is being updated
        
        existing.DateCreated = file.DateCreated; // this can be changed if library file is unheld
        if(file.ProcessingEnded > new DateTime(2020, 1,1))
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
        if(file.ExecutedNodes?.Any() == true)
            existing.ExecutedNodes = file.ExecutedNodes ?? new List<ExecutedNode>();
        if (file.OriginalMetadata?.Any() == true)
            existing.OriginalMetadata = file.OriginalMetadata;
        if (file.FinalMetadata?.Any() == true)
            existing.FinalMetadata = file.FinalMetadata;
        
        var updated = await ServiceLoader.Load<LibraryFileService>().Update(existing);
        
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
    public Task<bool> SaveFullLog([FromRoute] Guid uid, [FromBody] string log)
        => ServiceLoader.Load<LibraryFileService>().SaveFullLog(uid, log);

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
        await ServiceLoader.Load<LibraryFileService>().MoveToTop(list);
    }


    /// <summary>
    /// Checks if a library file exists on the server
    /// </summary>
    /// <param name="uid">The Uid of the library file to check</param>
    /// <returns>true if exists, otherwise false</returns>
    [HttpGet("exists-on-server/{uid}")]
    public Task<bool> ExistsOnServer([FromRoute] Guid uid)
        => ServiceLoader.Load<LibraryFileService>().ExistsOnServer(uid);

    /// <summary>
    /// Delete library files from the system
    /// </summary>
    /// <param name="model">A reference model containing UIDs to delete</param>
    /// <returns>an awaited task</returns>
    [HttpDelete]
    public Task Delete([FromBody] ReferenceModel<Guid> model)
        => ServiceLoader.Load<LibraryFileService>().Delete(model?.Uids);

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
            await ServiceLoader.Load<LibraryFileService>().Delete(deleted.ToArray());

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
        => ServiceLoader.Load<LibraryFileService>().Reprocess(model.Uids);

    /// <summary>
    /// Unhold library files
    /// </summary>
    /// <param name="model">A reference model containing UIDs to unhold</param>
    /// <returns>an awaited task</returns>
    [HttpPost("unhold")]
    public Task Unhold([FromBody] ReferenceModel<Guid> model)
        => ServiceLoader.Load<LibraryFileService>().Unhold(model?.Uids ?? new Guid[]{});

    /// <summary>
    /// Toggles force processing 
    /// </summary>
    /// <param name="model">A reference model containing UIDs to toggle force on</param>
    /// <returns>an awaited task</returns>
    [HttpPost("toggle-force")]
    public Task ToggleForce([FromBody] ReferenceModel<Guid> model)
        => ServiceLoader.Load<LibraryFileService>().ToggleForce(model?.Uids ?? new Guid[]{});
    

    /// <summary>
    /// Force processing of files
    /// Used to force files that are currently out of schedule to be processed
    /// </summary>
    /// <param name="model">the items to process</param>
    /// <returns>an awaited task</returns>
    [HttpPost("force-processing")]
    public Task ForceProcessing([FromBody] ReferenceModel<Guid> model)
        => ServiceLoader.Load<LibraryFileService>().ForceProcessing(model?.Uids ?? new Guid[]{});


    /// <summary>
    /// Sets the status of files
    /// </summary>
    /// <param name="status">the status to set to</param>
    /// <param name="model">the items to set the status on</param>
    /// <returns>an awaited task</returns>
    [HttpPost("set-status/{status}")]
    public Task SetStatus([FromRoute] FileStatus status, [FromBody] ReferenceModel<Guid> model)
        => ServiceLoader.Load<LibraryFileService>().SetStatus(status, model?.Uids ?? new Guid[]{});


    /// <summary>
    /// Performance a search for library files
    /// </summary>
    /// <param name="filter">the search filter</param>
    /// <returns>a list of matching library files</returns>
    [HttpPost("search")]
    public Task<List<LibraryFile>> Search([FromBody] LibraryFileSearchModel filter)
        => ServiceLoader.Load<LibraryFileService>().Search(filter);


    /// <summary>
    /// Get a specific library file using cache
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    /// <returns>the library file instance</returns>
    internal Task<LibraryFile?> GetCached(Guid uid)
        => ServiceLoader.Load<LibraryFileService>().Get(uid);

    /// <summary>
    /// Downloads a library file
    /// </summary>
    /// <param name="uid">the UID of the library file</param>
    /// <param name="test">[Optional] if the file should be tested to see if it still exists and can be downloaded</param>
    /// <returns>the download</returns>
    [HttpGet("download/{uid}")]
    public async Task<IActionResult> Download([FromRoute] Guid uid, [FromQuery] bool test = false)
    {
        var file = await ServiceLoader.Load<LibraryFileService>().Get(uid);
        if (file == null)
            return NotFound("File not found.");
        string filePath = file.Name;
        if (System.IO.File.Exists(filePath) == false)
        {
            filePath = file.OutputPath;
            if (string.IsNullOrEmpty(filePath) || System.IO.File.Exists(filePath) == false)
                return NotFound("File not found.");
        }

        if (test)
            return Ok();
        
        var fileInfo = new FileInfo(filePath);
        var stream = fileInfo.OpenRead();
        return File(stream, "application/octet-stream", fileInfo.Name);
    }

    /// <summary>
    /// Processes a file or adds it to the queue to add to the system
    /// </summary>
    /// <param name="filename">the filename of the file to process</param>
    /// <returns></returns>
    [HttpPost("process-file")]
    public async Task<IActionResult> ProcessFile([FromQuery] string filename)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filename))
                return BadRequest("Filename not set");

            var service = ServiceLoader.Load<LibraryFileService>();
            var file = await service.GetFileIfKnown(filename);
            if (file != null)
            {
                if ((int)file.Status < 2)
                    return Ok(); // already in the queue or processing
                await service.Reprocess(file.Uid);
                return Ok();
            }

            // file not known, add to the queue
            var library = (await ServiceLoader.Load<LibraryService>().GetAllAsync()).Where(x => x.Enabled)
                .FirstOrDefault(x => filename.StartsWith(x.Path));
            if (library == null)
                return BadRequest("No library found for file: " + filename);
            var watchedLibraray = LibraryWorker.GetWatchedLibrary(library);
            if (watchedLibraray == null)
                return BadRequest("Library is not currently watched");

            watchedLibraray.QueueItem(filename);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
