namespace FileFlows.Server.Helpers;

/// <summary>
/// Helper for file operations
/// </summary>
public class FileHelper
{
    /// <summary>
    /// Creates a directory if it does not exist
    /// </summary>
    /// <param name="directory">the directory</param>
    /// <returns>true if the directory was created</returns>
    public static bool CreateDirectoryIfNotExists(string directory)
    {
        return Plugin.Helpers.FileHelper.CreateDirectoryIfNotExists(Logger.Instance, directory);
    }

    /// <summary>
    /// Moves a file 
    /// </summary>
    /// <param name="source">the source file to move</param>
    /// <param name="destination">the destination to move to</param>
    public static void MoveFile(string source, string destination)
        => File.Move(source, destination);
    
    /// <summary>
    /// Gets the file size of a directory and all its files
    /// </summary>
    /// <param name="path">The path of the directory</param>
    /// <returns>The directories total size</returns>
    public static long GetDirectorySize(string path)
    {
        try
        {
            DirectoryInfo dir = new DirectoryInfo(path);
            return dir.EnumerateFiles("*.*", SearchOption.AllDirectories).Sum(x => x.Length);
        }
        catch (Exception)
        {
            return 0;
        }
    }    
}
