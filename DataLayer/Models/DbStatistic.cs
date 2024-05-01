using FileFlows.ServerShared;

namespace FileFlows.DataLayer.Models;

/// <summary>
/// Statistic saved in the database
/// </summary>
public class DbStatistic
{
    /// <summary>
    /// Gets or sets the name of the statistic
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the type of statistic
    /// </summary>
    public StatisticType Type { get; set; }
    
    /// <summary>
    /// Gets or sets the number value
    /// </summary>
    public string Data { get; set; } = string.Empty;
}