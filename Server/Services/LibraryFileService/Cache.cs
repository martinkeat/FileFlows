using System.Collections.Concurrent;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// Cache for library files service
/// </summary>
public partial class LibraryFileService 
{
    private static ConcurrentDictionary<Guid, LibraryFile> Data = new ();

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
    {
        // Create a new ConcurrentDictionary to hold the data
        ConcurrentDictionary<Guid, LibraryFile> concurrentData = new ConcurrentDictionary<Guid, LibraryFile>(data);

        // Replace the original Data dictionary with the concurrentData
        Data = concurrentData;
    }
#endif

    /// <summary>
    /// Refreshes the data
    /// </summary>
    public static void Refresh()
    {
        Logger.Instance.ILog("Refreshing LibraryFileService Cache");
        try
        {
            using var db =  GetDbWithMappings().Result;
            var data = db.Db.FetchAsync<LibraryFile>("select * from LibraryFile").Result;
    
            // Create a new ConcurrentDictionary to hold the data
            ConcurrentDictionary<Guid, LibraryFile> concurrentData = new ConcurrentDictionary<Guid, LibraryFile>();

            // Populate the ConcurrentDictionary directly from the fetched data
            foreach (var item in data)
            {
                concurrentData.TryAdd(item.Uid, item);
            }

            // Replace the original Data dictionary with the concurrentData
            Data = concurrentData;

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
                Data.TryAdd(file.Uid, file);
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
                    Data.Remove(uid, out var lf);
            }
        }
    }

    /// <summary>
    /// Remove files from where the libraries match a given library
    /// </summary>
    /// <param name="uids">UIDs of the libraries</param>
    private void RemoveLibraries(params Guid[] uids)
    {
        // Create a new ConcurrentDictionary to hold the filtered data
        ConcurrentDictionary<Guid, LibraryFile> concurrentData = new ();

        // Filter and populate the ConcurrentDictionary
        foreach (var kvp in Data)
        {
            if (kvp.Value.LibraryUid == null || uids.Contains(kvp.Value.LibraryUid.Value) == false)
            {
                concurrentData.TryAdd(kvp.Key, kvp.Value);
            }
        }

        // Replace the original Data dictionary with the filtered concurrentData
        Data = concurrentData;
    }

    /// <summary>
    /// Deletes a file to the data
    /// </summary>
    /// <param name="file">the file being deleted</param>
    private void DeleteFile(LibraryFile file)
    {
        lock (Data)
        {
            if (Data.ContainsKey(file.Uid))
                Data.Remove(file.Uid, out var lf);
        }
    }
}