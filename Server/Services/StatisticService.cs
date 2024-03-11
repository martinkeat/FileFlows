using FileFlows.Managers;
using FileFlows.ServerShared.Models;
using FileFlows.ServerShared.Models.StatisticModels;
using FileFlows.ServerShared.Services;

namespace FileFlows.Server.Services;

/// <summary>
/// Statistic service
/// </summary>
public class StatisticService : IStatisticService
{
    /// <summary>
    /// Records a statistic value
    /// </summary>
    /// <returns>a task to await</returns>
    public Task Record(string name, object value) =>
        Record(new Statistic { Name = name, Value = value });

    /// <summary>
    /// Records a statistic
    /// </summary>
    /// <param name="statistic">the statistic to record</param>
    public async Task Record(Statistic statistic)
    {
        if (statistic == null)
            return;
        await new StatisticManager().Update(statistic);
    }

    /// <summary>
    /// Clears DbStatistics based on specified conditions.
    /// </summary>
    /// <param name="name">Optional. The name for which DbStatistics should be cleared.</param>
    public Task Clear(string? name = null)
        => new StatisticManager().Clear(name);

    /// <summary>
    /// Gets statistics by name
    /// </summary>
    /// <returns>the matching statistics</returns>
    public async Task<IEnumerable<Statistic>> GetRunningTotals(string name)
    {
        var stat = await new StatisticManager().GetByName<RunningTotals>(name);
        if (stat == null)
            return new List<Statistic>();
        return stat.Totals.Select(x => new Statistic()
        {
            Name = x.Key,
            Value = x.Value
        });
    }

    /// <summary>
    /// Gets heatmap by name
    /// </summary>
    /// <returns>the heatmap</returns>
    public async Task<List<HeatmapData>> GetHeatMap(string name)
    {
        var data = await new StatisticManager().GetByName<Heatmap>(name);
        return (data ?? new()).ConvertData();
    }


    /// <summary>
    /// Gets storage saved
    /// </summary>
    /// <returns>the storage saved</returns>
    public async Task<List<StorageSavedData>> GetStorageSaved()
        => (await new StatisticManager().GetByName<StorageSaved>(Globals.STAT_STORAGE_SAVED))?.Data ?? new();
}