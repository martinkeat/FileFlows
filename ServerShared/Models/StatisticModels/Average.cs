namespace FileFlows.ServerShared.Models.StatisticModels;

/// <summary>
/// Average data
/// </summary>
public class Average
{
    /// <summary>
    /// Running totals for the statistic
    /// </summary>
    public Dictionary<int, int> Data { get; set; } = new();
}