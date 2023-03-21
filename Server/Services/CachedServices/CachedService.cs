using System.Runtime.CompilerServices;
using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// Services that caches its objects
/// </summary>
public abstract class CachedService<T> where T : FileFlowObject, new()
{
    /// <summary>
    /// Gets if this service increments the system configuration revision number when changes to the data happens
    /// </summary>
    public virtual bool IncrementsConfiguration => true;

    private static List<T> _Data;
    /// <summary>
    /// Gets or sets the data
    /// </summary>
    protected List<T> Data
    {
        get
        {
            if(_Data == null)
                Refresh();
            return _Data;
        }
        set => _Data = value;
    }

    /// <summary>
    /// Sets the data
    /// </summary>
    /// <param name="data">the data to set</param>
    internal static void SetData(List<T> data)
    {
        _Data = data ?? new List<T>();
    }

    /// <summary>
    /// Gets the data
    /// </summary>
    /// <returns>the data</returns>
    public List<T> GetAll() => Data;

    /// <summary>
    /// Gets all the data async
    /// </summary>
    /// <returns>the data</returns>
    public Task<List<T>> GetAllAsync() => Task.FromResult(GetAll());

    /// <summary>
    /// Gets an item by its UID
    /// </summary>
    /// <param name="uid">the UID of the item</param>
    /// <returns>the item</returns>
    public T? GetByUid(Guid uid)
        => Data.FirstOrDefault(x => x.Uid == uid);

    /// <summary>
    /// Gets an item by its UID async
    /// </summary>
    /// <param name="uid">the UID of the item</param>
    /// <returns>the item</returns>
    public Task<T> GetByUidAsync(Guid uid) => Task.FromResult(GetByUid(uid)!);

    /// <summary>
    /// Gets a item by it's name
    /// </summary>
    /// <param name="name">the name of the item</param>
    /// <param name="ignoreCase">if case should be ignored</param>
    /// <returns>the item</returns>
    public T? GetByName(string name, bool ignoreCase = true)
    {
        if (ignoreCase)
        {
            name = name.ToLowerInvariant();
            return Data.FirstOrDefault(x => x.Name.ToLowerInvariant() == name);
        }
        return Data.FirstOrDefault(x => x.Name == name);
    }

    /// <summary>
    /// Updates an item
    /// </summary>
    /// <param name="item">the item being updated</param>
    /// <param name="dontIncrementConfigRevision">if this is a revision object, if the revision should be updated</param>
    public void Update(T item, bool dontIncrementConfigRevision = false)
    {
        if (item == null)
            throw new Exception("No model");
        
        if (string.IsNullOrWhiteSpace(item.Name))
            throw new Exception("ErrorMessages.NameRequired");
        
        var existingName = GetByName(item.Name);
        if (existingName != null && existingName.Uid != item.Uid)
            throw new Exception("ErrorMessages.NameInUse");
        
        UpdateActual(item, dontIncrementConfigRevision);
        if (dontIncrementConfigRevision == false)
            IncrementConfigurationRevision();
        Refresh();
    }

    /// <summary>
    /// Actual update method
    /// </summary>
    /// <param name="item">the item being updated</param>
    /// <param name="dontIncrementConfigRevision">if this is a revision object, if the revision should be updated</param>
    protected virtual void UpdateActual(T item, bool dontIncrementConfigRevision = false)
        => DbHelper.Update(item);
        

    /// <summary>
    /// Refreshes the data
    /// </summary>
    public void Refresh()
        => Data = DbHelper.Select<T>().Result.ToList();
    
    /// <summary>
    /// Deletes items matching the UIDs
    /// </summary>
    /// <param name="uids">the UIDs of the items to delete</param>
    public virtual async Task Delete(params Guid[] uids)
    {
        if (uids?.Any() != true)
            return;
        
        await DbHelper.Delete(uids);
        IncrementConfigurationRevision();
        
        Refresh();
    }

    
    /// <summary>
    /// Increments the revision of the configuration
    /// </summary>
    protected void IncrementConfigurationRevision()
    {
        if (IncrementsConfiguration == false)
            return;
        var service = new SettingsService();
        _ = service.RevisionIncrement();
    }
    
    
    /// <summary>
    /// Gets a unique name
    /// </summary>
    /// <param name="name">the name to make unique</param>
    /// <returns>the unique name</returns>
    public string GetNewUniqueName(string name)
    {
        List<string> names = Data.Select(x => x.Name.ToLowerInvariant()).ToList();
        return UniqueNameHelper.GetUnique(name, names);
    }

    /// <summary>
    /// Checks to see if a name is in use
    /// </summary>
    /// <param name="uid">the Uid of the item</param>
    /// <param name="name">the name of the item</param>
    /// <returns>true if name is in use</returns>
    public bool NameInUse(Guid uid, string name)
    {
        name = name.ToLowerInvariant().Trim();
        return Data.Any(x => uid != x.Uid && x.Name.ToLowerInvariant() == name);
    }
}