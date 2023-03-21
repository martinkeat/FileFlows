using FileFlows.Server.Helpers;
using FileFlows.ServerShared.Models;
using FileFlows.Server.Controllers;
using FileFlows.Server.Database;
using FileFlows.ServerShared.Workers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// Cache for library files service
/// </summary>
public partial class LibraryFileService 
{
    /// <summary>
    /// Gets a library file if it is known
    /// </summary>
    /// <param name="uid">the UID of the library file</param>
    /// <returns>the library file, or null if not found</returns>
    public LibraryFile? GetByUid(Guid uid)
    {
        if (Data.TryGetValue(uid, out LibraryFile? file))
            return file;
        return null;
    }

    /// <summary>
    /// Gets a library file by its UID
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    /// <returns>The library file if found, otherwise null</returns>
    public Task<LibraryFile?> Get(Guid uid)
        => Task.FromResult(GetByUid(uid));

    /// <summary>
    /// Gets a library file if it is known
    /// </summary>
    /// <param name="path">the path of the library file</param>
    /// <returns>the library file if it is known</returns>
    public LibraryFile? GetFileIfKnown(string path)
        => Data.Where(x => x.Value.Name == path || x.Value.OutputPath == path)
            .Select(x => x.Value).FirstOrDefault();
    
    
    /// <summary>
    /// Gets a library file if it is known by its fingerprint
    /// </summary>
    /// <param name="fingerprint">the fingerprint of the library file</param>
    /// <returns>the library file if it is known</returns>
    public LibraryFile GetFileByFingerprint(string fingerprint)
        => Data.Where(x => x.Value.Fingerprint == fingerprint || x.Value.FinalFingerprint == fingerprint)
            .Select(x => x.Value).FirstOrDefault();

}