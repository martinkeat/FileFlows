namespace FileFlows.ServerShared.Models.StatisticModels;

/// <summary>
/// Running totals statistics
/// This could be used to record the codecs processed through the system, where the Total dictionary would be the
/// codec name and the times it has appeared
/// </summary>
public class RunningTotals
{
    /// <summary>
    /// Running totals for the statistic
    /// </summary>
    public Dictionary<string, long> Data { get; set; } = new();
}