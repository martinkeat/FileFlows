using FileFlows.Plugin;
using FileFlows.Plugin.Helpers;
using FileFlows.Plugin.Models;
using FileFlows.Plugin.Services;
using FileFlows.Shared.Helpers;
using FileHelper = FileFlows.Plugin.Helpers.FileHelper;

namespace FileFlows.ServerShared.FileServices;

public class RemoteFileService : IFileService
{
    public char PathSeparator { get; init; }

    private readonly Guid executorUid;
    private readonly string serverUrl;
    private readonly string tempPath;
    private readonly ILogger logger;
    private static HttpClient _Client;
    private readonly LocalFileService _localFileService;

    public RemoteFileService(Guid executorUid, string serverUrl, string tempPath, ILogger logger)
    {
        this.executorUid = executorUid;
        this.serverUrl = serverUrl;
        this.tempPath = tempPath;
        this.logger = logger;
        this._localFileService = new();
        HttpHelper.OnHttpRequestCreated = OnHttpRequestCreated;
    }

    private void OnHttpRequestCreated(HttpRequestMessage request)
    {
        request.Headers.Add("x-executor", executorUid.ToString());
    }

    private string GetUrl(string route)
    {
        return serverUrl + "/api/file-server/" + route;
    }

    public Result<string[]> GetFiles(string path, string searchPattern = "", bool recursive = false)
    {
        if (FileIsLocal(path))
            return _localFileService.GetFiles(path, searchPattern, recursive);
        try
        {
            var result = HttpHelper.Post<string[]>(GetUrl("list-files"), new
            {
                path,
                searchPattern,
                recursive
            }).Result;
            return result.Data ?? new string[] { };
        }
        catch (Exception)
        {
            return new string[] { };
        }
    }

    public Result<string[]> GetDirectories(string path)
    {
        if (FileIsLocal(path))
            return _localFileService.GetDirectories(path);
        try
        {
            var result = HttpHelper.Post<string[]>(GetUrl("list-directories"), new { path }).Result;
            return result.Data ?? new string[] { };
        }
        catch (Exception)
        {
            return new string[] { };
        }
    }

    public Result<bool> DirectoryExists(string path)
    {
        if (FileIsLocal(path))
            return _localFileService.DirectoryExists(path);
        try
        {
            var result = HttpHelper.Post<bool>(GetUrl("directory/exists"), new { path }).Result;
            return result.Data == true;
        }
        catch (Exception)
        {
            return false;
        }
    }
    public Result<bool> DirectoryDelete(string path, bool recursive = false)
    {
        if (FileIsLocal(path))
            return _localFileService.DirectoryDelete(path, recursive);
        try
        {
            var result = HttpHelper.Post<bool>(GetUrl("directory/delete"), new
            {
                path,
                recursive
            }).Result;
            return result.Data;
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail("Failed to delete directory: " + ex.Message);
        }
    }

    public Result<bool> DirectoryMove(string path, string destination)
    {
        if (FileIsLocal(path))
        {
            if(FileIsLocal(destination))
                return _localFileService.DirectoryMove(path, destination);
            return Result<bool>.Fail("Cannot move temporary directory to remote host");
        }
        
        if(FileIsLocal(destination) && FileIsLocal(path) == false)
            return Result<bool>.Fail("Cannot move remote directory to local host");

        try
        {
            var result = HttpHelper.Post<bool>(GetUrl("directory/move"), new
            {
                path,
                destination
            }).Result;
            return result.Data;
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail("Failed to move directory: " + ex.Message);
        }
    }

    public Result<bool> DirectoryCreate(string path)
    {
        if (FileIsLocal(path))
            return _localFileService.DirectoryCreate(path);
        try
        {
            var result = HttpHelper.Post<bool>(GetUrl("directory/create"), new { path }).Result;
            return result.Data;
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail("Failed to create directory: " + ex.Message);
        }
    }

