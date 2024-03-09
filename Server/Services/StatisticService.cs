using FileFlows.DataLayer;
using FileFlows.Managers;
using FileFlows.Server.Controllers;
using FileFlows.Server.Helpers;
using FileFlows.ServerShared.Models;
using FileFlows.ServerShared.Services;
using DbStatistic = FileFlows.DataLayer.Models.DbStatistic;

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
        await new StatisticManager().Insert(statistic);
    }

    /// <summary>
    /// Gets statistics by name
    /// </summary>
    /// <returns>the matching statistics</returns>
    public Task<IEnumerable<Statistic>> GetStatisticsByName(string name)
        => new StatisticManager().GetStatisticsByName(name);

    /// <summary>
    /// Gets statistics totaled by their name
    /// </summary>
    /// <returns>the matching statistics</returns>
    public async Task<Dictionary<string, int>> GetTotalsByName( string name)
    {
        var stats = await new StatisticManager().GetStatisticsByName(name);
        var groupedStats = stats.GroupBy(stat => stat.Value.ToString());

        // Create a dictionary to store the counts
        var resultDictionary = new Dictionary<string, int>();

        // Iterate through the grouped stats and count the occurrences
        foreach (var group in groupedStats)
        {
            // group.Key is the unique value, group.Count() is the count
            resultDictionary.Add(group.Key, group.Count());
        }

        // Order the dictionary by count in descending order
        resultDictionary = resultDictionary.OrderByDescending(kv => kv.Value)
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        return resultDictionary;
    }

    /// <summary>
    /// Clears DbStatistics based on specified conditions.
    /// </summary>
    /// <param name="name">Optional. The name for which DbStatistics should be cleared.</param>
    /// <param name="before">Optional. The date before which DbStatistics should be cleared.</param>
    /// <param name="after">Optional. The date after which DbStatistics should be cleared.</param>
    public Task Clear(string? name = null, DateTime? before = null, DateTime? after = null)
        => new StatisticManager().Clear(name, before, after);
}