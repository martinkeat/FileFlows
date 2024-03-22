using System.Text.Json;
using FileFlows.ServerShared;
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
        string json = JsonSerializer.Serialize(statistic.Value);
        await _semaphore.WaitAsync();
        try
        {
            if (await DatabaseAccessManager.Instance.StatisticManager.Update(statistic.Name, statistic.Type, json))
                return;
            await DatabaseAccessManager.Instance.StatisticManager.Insert(new()
            {
                Name = statistic.Name,
                Data = json,
                Type = statistic.Type
            });
        }
        catch (Exception ex)
        {
            Logger.Instance.WLog($"Failed to update statistic '{statistic.Name}': " + ex.Message);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Clears DbStatistics based on specified conditions.
    /// </summary>
    /// <param name="name">Optional. The name for which DbStatistics should be cleared.</param>
    public Task Clear(string? name)
        => DatabaseAccessManager.Instance.StatisticManager.Clear(name);

    

    /// <summary>
    /// Gets all statistics in the system
    /// </summary>
    /// <returns>all the statistics</returns>
    public async Task<Dictionary<string, object>> GetAll()
    {
        var manager = DatabaseAccessManager.Instance.StatisticManager;
        var items = await manager.GetAll();
        Dictionary<string, object> results = new();
        foreach (var item in items)
        {
            switch (item.Type)
            {
                case StatisticType.Heatmap:
                    results[item.Name] = JsonSerializer.Deserialize<Heatmap>(item.Data)!;
                    break;
                case StatisticType.RunningTotals:
                    results[item.Name] = JsonSerializer.Deserialize<RunningTotals>(item.Data)!;
                    break;
                case StatisticType.StorageSaved:
                    results[item.Name] = JsonSerializer.Deserialize<StorageSaved>(item.Data)!;
                    break;
                case StatisticType.Average:
                    results[item.Name] = JsonSerializer.Deserialize<Average>(item.Data)!;
                    break;
            }
        }
        return results;
    }
}