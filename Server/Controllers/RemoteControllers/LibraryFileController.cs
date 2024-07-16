using FileFlows.Plugin;
using FileFlows.Server.Authentication;
using FileFlows.Server.Helpers;
using FileFlows.Server.Services;
using FileFlows.ServerShared.Models;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace FileFlows.Server.Controllers.RemoteControllers;

/// <summary>
/// System remote controller
/// </summary>
[Route("/remote/library-file")]
[FileFlowsApiAuthorize]
[ApiExplorerSettings(IgnoreApi = true)]
public class LibraryFileController : Controller
{
    /// <summary>
    /// The semaphore to ensure only one file is requested at a time
    /// </summary>
    private static FairSemaphore nextFileSemaphore = new (1);

    private static Logger NextFileLogger;
    
    /// <summary>
    /// Get a specific library file
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    /// <returns>the library file instance</returns>
    [HttpGet("{uid}")]
    public async Task<LibraryFile> GetLibraryFile(Guid uid)
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
    /// Gets the next library file for processing, and puts it into progress
    /// </summary>
    /// <param name="args">The arguments for the call</param>
    /// <returns>the next library file to process</returns>
    [HttpPost("next-file")]
    public async Task<NextLibraryFileResult> GetNextLibraryFile([FromBody] NextLibraryFileArgs args)
    {
        await nextFileSemaphore.WaitAsync();
        try
        {
            if (NextFileLogger == null)
            {
                NextFileLogger = new();
                NextFileLogger.RegisterWriter(new FileLogger(DirectoryHelper.LoggingDirectory, "FileProcessRequest", false));
            }
            var service = ServiceLoader.Load<LibraryFileService>();
            var result = await service.GetNext(NextFileLogger, args.NodeName, args.NodeUid, args.NodeVersion, args.WorkerUid);
            if (result == null)
                return result;

            // don't add any logic here to clear the file etc.  
            // the internal processing node bypasses this call and call the service directly (as does debug testing)
            // only remote processing nodes make this call

            Logger.Instance.ILog($"GetNextFile for ['{args.NodeName}']({args.NodeUid}): {result.Status}");

            if (result.File != null)
            {
                // record that this has started now, its not the complete start, but the flow runner has request it
                // by recording this now, we add the flow running extremely early into the life cycle and we can 
                // then limit the library runners, and wont have issues with "Unknown executor identifier" when using the file server
                FlowRunnerService.Executors[result.File.Uid] = new()
                {
                    Uid = result.File.Uid,
                    LibraryFile = result.File,
                    NodeName = args.NodeName,
                    NodeUid = args.NodeUid,
                    IsRemote = args.NodeUid != Globals.InternalNodeUid,
                    RelativeFile = result.File.RelativePath,
                    Library = result.File.Library,
                    IsDirectory = result.File.IsDirectory,
                    StartedAt = DateTime.UtcNow
                };
            }

            return result;
        }
        finally
        {
            nextFileSemaphore.Release();
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
    /// Checks if a library file exists on the server
    /// </summary>
    /// <param name="uid">The Uid of the library file to check</param>
    /// <returns>true if exists, otherwise false</returns>
    [HttpGet("exists-on-server/{uid}")]
    public Task<bool> ExistsOnServer([FromRoute] Guid uid)
        => ServiceLoader.Load<LibraryFileService>().ExistsOnServer(uid);
    
    
    /// <summary>
    /// Delete a library files from disk and the database
    /// </summary>
    /// <param name="uid">the UID of the file to delete</param>
    /// <returns>an awaited task</returns>
    [HttpDelete("{uid}")]
    public async Task<string> DeleteFile([FromRoute] Guid uid)
    {
        bool failed = false;
        var lf = await GetLibraryFile(uid);
        if (lf == null)
            return string.Empty;
        
        if (System.IO.File.Exists(lf.Name))
        {
            if (DeleteFile(lf.Name) == false)
            {
                failed = true;
            }
        }

        await ServiceLoader.Load<LibraryFileService>().Delete(uid);

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
}