using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// Cache for library files service
/// </summary>
public partial class LibraryFileService 
{
    private static Dictionary<Guid, LibraryFile> Data = new Dictionary<Guid, LibraryFile>();

    static LibraryFileService()
    {
        if (Globals.IsUnitTesting == false)
            Refresh();
    }

    #if(DEBUG)
    /// <summary>
    /// Sets the cached data, only intended for unit tests
    /// </summary>
    /// <param name="data">the data</param>
    public void SetData(Dictionary<Guid, LibraryFile> data)
        => Data = data;
    #endif

    /// <summary>
    /// Refreshes the data
    /// </summary>
    public static void Refresh()
    {
        Logger.Instance.ILog("Refreshing LibraryFileService Cache");
        try
        {
            using var db = GetDbWithMappings().Result;
            var data = db.Db.Fetch<LibraryFile>("select * from LibraryFile");
            var dict = data.ToDictionary(x => x.Uid, x => x);
            Data = dict;

            Logger.Instance.ILog("Refreshed LibraryFileService Cache");
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Error refreshing LibraryFileService Cache: " + ex.Message + Environment.NewLine + ex.StackTrace);
        }
    }
    
    /// <summary>
    /// Adds a file to the data
    /// </summary>
    /// <param name="file">the file being added</param>
    private void AddFile(LibraryFile file)
        => Data.TryAdd(file.Uid, file);

    /// <summary>
    /// Updates a file 
    /// </summary>
    /// <param name="file">the file being updated</param>
    private void UpdateFile(LibraryFile file)
    {
        lock (Data)
        {
            if (Data.ContainsKey(file.Uid))
                Data[file.Uid] = file;
            else
                Data.Add(file.Uid, file);
        };
    }

    /// <summary>
    /// Remove files from the cache
    /// </summary>
    /// <param name="uids">UIDs to remove</param>
    private void Remove(params Guid[] uids)
    {
        lock (Data)
        {
            foreach (var uid in uids)
            {
                if (Data.ContainsKey(uid))
                    Data.Remove(uid);
            }
        }
    }

    /// <summary>
    /// Remove files from where the libraries match a given library
    /// </summary>
    /// <param name="uids">UIDs of the libraries</param>
    private void RemoveLibraries(params Guid[] uids)
        => Data = Data.Where(x =>
            x.Value.LibraryUid == null || uids.Contains(x.Value.LibraryUid.Value) == false
        ).ToDictionary(x => x.Key, x => x.Value);

    /// <summary>
    /// Deletes a file to the data
    /// </summary>
    /// <param name="file">the file being deleted</param>
    private void DeleteFile(LibraryFile file)
    {
        lock (Data)
        {
            if (Data.ContainsKey(file.Uid))
                Data.Remove(file.Uid);
        }
    }
}