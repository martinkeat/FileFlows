using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// Services that caches its objects
/// </summary>
public abstract class CachedService<T> where T : FileFlowObject, new()
{
    /// <summary>
    /// Gets or sets the data
    /// </summary>
    protected List<T> Data { get; set; }

    /// <summary>
    /// Gets the data
    /// </summary>
    /// <returns>the data</returns>
    public List<T> GetAll() => Data;

    /// <summary>
    /// Gets an item by its UID
    /// </summary>
    /// <param name="uid">the UID of the item</param>
    /// <returns>the item</returns>
    public T? GetByUid(Guid uid)
        => Data.FirstOrDefault(x => x.Uid == uid);

    /// <summary>
    /// Updates an item
    /// </summary>
    /// <param name="item">the item being updated</param>
    public void Update(T item)
    {
        UpdateActual(item);
        Refresh();
    }

    /// <summary>
    /// Actual update method
    /// </summary>
    /// <param name="item">the item being updated</param>
    protected virtual void UpdateActual(T item)
        => DbHelper.Update(item);

    /// <summary>
    /// Refreshes the data
    /// </summary>
    protected void Refresh()
        => this.Data = DbHelper.Select<T>().Result.ToList();
    
    
    /// <summary>
    /// Deletes all items matching the UIDs
    /// </summary>
    /// <param name="uids">the UIDs of the items to delete</param>
    public async Task DeleteAll(params Guid[] uids)
    {
        await DbHelper.Delete(uids);
        Refresh();
    }

}