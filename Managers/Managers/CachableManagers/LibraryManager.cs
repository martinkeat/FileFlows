namespace FileFlows.Managers;

/// <summary>
/// Service for communicating with FileFlows server for libraries
/// </summary>
public class LibraryManager : CachedManager<Library>
{
    /// <summary>
    /// Updates the last scanned of a library to now
    /// </summary>
    /// <param name="uid">the UID of the library</param>
    public async Task UpdateLastScanned(Guid uid)
    {
        DateTime lastScanned = DateTime.Now;
        if (UseCache)
        {
            var lib = await GetByUid(uid);
            if (lib == null)
                return;
            lib.LastScanned = lastScanned;
        }

        await DatabaseAccessManager.Instance.DbObjectManager.SetDataValue(uid, typeof(Library).FullName,
            nameof(Library.LastScanned), lastScanned);
    }
}