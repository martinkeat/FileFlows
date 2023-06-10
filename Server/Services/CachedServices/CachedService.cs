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

    private SemaphoreSlim GetDataSemaphore = new(1);
    protected static List<T> _Data;
    /// <summary>
    /// Gets or sets the data
    /// </summary>
    protected List<T> Data
    {
        get
        {
            GetDataSemaphore.Wait();
            try
            {
                if (_Data == null)
                    Refresh();
                return _Data;
            }
            finally
            {
                GetDataSemaphore.Release();
            }
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
    public virtual List<T> GetAll() => Data;

    /// <summary>
    /// Gets all the data async
    /// </summary>
    /// <returns>the data</returns>
    public virtual Task<List<T>> GetAllAsync() => Task.FromResult(GetAll());

    /// <summary>
    /// Gets an item by its UID
    /// </summary>
    /// <param name="uid">the UID of the item</param>
    /// <returns>the item</returns>
    public virtual T? GetByUid(Guid uid)
        => Data.FirstOrDefault(x => x.Uid == uid);

    /// <summary>
    /// Gets an item by its UID async
    /// </summary>
    /// <param name="uid">the UID of the item</param>
    /// <returns>the item</returns>
    public virtual Task<T> GetByUidAsync(Guid uid) => Task.FromResult(GetByUid(uid)!);

    /// <summary>
    /// Gets a item by it's name
    /// </summary>
    /// <param name="name">the name of the item</param>
    /// <param name="ignoreCase">if case should be ignored</param>
    /// <returns>the item</returns>
    public virtual T? GetByName(string name, bool ignoreCase = true)
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
    public async virtual Task<T> Update(T item, bool dontIncrementConfigRevision = false)
    {
        if (item == null)
            throw new Exception("No model");

        if (string.IsNullOrWhiteSpace(item.Name))
            throw new Exception("ErrorMessages.NameRequired");

        var existingName = GetByName(item.Name);
        if (existingName != null && existingName.Uid != item.Uid)
            throw new Exception("ErrorMessages.NameInUse");
        
        Logger.Instance.ILog($"Updating {item.GetType().Name}: '{item.Name}'");
        await UpdateActual(item, dontIncrementConfigRevision);
        if (dontIncrementConfigRevision == false)
            IncrementConfigurationRevision();
        Refresh();

        return GetByUid(item.Uid);
    }

    /// <summary>
    /// Actual update method
    /// </summary>
    /// <param name="item">the item being updated</param>
    /// <param name="dontIncrementConfigRevision">if this is a revision object, if the revision should be updated</param>
    protected virtual Task UpdateActual(T item, bool dontIncrementConfigRevision = false)
        => DbHelper.Update(item);


    /// <summary>
    /// Refreshes the data
    /// </summary>
    public virtual void Refresh()
    {
        
        Logger.Instance.ILog($"Refreshing Data for '{typeof(T).Name}'");
        var newData = DbHelper.Select<T>().Result.ToList();
        if (_Data?.Any() != true)
        {
            _Data = newData;
            return;
        }
        
        List<T> updatedData = new List<T>();

        foreach (var newItem in newData)
        {
            var existingItem = _Data.FirstOrDefault(item => item.Uid == newItem.Uid);
            if (existingItem != null)
            {
                // Update properties of existing item with values from newItem
                var properties = typeof(T).GetProperties();
                foreach (var property in properties)
                {
                    if (property.CanRead && property.CanWrite)
                    {
                        var value = property.GetValue(newItem);
                        property.SetValue(existingItem, value);
                    }
                }
                updatedData.Add(existingItem);
            }
            else
            {
                updatedData.Add(newItem);
            }
        }

        _Data = updatedData;
    }

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
    public virtual string GetNewUniqueName(string name)
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
    public virtual bool NameInUse(Guid uid, string name)
    {
        name = name.ToLowerInvariant().Trim();
        return Data.Any(x => uid != x.Uid && x.Name.ToLowerInvariant() == name);
    }

    // /// <summary>
    // /// Copies the data from one object into another
    // /// </summary>
    // /// <param name="source">the source object</param>
    // /// <param name="destination">the destination object</param>
    // protected virtual void CopyInto(T source, T destination)
    // {
    //     var properties = typeof(T).GetProperties();
    //     foreach (var property in properties)
    //     {
    //         if (property.CanRead && property.CanWrite)
    //         {
    //             var value = property.GetValue(source);
    //             property.SetValue(destination, value);
    //         }
    //     }
    // }
}