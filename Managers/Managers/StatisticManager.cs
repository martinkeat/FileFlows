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
    /// Gets statistics by name
    /// </summary>
    /// <returns>the matching statistics</returns>
    public async Task<IEnumerable<Statistic>> GetStatisticsByName(string name)
    {
        List<DbStatistic> stats = await DatabaseAccessManager.Instance.StatisticManager.GetStatisticsByName(name);

        var results = new List<Statistic>();
        foreach (var stat in stats)
        {
            if(stat.Type == StatisticType.Number)
                results.Add(new () { Name = stat.Name, Value = stat.NumberValue});
            if(stat.Type == StatisticType.String)
                results.Add(new () { Name = stat.Name, Value = stat.StringValue});
        }

        return results;


    }
    
    /// <summary>
    /// Records a statistic
    /// </summary>
    /// <param name="statistic">the statistic to record</param>
    public async Task Insert(Statistic statistic)
    {
        DbStatistic stat;
        if (double.TryParse(statistic.Value.ToString(), out double number))
        {
            stat = new DbStatistic()
            {
                Type = StatisticType.Number,
                Name = statistic.Name,
                LogDate = DateTime.Now,
                NumberValue = number,
                StringValue = string.Empty
            };
        }
        else
        {
            // treat as string
            stat = new DbStatistic()
            {
                Type = StatisticType.String,
                Name = statistic.Name,
                LogDate = DateTime.Now,
                NumberValue = 0,
                StringValue = statistic.Value.ToString()
            };
        }
        await DatabaseAccessManager.Instance.StatisticManager.Insert(stat);
    }

    /// <summary>
    /// Clears DbStatistics based on specified conditions.
    /// </summary>
    /// <param name="name">Optional. The name for which DbStatistics should be cleared.</param>
    /// <param name="before">Optional. The date before which DbStatistics should be cleared.</param>
    /// <param name="after">Optional. The date after which DbStatistics should be cleared.</param>
    public Task Clear(string? name, DateTime? before, DateTime? after)
        => DatabaseAccessManager.Instance.StatisticManager.Clear(name, before, after);
}