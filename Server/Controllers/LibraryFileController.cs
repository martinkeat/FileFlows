using FileFlows.Server.Authentication;
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
[FileFlowsAuthorize(UserRole.Files)]
public class LibraryFileController : Controller //ControllerStore<LibraryFile>
{
    private static CacheStore CacheStore = new();

    /// <summary>
    /// Lists all of the library files, only intended for the UI
    /// </summary>
    /// <param name="status">The status to list</param>
    /// <param name="page">The page to get</param>
    /// <param name="pageSize">The number of items to fetch</param>
    /// <param name="filter">[Optional] filter text</param>
    /// <param name="node">[Optional] node to filter by</param>
    /// <param name="library">[Optional] library to filter by</param>
    /// <param name="flow">[Optional] flow to filter by</param>
    /// <param name="sortBy">[Optional] sort by method</param>
    /// <returns>a slimmed down list of files with only needed information</returns>
    [HttpGet("list-all")]
    public async Task<LibraryFileDatalistModel> ListAll([FromQuery] FileStatus status, [FromQuery] int page = 0, 
        [FromQuery] int pageSize = 0, [FromQuery] string filter = null, [FromQuery] Guid? node = null, 
        [FromQuery] Guid? library = null, [FromQuery] Guid? flow = null, [FromQuery] FilesSortBy? sortBy = null)
    {
        var service = ServiceLoader.Load<LibraryFileService>();
        var lfStatus = await service.GetStatus();
        var libraries = await ServiceLoader.Load<LibraryService>().GetAllAsync();
        
        
        var allLibraries = (await ServiceLoader.Load<LibraryService>().GetAllAsync());
        
        var sysInfo = new LibraryFilterSystemInfo()
        {
            AllLibraries = allLibraries.ToDictionary(x => x.Uid, x => x),
            Executors = FlowRunnerService.Executors.Values.ToList(),
            LicensedForProcessingOrder = LicenseHelper.IsLicensed(LicenseFlags.ProcessingOrder)
        };
        var lfFilter = new LibraryFileFilter()
        {
            Status = status,
            Skip = page * pageSize,
            Rows = pageSize,
            Filter = filter,
            NodeUid = node,
            LibraryUid = library,
            FlowUid = flow,
            SortBy = sortBy,
            SysInfo = sysInfo,
        };
        
        List<LibraryFile> files = await service.GetAll(lfFilter);
        if (string.IsNullOrWhiteSpace(filter) == false || node != null || flow != null || library != null)
        {
            // need to get total number of items matching filter as well
            int total = await service.GetTotalMatchingItems(lfFilter);
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
    /// Basic node list
    /// In this controller in case the users only has access to files page
    /// </summary>
    /// <returns>node list</returns>
    [HttpGet("node-list")]
    public async Task<List<NodeInfo>> GetNodeList()
    {
        var nodes = await new NodeService().GetAllAsync();
        return nodes.Select(x => new NodeInfo()
        {
            Uid = x.Uid,
            Name = x.Name,
            OperatingSystem = x.OperatingSystem
        }).ToList();
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
    [FileFlowsAuthorize]
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
    [FileFlowsAuthorize]
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
            var date = x.ProcessingEnded.Year > 2000 ? x.ProcessingEnded : x.ProcessingStarted;
            var when = date.ToLocalTime().Humanize(false, DateTime.UtcNow);
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
    [FileFlowsAuthorize]
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
