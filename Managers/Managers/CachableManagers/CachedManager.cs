using FileFlows.Plugin;
using FileFlows.Shared.Helpers;

namespace FileFlows.Managers;

/// <summary>
/// Services that caches its objects
/// </summary>
public abstract class CachedManager<T> where T : FileFlowObject, new()
{
    /// <summary>
    /// Gets if this service increments the system configuration revision number when changes to the data happens
    /// </summary>
    public virtual bool IncrementsConfiguration => true;

    /// <summary>
    /// Gets if the cache should be used
    /// </summary>
    protected bool UseCache => SettingsManager.UseCache;

    private FairSemaphore GetDataSemaphore = new(1);
    
    protected static List<T> _Data;
    /// <summary>
    /// Gets or sets the data
    /// </summary>
    protected List<T> Data
    {
        get
        {
            GetDataSemaphore.WaitAsync().Wait();
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
    public virtual async Task<List<T>> GetAll()
    {
        if(UseCache)
            return Data;
        return await LoadDataFromDatabase();
    }

    /// <summary>
    /// Gets an item by its UID
    /// </summary>
    /// <param name="uid">the UID of the item</param>
    /// <returns>the item</returns>
    public virtual async Task<T?> GetByUid(Guid uid)
    {
        if(SettingsManager.UseCache)
            return Data.FirstOrDefault(x => x.Uid == uid);
        return await DatabaseAccessManager.Instance.FileFlowsObjectManager.Single<T>(uid);
    }

    /// <summary>
    /// Gets a item by it's name
    /// </summary>
    /// <param name="name">the name of the item</param>
    /// <param name="ignoreCase">if case should be ignored</param>
    /// <returns>the item</returns>
    public virtual async Task<T?> GetByName(string name, bool ignoreCase = true)
    {
        if (UseCache)
        {
            if (ignoreCase)
            {
                name = name.ToLowerInvariant();
                return Data.FirstOrDefault(x => x.Name.ToLowerInvariant() == name);
            }

            return Data.FirstOrDefault(x => x.Name == name);
        }

        return await DatabaseAccessManager.Instance.FileFlowsObjectManager.GetByName<T>(name, ignoreCase);
    }

    /// <summary>
    /// Updates an item
    /// </summary>
    /// <param name="item">the item being updated</param>
    /// <param name="dontIncrementConfigRevision">if this is a revision object, if the revision should be updated</param>
    public async virtual Task<Result<T>> Update(T item, bool dontIncrementConfigRevision = false)
    {
        if (item == null)
            return Result<T>.Fail("No model");

        if (string.IsNullOrWhiteSpace(item.Name))
            return Result<T>.Fail("ErrorMessages.NameRequired");

        var existingName = await GetByName(item.Name);
        if (existingName != null && existingName.Uid != item.Uid)
            return Result<T>.Fail("ErrorMessages.NameInUse");
        
        Logger.Instance.ILog($"Updating {item.GetType().Name}: '{item.Name}'");
        await UpdateActual(item, dontIncrementConfigRevision);
        
        if (dontIncrementConfigRevision == false)
            IncrementConfigurationRevision();
        
        if(UseCache)
            Refresh();

        return (await GetByUid(item.Uid))!;
    }

    /// <summary>
    /// Actual update method
    /// </summary>
    /// <param name="item">the item being updated</param>
    /// <param name="dontIncrementConfigRevision">if this is a revision object, if the revision should be updated</param>
    protected virtual Task UpdateActual(T item, bool dontIncrementConfigRevision = false)
        => DatabaseAccessManager.Instance.FileFlowsObjectManager.Update(item);


    /// <summary>
    /// Refreshes the data
    /// </summary>
    public virtual void Refresh()
    {
        if (UseCache == false)
            return;
        
        Logger.Instance.ILog($"Refreshing Data for '{typeof(T).Name}'");
        var newData = LoadDataFromDatabase().Result;
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
    /// Loads the data from the database
    /// </summary>
    /// <returns>the data</returns>
    protected async Task<List<T>> LoadDataFromDatabase()
     => (await DatabaseAccessManager.Instance.FileFlowsObjectManager.Select<T>()).ToList();

    /// <summary>
    /// Deletes items matching the UIDs
    /// </summary>
    /// <param name="uids">the UIDs of the items to delete</param>
    public virtual async Task Delete(params Guid[] uids)
    {
        if (uids?.Any() != true)
            return;
        
        await DatabaseAccessManager.Instance.FileFlowsObjectManager.Delete(uids);
        IncrementConfigurationRevision();
        
        if(UseCache)
            Refresh();
    }

    
    /// <summary>
    /// Increments the revision of the configuration
    /// </summary>
    protected void IncrementConfigurationRevision()
    {
        if (IncrementsConfiguration == false)
            return;
        var service = new SettingsManager();
        _ = service.RevisionIncrement();
    }
    
    
    /// <summary>
    /// Gets a unique name
    /// </summary>
    /// <param name="name">the name to make unique</param>
    /// <returns>the unique name</returns>
    public virtual async Task<string> GetNewUniqueName(string name)
    {
        List<string> names;
        if (UseCache)
        {
             names = Data.Select(x => x.Name.ToLowerInvariant()).ToList();
        }
        else
        {
            names = (await DatabaseAccessManager.Instance.FileFlowsObjectManager.GetNames<T>())
                .Select(x => x.ToLowerInvariant()).ToList();
        }

        return UniqueNameHelper.GetUnique(name, names);
    }

    /// <summary>
    /// Checks to see if a name is in use
    /// </summary>
    /// <param name="uid">the Uid of the item</param>
    /// <param name="name">the name of the item</param>
    /// <returns>true if name is in use</returns>
    public virtual async Task<bool> NameInUse(Guid uid, string name)
    {
        if (UseCache)
        {
            name = name.ToLowerInvariant().Trim();
            return Data.Any(x => uid != x.Uid && x.Name.ToLowerInvariant() == name);
        }
        else
        {
            var existing =
                await DatabaseAccessManager.Instance.FileFlowsObjectManager.GetByName<T>(name, ignoreCase: true);
            return existing.IsFailed == false && existing.ValueOrDefault != null;
        }
    }
}