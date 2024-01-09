using FileFlows.Plugin;
using FileFlows.Plugin.Models;
using FileFlows.Plugin.Services;
using FileHelper = FileFlows.Plugin.Helpers.FileHelper;

namespace FileFlows.ServerShared.FileServices;

public class LocalFileService : IFileService
{
    public char PathSeparator { get; init; } = Path.PathSeparator;

    public Result<string[]> GetFiles(string path, string searchPattern = "", bool recursive = false)
    {
        if (IsProtectedPath(path))
            return Result<string[]>.Fail("Cannot access protected path: " + path);
        try
        {
            return Directory.GetFiles(path, searchPattern,
                recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        }
        catch (Exception)
        {
            return new string[] { };
        }
    }

    public Result<string[]> GetDirectories(string path)
    {
        if (IsProtectedPath(path))
            return Result<string[]>.Fail("Cannot access protected path: " + path);
        try
        {
            return Directory.GetDirectories(path);
        }
        catch (Exception)
        {
            return new string[] { };
        }
    }

    public Result<bool> DirectoryExists(string path)
    {
        if (IsProtectedPath(path))
            return Result<bool>.Fail("Cannot access protected path: " + path);
        try
        {
            return Directory.Exists(path);
        }
        catch (Exception)
        {
            return false;
        }
    }

    public Result<bool> DirectoryDelete(string path, bool recursive = false)
    {
        if (IsProtectedPath(path))
            return Result<bool>.Fail("Cannot access protected path: " + path);
        try
        {
            Directory.Delete(path, recursive);
            return true;
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex.Message);
        }
    }