    public Result<bool> FileExists(string path)
    {
        if (FileIsLocal(path))
            return _localFileService.FileExists(path);
        try
        {
            var result = HttpHelper.Post<bool>(GetUrl("file/exists"), new { path }).Result;
            return result.Data == true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public bool FileIsLocal(string path)
    {
        if (path.StartsWith(tempPath))
            return true;
        return false;
    }

    public Result<string> GetLocalPath(string path)
    {
        if (FileIsLocal(path))
            return path;
        string filename = Path.Combine(tempPath, FileHelper.GetShortFileName(path));
        if (File.Exists(filename))
            return filename;
        var result = new FileDownloader(logger, serverUrl, executorUid).DownloadFile(path, filename).Result;
        if (result.IsFailed)
            return Result<string>.Fail(result.Error);
        return filename;
    }

    public Result<bool> Touch(string path)
    {
        if (FileIsLocal(path))
            return _localFileService.Touch(path);
        try
        {
            var result = HttpHelper.Post<bool>(GetUrl("touch"), new { path }).Result;
            return result.Data;
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail("Failed touching file: " + ex.Message);
        }
    }

    public Result<FileInformation> FileInfo(string path)
    {
        if (FileIsLocal(path))
            return _localFileService.FileInfo(path);
        try
        {
            var result = HttpHelper.Post<FileInformation>(GetUrl("file/info"), new { path }).Result;
            return result.Data;
        }
        catch (Exception ex)
        {
            return Result<FileInformation>.Fail("Failed to get file information: " + ex.Message);
        }
    }

    public Result<bool> FileDelete(string path)
    {
        if (FileIsLocal(path))
            return _localFileService.FileDelete(path);
        try
        {
            var result = HttpHelper.Post<bool>(GetUrl("file/delete"), new { path }).Result;
            return result.Data;
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail("Failed to delete file: " + ex.Message);
        }
    }

    public Result<long> FileSize(string path)
    {
        if (FileIsLocal(path))
            return _localFileService.FileSize(path);
        try
        {
            var result = HttpHelper.Post<long>(GetUrl("file/size"), new { path }).Result;
            return result.Data;
        }
        catch (Exception ex)
        {
            return Result<long>.Fail("Failed to get file size: " + ex.Message);
        }
    }

    public Result<DateTime> FileCreationTimeUtc(string path)
    {
        if (FileIsLocal(path))
            return _localFileService.FileCreationTimeUtc(path);
        try
        {
            var result = HttpHelper.Post<DateTime>(GetUrl("file/creation-time-utc"), new { path }).Result;
            return result.Data;
        }
        catch (Exception ex)
        {
            return Result<DateTime>.Fail("Failed to get file creation time (UTC): " + ex.Message);
        }
    }

    public Result<DateTime> FileLastWriteTimeUtc(string path)
    {
        if (FileIsLocal(path))
            return _localFileService.FileLastWriteTimeUtc(path);
        try
        {
            var result = HttpHelper.Post<DateTime>(GetUrl("file/last-write-time-utc"), new { path }).Result;
            return result.Data;
        }
        catch (Exception ex)
        {
            return Result<DateTime>.Fail("Failed to get file last write time (UTC): " + ex.Message);
        }
    }

    public Result<bool> FileMove(string path, string destination, bool overwrite)
    {
        if (FileIsLocal(path))
        {
            if(FileIsLocal(destination))
                return _localFileService.FileMove(path, destination, overwrite);
            var result = new FileUploader(logger, serverUrl, executorUid).UploadFile(path, destination).Result;
            if (result.Success == false)
                return Result<bool>.Fail(result.Error);
            FileDelete(path);
            return true;
        }
        
        try
        {
            logger.ILog("Moving file via RemoteFileService file/move: " + path);
            var result = HttpHelper.Post<bool>(GetUrl("file/move"), new
            {
                path,
                destination,
                overwrite
            }).Result;
            return result.Data;
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail("Failed to move file: " + ex.Message);
        }
    }

    public Result<bool> FileCopy(string path, string destination, bool overwrite)
    {
        if (FileIsLocal(path))
        {
            if(FileIsLocal(destination))
                return _localFileService.FileCopy(path, destination, overwrite);
            var result = new FileUploader(logger, serverUrl, executorUid).UploadFile(path, destination).Result;
            if (result.Success == false)
                return Result<bool>.Fail(result.Error);
            return true;
        }

        if (FileIsLocal(destination))
        {
            // download the file
            var result = new FileDownloader(logger, serverUrl, executorUid).DownloadFile(path, destination).Result;
            if (result.IsFailed)
                return Result<bool>.Fail(result.Error);
            return true;
            
        }

        try
        {
            var result = HttpHelper.Post<bool>(GetUrl("file/copy"), new
            {
                path,
                destination,
                overwrite
            }).Result;
            return result.Data;
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail("Failed to copy file: " + ex.Message);
        }
    }

    public Result<bool> FileAppendAllText(string path, string text)
    {
        if (FileIsLocal(path))
            return _localFileService.FileAppendAllText(path, text);
        try
        {
            var result = HttpHelper.Post<bool>(GetUrl("file/append-text"), new
            {
                path,
                text
            }).Result;
            return result.Data;
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail("Failed to append text to file: " + ex.Message);
        }
    }

    public Result<bool> SetCreationTimeUtc(string path, DateTime date)
    {
        if (FileIsLocal(path))
            return _localFileService.SetCreationTimeUtc(path, date);
        try
        {
            var result = HttpHelper.Post<bool>(GetUrl("file/set-creation-time-utc"), new
            {
                path,
                date
            }).Result;
            return result.Data;
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail("Failed to set file creation time (UTC): " + ex.Message);
        }
    }

    public Result<bool> SetLastWriteTimeUtc(string path, DateTime date)
    {
        if (FileIsLocal(path))
            return _localFileService.SetLastWriteTimeUtc(path, date);
        try
        {
            var result = HttpHelper.Post<bool>(GetUrl("file/set-last-write-time-utc"), new
            {
                path,
                date
            }).Result;
            return result.Data;
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail("Failed to set file last write time (UTC): " + ex.Message);
        }
    }

}