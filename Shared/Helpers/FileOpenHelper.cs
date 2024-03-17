using System.IO;

/// <summary>
/// Helper class for file open operations
/// </summary>
public static class FileOpenHelper
{
    /// <summary>
    /// Opens a file for reading without a read/write lock
    /// This allows other applications to read/write to the file while it is being read
    /// </summary>
    /// <param name="file">the file to open</param>
    /// <returns>the file stream</returns>
    public static FileStream OpenRead_NoLocks(string file)
        => File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

    /// <summary>
    /// Opens a file for reading and writing without a read/write lock
    /// This allows other applications to keep reading/writing the file while it's being opened
    /// This is useful to check if CanRead/CanWrite is true on a file
    /// Actually writing to the file with this stream might have undesired effects
    /// </summary>
    /// <param name="file">the file to open</param>
    /// <returns>the file stream</returns>
    public static FileStream OpenForCheckingReadWriteAccess(string file)
        => File.Open(file, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

    /// <summary>
    /// Opens a file for writing without a write lock
    /// This allows other applications to read the file while it is being written to
    /// </summary>
    public static FileStream OpenWrite_NoReadLock(string file, FileMode fileMode)
        => File.Open(file, fileMode, FileAccess.Write, FileShare.Read);
}
