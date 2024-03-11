using System.Text.Json;
using FileFlows.ServerShared;
using FileFlows.ServerShared.Helpers;
using FileFlows.ServerShared.Models;
using FileFlows.ServerShared.Models.StatisticModels;

namespace FileFlows.Managers;

/// <summary>
/// Manager for the libraries
/// </summary>
public class StatisticManager
{
    private FairSemaphore _semaphore = new(1);
    
    /// <summary>
    /// Gets statistic by name
    /// </summary>
    /// <returns>the matching statistic</returns>
    public async Task<T?> GetByName<T>(string name)
    {
        var stat = await DatabaseAccessManager.Instance.StatisticManager.GetStatisticByName(name);
        if (stat == null)
            return default;
        try
        {
            var data = JsonSerializer.Deserialize<T>(stat.Data);
            return data;
        }
        catch (Exception)
        {
            return default;
        }

    }
    
    /// <summary>
    /// Records a statistic
    /// </summary>
    /// <param name="statistic">the statistic to record</param>
    public async Task Update(Statistic statistic)
    {
        if (statistic.Value is string strValue)
            await RecordRunningTotal(statistic.Name, strValue);
        else
            throw new Exception("Unknown statistic: " + statistic.Name);
    }

    /// <summary>
    /// Clears DbStatistics based on specified conditions.
    /// </summary>
    /// <param name="name">Optional. The name for which DbStatistics should be cleared.</param>
    public Task Clear(string? name)
        => DatabaseAccessManager.Instance.StatisticManager.Clear(name);

    /// <summary>
    /// Records storage saved statistic
    /// </summary>
    /// <param name="library">the name of the library</param>
    /// <param name="originalSize">the original size</param>
    /// <param name="finalSize">the final size</param>
    /// <returns>an awaited task</returns>
    public async Task RecordStorageSaved(string library, long originalSize, long finalSize)
    {
        await _semaphore.WaitAsync();
        try
        {
            var saved = await GetByName<StorageSaved>(Globals.STAT_STORAGE_SAVED);
            bool isNew = saved == null;
            if (saved == null)
            {
                saved = new();
            }

            saved.Data ??= new();
            var lib = saved.Data.FirstOrDefault(x => x.Library == library);
            if (lib == null)
            {
                lib = new();
                lib.Library = library;
                saved.Data.Add(lib);
            }

            lib.OriginalSize += originalSize;
            lib.FinalSize += finalSize;
            
            string data = JsonSerializer.Serialize(saved);
            var manager = DatabaseAccessManager.Instance.StatisticManager;
            if (isNew)
                await manager.Insert(new () { Name = Globals.STAT_STORAGE_SAVED, Data = data });
            else
                await manager.Update(Globals.STAT_STORAGE_SAVED, data);
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    /// <summary>
    /// Records a running total 
    /// </summary>
    /// <param name="name">the name of statistic to record</param>
    /// <param name="value">the value of statistic to record</param>
    public async Task RecordRunningTotal(string name, string value)
    {
        await _semaphore.WaitAsync();
        try
        {
            bool isNew = false;
            var existing = await GetByName<RunningTotals>(name);
            if (existing == null)
            {
                isNew = true;
                existing = new RunningTotals();
                existing.Totals = new();
            }

            if (existing.Totals.TryAdd(value, 1) == false)
                existing.Totals[value] += 1;

            string data = JsonSerializer.Serialize(existing);
            var manager = DatabaseAccessManager.Instance.StatisticManager;
            if (isNew)
                await manager.Insert(new () { Name = name, Data = data });
            else
                await manager.Update(name, data);
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    /// <summary>
    /// Records a file has started processing 
    /// </summary>
    public async Task RecordFileStarted()
    {
        await _semaphore.WaitAsync();
        try
        {
            bool isNew = false;
            var existing = await GetByName<Heatmap>(Globals.STAT_PROCESSING_TIMES_HEATMAP);
            if (existing == null)
            {
                isNew = true;
                existing = new Heatmap();
                existing.Data = new();
            }

            int quarter = TimeHelper.GetCurrentQuarter();

            if (existing.Data.TryAdd(quarter, 1) == false)
                existing.Data[quarter] += 1;

            string data = JsonSerializer.Serialize(existing);
            var manager = DatabaseAccessManager.Instance.StatisticManager;
            if (isNew)
                await manager.Insert(new () { Name = Globals.STAT_PROCESSING_TIMES_HEATMAP, Data = data });
            else
                await manager.Update(Globals.STAT_PROCESSING_TIMES_HEATMAP, data);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}