using FileFlows.ServerShared.Models;

namespace FileFlows.ServerShared.Services;

using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;

/// <summary>
/// Interface for communicating with FileFlows server for library files
/// </summary>
public interface ILibraryFileService
{
    /// <summary>
    /// Gets the next library file queued for processing
    /// </summary>
    /// <param name="nodeName">The name of the node requesting a library file</param>
    /// <param name="nodeUid">The UID of the node</param>
    /// <param name="nodeVersion">the version of the node</param>
    /// <param name="workerUid">The UID of the worker on the node</param>
    /// <returns>If found, the next library file to process, otherwise null</returns>
    Task<NextLibraryFileResult> GetNext(string nodeName, Guid nodeUid, string nodeVersion, Guid workerUid);

    /// <summary>
    /// Gets a library file by its UID
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    /// <returns>The library file if found, otherwise null</returns>
    Task<LibraryFile?> Get(Guid uid);

    /// <summary>
    /// Deletes library files
    /// </summary>
    /// <param name="uids">a list of UIDs to delete</param>
    /// <returns>a completed task</returns>
    Task Delete(params Guid[] uids);

    /// <summary>
    /// Updates a library file
    /// </summary>
    /// <param name="libraryFile">The library file to update</param>
    /// <returns>The newly updated library file</returns>
    Task<LibraryFile> Update(LibraryFile libraryFile);

    /// <summary>
    /// Saves the full library file log
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    /// <param name="log">The full plain text log to save</param>
    /// <returns>If it was successfully saved or not</returns>
    Task<bool> SaveFullLog(Guid uid, string log);
    
    /// <summary>
    /// Tests if a library file exists on server.
    /// This is used to test if a mapping issue exists on the node, and will be called if a Node cannot find the library file
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    /// <returns>True if it exists on the server, otherwise false</returns>
    Task<bool> ExistsOnServer(Guid uid);
}

/// <summary>
/// Service for communicating with FileFlows server for library files
/// </summary>
public class LibraryFileService : Service, ILibraryFileService
{

    /// <summary>
    /// Gets or sets a function to load an instance of ILibraryFileService by the Load function
    /// </summary>
    public static Func<ILibraryFileService> Loader { get; set; }

    /// <summary>
    /// Loads an instance of the ILibraryFileService
    /// </summary>
    /// <returns>an instance of the ILibraryFileService</returns>
    public static ILibraryFileService Load()
    {
        if (Loader == null)
            return new LibraryFileService();
        return Loader.Invoke();
    }

    /// <summary>
    /// Deletes library files
    /// </summary>
    /// <param name="uids">The UIDs to delete</param>
    /// <returns>a completed task</returns>
    public async Task Delete(params Guid[] uids)
    {
        try
        {
            var result = await HttpHelper.Delete($"{ServiceBaseUrl}/api/library-file", new ReferenceModel<Guid> { Uids = uids });                
        }
        catch (Exception)
        {
            return;
        }
    }

    /// <summary>
    /// Tests if a library file exists on server.
    /// This is used to test if a mapping issue exists on the node, and will be called if a Node cannot find the library file
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    /// <returns>True if it exists on the server, otherwise false</returns>
    public async Task<bool> ExistsOnServer(Guid uid)
    {
        try
        {
            var result = await HttpHelper.Get<bool>($"{ServiceBaseUrl}/api/library-file/exists-on-server/{uid}");
            if (result.Success == false)
                return false;
            return result.Data;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Gets a library file by its UID
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    /// <returns>The library file if found, otherwise null</returns>
    public async Task<LibraryFile?> Get(Guid uid)
    {
        try
        {
            var result = await HttpHelper.Get<LibraryFile>($"{ServiceBaseUrl}/api/library-file/{uid}");
            if (result.Success == false)
                return null;
            return result.Data;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the next library file queued for processing
    /// </summary>
    /// <param name="nodeName">The name of the node requesting a library file</param>
    /// <param name="nodeUid">The UID of the node</param>
    /// <param name="nodeVersion">the version of the node</param>
    /// <param name="workerUid">The UID of the worker on the node</param>
    /// <returns>If found, the next library file to process, otherwise null</returns>
    public async Task<NextLibraryFileResult> GetNext(string nodeName, Guid nodeUid, string nodeVersion, Guid workerUid)
    {
        // can throw exception if nothing to process
        try
        {
            var result = await HttpHelper.Post<NextLibraryFileResult>($"{ServiceBaseUrl}/api/library-file/next-file", new NextLibraryFileArgs
            {
                NodeName = nodeName,
                NodeUid = nodeUid,
                WorkerUid = workerUid,
                NodeVersion = Globals.Version.ToString()
            });
            if (result.Success == false)
                return null; 
            return result.Data;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Saves the full library file log
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    /// <param name="log">The full plain text log to save</param>
    /// <returns>If it was successfully saved or not</returns>
    public async Task<bool> SaveFullLog(Guid uid, string log)
    {
        try
        {
            var result = await HttpHelper.Put<bool>($"{ServiceBaseUrl}/api/library-file/{uid}/full-log", log);
            if (result.Success)
                return result.Data;
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Updates a library file
    /// </summary>
    /// <param name="libraryFile">The library file to update</param>
    /// <returns>The newly updated library file</returns>
    public async Task<LibraryFile> Update(LibraryFile libraryFile)
    {
        var result = await HttpHelper.Put<LibraryFile>($"{ServiceBaseUrl}/api/library-file", libraryFile);
        if (result.Success == false)
            throw new Exception("Failed to update library file: " + result.Body);
        return result.Data;
    }
}
