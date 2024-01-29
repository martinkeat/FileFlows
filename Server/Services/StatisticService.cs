using FileFlows.Server.Controllers;
using FileFlows.Server.Helpers;
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
        await DbHelper.RecordStatistic(statistic);
    }

    /// <summary>
    /// Gets statistics by name
    /// </summary>
    /// <returns>the matching statistics</returns>
    public Task<IEnumerable<Statistic>> GetStatisticsByName(string name)
        => DbHelper.GetStatisticsByName(name);

    /// <summary>
    /// Gets statistics totaled by their name
    /// </summary>
    /// <returns>the matching statistics</returns>
    public async Task<Dictionary<string, int>> GetTotalsByName( string name)
    {
        var stats = await DbHelper.GetStatisticsByName(name);
        var groupedStats = stats.GroupBy(stat => stat.Value.ToString());

        // Create a dictionary to store the counts
        var resultDictionary = new Dictionary<string, int>();

        // Iterate through the grouped stats and count the occurrences
        foreach (var group in groupedStats)
        {
            // group.Key is the unique value, group.Count() is the count
            resultDictionary.Add(group.Key, group.Count());
        }

        return resultDictionary;
    }

    /// <summary>
    /// Clears statistics
    /// </summary>
    /// <param name="name">[Optional] the name of the statistic to clear</param>
    /// <param name="before">[Optional] the date of the statistic to clear</param>
    /// <returns>the response</returns>
    public void Clear(string? name = null, DateTime? before = null)
    {
        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(name))
        {
            Logger.Instance.ILog($"Deleting ALL DbStatistics");
            DbHelper.Execute("delete from DbStatistic");
            return;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            Logger.Instance.ILog($"Deleting DbStatistics before '{before.Value}'");
            DbHelper.Execute("delete from DbStatistic where LogDate < @0", before.Value);
            return;
        }

        Logger.Instance.ILog($"Deleting DbStatistics for '{name}' before '{before.Value}'");
        DbHelper.Execute("delete from DbStatistic where LogDate < @0 and Name = @1 ", before.Value, name);
    }
}