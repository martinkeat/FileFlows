using FileFlows.Plugin.Models;

namespace FileFlows.Plugin.Services;


/// <summary>
/// The File Service
/// </summary>
public class FileService
{
    /// <summary>
    /// Gets a singleton instance of a file service
    /// </summary>
    public static IFileService Instance { get; set; }
}

/// <summary>
/// Interface for interacting with the file system
/// This is needed in case the file system is remote
/// This does not download or upload files, it queries the file system
/// </summary>
public interface IFileService
{
    /// <summary>
    /// Gets the path separator for this file system
    /// </summary>
    char PathSeparator { get; init; }
    
    /// <summary>
    /// Gets or sets a function for replacing variables in a string.
    /// </summary>
    /// <remarks>
    /// The function takes a string input, a boolean indicating whether to strip missing variables,
    /// and a boolean indicating whether to clean special characters.
    /// </remarks>
    ReplaceVariablesDelegate ReplaceVariables { get; set; }
    
    /// <summary>
    /// Gets files from a directory
    /// </summary>
    /// <param name="path">the path of the directory</param>
    /// <param name="searchPattern">[Optional] search pattern</param>
    /// <param name="recursive">[Optional] if all folders should be searched and all sub files returned</param>
    /// <returns>a list of files</returns>
    Result<string[]> GetFiles(string path, string searchPattern = "", bool recursive = false);
    
    /// <summary>
    /// Gets sub folders from a directory
    /// </summary>
    /// <param name="path">the path of the directory</param>
    /// <returns>a list of sub folders</returns>
    Result<string[]> GetDirectories(string path);

    /// <summary>
    /// Checks if a directory exists
    /// </summary>
    /// <param name="path">the path to the directory</param>
    /// <returns>true if exists, otherwise false</returns>
    Result<bool> DirectoryExists(string path);
    
    /// <summary>
    /// Deletes a directory
    /// </summary>
    /// <param name="path">the path to the file</param>
    /// <param name="recursive">true to delete this directory, its subdirectories, and all files; otherwise, false</param> 
    /// <returns>true if successful or false if not</returns>
    Result<bool> DirectoryDelete(string path, bool recursive = false);
    
    /// <summary>
    /// Moves a directory
    /// </summary>
    /// <param name="path">the path to the directory</param>
    /// <param name="destination">the destination of the directory</param>
    /// <returns>true if successful or false if not</returns>
    Result<bool> DirectoryMove(string path, string destination);
    
    /// <summary>
    /// Creates a directory if one does not yet exist
    /// </summary>
    /// <param name="path">the path to the directory</param>
    /// <returns>true if successful or false if not</returns>
    Result<bool> DirectoryCreate(string path);

    /// <summary>
    /// Checks if a file exists
    /// </summary>
    /// <param name="path">the path to the file</param>
    /// <returns>true if exists, otherwise false</returns>
    Result<bool> FileExists(string path);

    /// <summary>
    /// Gets file information for a given file
    /// </summary>
    /// <param name="path">the path to the file</param>
    /// <returns>the file information</returns>
    Result<FileInformation> FileInfo(string path);
    
    /// <summary>
    /// Deletes a file
    /// </summary>
    /// <param name="path">the path to the file</param>
    /// <returns>true if successful or false if not</returns>
    Result<bool> FileDelete(string path);

    /// <summary>
    /// Gets the size of a file
    /// </summary>
    /// <param name="path">the path to the file</param>
    /// <returns>the size of the file</returns>
    Result<long> FileSize(string path);

    /// <summary>
    /// Gets the creation time of a file
    /// </summary>
    /// <param name="path">the path to the file</param>
    /// <returns>the creation time of the file</returns>
    Result<DateTime> FileCreationTimeUtc(string path);

    /// <summary>
    /// Gets the last write time of a file
    /// </summary>
    /// <param name="path">the path to the file</param>
    /// <returns>the last write time of the file</returns>
    Result<DateTime> FileLastWriteTimeUtc(string path);
    
    /// <summary>
    /// Moves a file
    /// </summary>
    /// <param name="path">the path to the file</param>
    /// <param name="destination">the destination of the file</param>
    /// <param name="overwrite">if the destination should be overwritten if it exists</param>
    /// <returns>true if successful or false if not</returns>
    Result<bool> FileMove(string path, string destination, bool overwrite = true);

    /// <summary>
    /// Copies a file
    /// </summary>
    /// <param name="path">the path of the file to copy</param>
    /// <param name="destination">the destination to move the file</param>
    /// <param name="overwrite">if the destination should be overwritten if it exists</param>
    /// <returns>true if successfully copied or not</returns>
    Result<bool> FileCopy(string path, string destination, bool overwrite = true);

    /// <summary>
    /// Appends the specified text to the file at the given path.
    /// </summary>
    /// <param name="path">The path to the file to which the text will be appended.</param>
    /// <param name="text">The text to append to the file.</param>
    /// <returns>A <see cref="Result{T}"/> indicating success (true) or failure (false).</returns>
    Result<bool> FileAppendAllText(string path, string text);

    /// <summary>
    /// Checks if a file is local to the current system or not
    /// </summary>
    /// <param name="path">the path to the file</param>
    /// <returns>true if local, otherwise false</returns>
    bool FileIsLocal(string path);

    /// <summary>
    /// Gets the local path to the file
    /// If the file is remote, it should be downloaded to the local temp directory of the runner
    /// If the file is not remote, it should just return the current path 
    /// </summary>
    /// <param name="path">the path to the file</param>
    /// <returns>the local path</returns>
    Result<string> GetLocalPath(string path);

    /// <summary>
    /// Touches a file or directory
    /// </summary>
    /// <param name="path">the path of the file or directory</param>
    /// <returns>true if successfully touched or not</returns>
    Result<bool> Touch(string path);

    Result<bool> SetCreationTimeUtc(string path, DateTime date);
    Result<bool> SetLastWriteTimeUtc(string path, DateTime date);
}


/// <summary>
/// Represents a delegate for the function that replaces variables in a string.
/// </summary>
/// <param name="input">The input string containing variables to be replaced.</param>
/// <param name="stripMissing">A boolean indicating whether to strip missing variables.</param>
/// <param name="cleanSpecialCharacters">A boolean indicating whether to clean special characters.</param>
/// <returns>The string after replacing variables according to the specified conditions.</returns>
public delegate string ReplaceVariablesDelegate(string input, bool stripMissing = false, bool cleanSpecialCharacters = false);