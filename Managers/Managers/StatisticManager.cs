using System.Text.Json;
using FileFlows.DataLayer.Models;
using FileFlows.ServerShared;
using FileFlows.ServerShared.Models;

namespace FileFlows.Managers;

/// <summary>
/// Manager for the libraries
/// </summary>
public class StatisticManager
{
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
    public async Task Insert(Statistic statistic)
    {
        DbStatistic stat;
        stat = new DbStatistic()
        {
            Name = statistic.Name,
            Data = JsonSerializer.Serialize(statistic.Value)
        };
        await DatabaseAccessManager.Instance.StatisticManager.Insert(stat);
    }

    /// <summary>
    /// Clears DbStatistics based on specified conditions.
    /// </summary>
    /// <param name="name">Optional. The name for which DbStatistics should be cleared.</param>
    public Task Clear(string? name)
        => DatabaseAccessManager.Instance.StatisticManager.Clear(name);
}