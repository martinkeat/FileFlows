namespace FileFlows.Plugin.Helpers;

/// <summary>
/// Archive helper responsible for managing archives/compressed files
/// </summary>
public interface IArchiveHelper
{
    /// <summary>
    /// Checks if a file exist in the archive
    /// </summary>
    /// <param name="archivePath">the path to the archive</param>
    /// <param name="file">the file to look for</param>
    /// <returns>true if exists, otherwise false</returns>
    Result<bool> FileExists(string archivePath, string file);
    
    /// <summary>
    /// Adds a file to the archive
    /// </summary>
    /// <param name="archivePath">the path to the archive</param>
    /// <param name="file">the file to add</param>
    Result<bool> AddToArchive(string archivePath, string file);

    /// <summary>
    /// Gets the total number of files matching the pattern
    /// </summary>
    /// <param name="archivePath">the path to the archive</param>
    /// <param name="pattern">the regular expression pattern to match the files against</param>
    /// <returns>the number of files</returns>
    Result<int> GetFileCount(string archivePath, string pattern);

    /// <summary>
    /// Zips a folder to a file
    /// </summary>
    /// <param name="path">the path to the file or folder to compress</param>
    /// <param name="output">the output file of the zip</param>
    /// <param name="pattern">the file pattern to include in the zip</param>
    /// <param name="allDirectories">If all directories should be included or just the top most</param>
    /// <param name="percentCallback">the callback to update with the percent complete</param>
    /// <returns>true if successful, otherwise false</returns>
    Result<bool> Compress(string path, string output, string pattern = "",
        bool allDirectories = false, Action<float>? percentCallback = null);

    /// <summary>
    /// Extracts a file
    /// </summary>
    /// <param name="archivePath">the path to the archive</param>
    /// <param name="destinationPath">the location to extract the file to</param>
    /// <param name="percentCallback">the callback to update with the percent complete</param>
    /// <returns>true if successful, otherwise false</returns>
    Result<bool> Extract(string archivePath, string destinationPath, Action<float>? percentCallback = null);
}