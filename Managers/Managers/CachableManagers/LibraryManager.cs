namespace FileFlows.Managers;

/// <summary>
/// Manager for the libraries
/// </summary>
public class LibraryManager : CachedManager<Library>
{
    /// <inheritdoc />
    protected override bool SaveRevisions => true;
    
    /// <summary>
    /// Updates the last scanned of a library to now
    /// </summary>
    /// <param name="uid">the UID of the library</param>
    public async Task UpdateLastScanned(Guid uid)
    {
        DateTime lastScanned = DateTime.UtcNow;
        if (UseCache)
        {
            var lib = await GetByUid(uid);
            if (lib == null)
                return;
            lib.LastScanned = lastScanned;
        }

        await DatabaseAccessManager.Instance.ObjectManager.SetDataValue(uid, typeof(Library).FullName,
            nameof(Library.LastScanned), lastScanned);
    }

    /// <summary>
    /// Gets if there are any libraries in the system
    /// </summary>
    /// <returns>true if there are some, otherwise false</returns>
    public Task<bool> HasAny()
        => DatabaseAccessManager.Instance.ObjectManager.Any(typeof(Library).FullName!);


    /// <summary>
    /// Updates all libraries with the new flow name if they used this flow
    /// </summary>
    /// <param name="uid">the UID of the flow</param>
    /// <param name="name">the new name of the flow</param>
    /// <returns>a task to await</returns>
    public async Task UpdateFlowName(Guid uid, string name)
    {
        await DatabaseAccessManager.Instance.ObjectManager.UpdateAllObjectReferences(nameof(Library.Flow), uid, name);
        if (UseCache == false || _Data?.Any() != true)
            return;
        foreach (var d in _Data)
        {
            if (d.Flow?.Uid == uid)
                d.Flow.Name = name;
        }
    }
}