    public Result<bool> DirectoryMove(string path, string destination)
    {
        if (IsProtectedPath(path))
            return Result<bool>.Fail("Cannot access protected path: " + path);
        if (IsProtectedPath(destination))
            return Result<bool>.Fail("Cannot access protected path: " + destination);
        try
        {
            Directory.Move(path, destination);
            return true;
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex.Message);
        }
    }

    public Result<bool> DirectoryCreate(string path)
    {
        if (IsProtectedPath(path))
            return Result<bool>.Fail("Cannot access protected path: " + path);
        try
        {
            var dirInfo = new DirectoryInfo(path);
            if (dirInfo.Exists == false)
                dirInfo.Create();
            return true;
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex.Message);
        }
    }

    public Result<bool> FileExists(string path)
    {
        if (IsProtectedPath(path))
            return Result<bool>.Fail("Cannot access protected path: " + path);
        try
        {
            return File.Exists(path);
        }
        catch (Exception)
        {
            return false;
        }
    }

    public Result<FileInformation> FileInfo(string path)
    {
        if (IsProtectedPath(path))
            return Result<FileInformation>.Fail("Cannot access protected path: " + path);
        try
        {
            FileInfo fileInfo = new FileInfo(path);

            return new FileInformation
            {
                CreationTime = fileInfo.CreationTime,
                CreationTimeUtc = fileInfo.CreationTimeUtc,
                LastWriteTime = fileInfo.LastWriteTime,
                LastWriteTimeUtc = fileInfo.LastWriteTimeUtc,
                Extension = fileInfo.Extension.TrimStart('.'),
                Name = fileInfo.Name,
                FullName = fileInfo.FullName,
                Length = fileInfo.Length,
                Directory = fileInfo.DirectoryName
            };
        }
        catch (Exception ex)
        {
            return Result<FileInformation>.Fail(ex.Message);
        }
    }

    public Result<bool> FileDelete(string path)
    {
        if (IsProtectedPath(path))
            return Result<bool>.Fail("Cannot access protected path: " + path);
        try
        {
            var fileInfo = new FileInfo(path);
            if(fileInfo.Exists)
                fileInfo.Delete();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public Result<long> FileSize(string path)
    {
        if (IsProtectedPath(path))
            return Result<long>.Fail("Cannot access protected path: " + path);
        try
        {
            var fileInfo = new FileInfo(path);
            if (fileInfo.Exists == false)
                return Result<long>.Fail("File does not exist");
            return fileInfo.Length;
        }
        catch (Exception ex)
        {
            return Result<long>.Fail(ex.Message);
        }
    }

    public Result<DateTime> FileCreationTimeUtc(string path)
    {
        if (IsProtectedPath(path))
            return Result<DateTime>.Fail("Cannot access protected path: " + path);
        try
        {
            var fileInfo = new FileInfo(path);
            if (fileInfo.Exists == false)
                return Result<DateTime>.Fail("File does not exist");
            return fileInfo.CreationTimeUtc;
        }
        catch (Exception ex)
        {
            return Result<DateTime>.Fail(ex.Message);
        }
    }

    public Result<DateTime> FileLastWriteTimeUtc(string path)
    {
        if (IsProtectedPath(path))
            return Result<DateTime>.Fail("Cannot access protected path: " + path);
        try
        {
            var fileInfo = new FileInfo(path);
            if (fileInfo.Exists == false)
                return Result<DateTime>.Fail("File does not exist");
            return fileInfo.LastWriteTimeUtc;
        }
        catch (Exception ex)
        {
            return Result<DateTime>.Fail(ex.Message);
        }
    }

    public Result<bool> FileMove(string path, string destination, bool overwrite)
    {
        if (IsProtectedPath(path))
            return Result<bool>.Fail("Cannot access protected path: " + path);
        if (IsProtectedPath(destination))
            return Result<bool>.Fail("Cannot access protected path: " + destination);
        try
        {
            var fileInfo = new FileInfo(path);
            if (fileInfo.Exists == false)
                return Result<bool>.Fail("File does not exist");
            fileInfo.MoveTo(destination, overwrite);
            return true;
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex.Message);
        }
    }

    public Result<bool> FileCopy(string path, string destination, bool overwrite)
    {
        if (IsProtectedPath(path))
            return Result<bool>.Fail("Cannot access protected path: " + path);
        if (IsProtectedPath(destination))
            return Result<bool>.Fail("Cannot access protected path: " + destination);
        try
        {
            var fileInfo = new FileInfo(path);
            if (fileInfo.Exists == false)
                return Result<bool>.Fail("File does not exist");
            fileInfo.CopyTo(destination, overwrite);
            return true;
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex.Message);
        }
    }

    public Result<bool> FileAppendAllText(string path, string text)
    {
        if (IsProtectedPath(path))
            return Result<bool>.Fail("Cannot access protected path: " + path);
        try
        {
            File.AppendAllText(path, text);
            return true;
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex.Message);
        }
    }

    public bool FileIsLocal(string path) => true;

    /// <summary>
    /// Gets the local path
    /// </summary>
    /// <param name="path">the path</param>
    /// <returns>the local path to the file</returns>
    public Result<string> GetLocalPath(string path)
        => Result<string>.Success(path);

    public Result<bool> Touch(string path)
    {
        if (IsProtectedPath(path))
            return Result<bool>.Fail("Cannot access protected path: " + path);
        
        if (DirectoryExists(path).Is(true))
        {
            try
            {
                Directory.SetLastWriteTimeUtc(path, DateTime.UtcNow);
                return true;
            }
            catch (Exception ex)
            {
                return Result<bool>.Fail("Failed to touch directory: " + ex.Message);
            }
        }
        
        try
        {
            if (File.Exists(path))
                File.SetLastWriteTimeUtc(path, DateTime.UtcNow);
            else
                File.Create(path);
            return true;
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail($"Failed to touch file: '{path}' => {ex.Message}");
        }
    }

    public Result<bool> SetCreationTimeUtc(string path, DateTime date)
    {
        if (IsProtectedPath(path))
            return Result<bool>.Fail("Cannot access protected path: " + path);
        try
        {
            if (!File.Exists(path))
                return Result<bool>.Fail("File not found.");

            File.SetCreationTimeUtc(path, date);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail($"Error setting creation time: {ex.Message}");
        }
    }

    public Result<bool> SetLastWriteTimeUtc(string path, DateTime date)
    {
        if (IsProtectedPath(path))
            return Result<bool>.Fail("Cannot access protected path: " + path);
        try
        {
            if (!File.Exists(path))
                return Result<bool>.Fail("File not found.");

            File.SetLastWriteTimeUtc(path, date);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail($"Error setting last write time: {ex.Message}");
        }
    }

    private bool IsProtectedPath(string path)
        => FileHelper.IsSystemDirectory(path);
}