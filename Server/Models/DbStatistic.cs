using FileFlows.Plugin;

namespace FileFlows.Server.Models;

/// <summary>
/// Statistic saved in the database
/// </summary>
public class DbStatistic
{
    /// <summary>
    /// Gets or sets when the statistic was recorded
    /// </summary>
    public DateTime LogDate { get; set; }

    /// <summary>
    /// Gets or sets the name of the statistic
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the type
    /// </summary>
    public StatisticType Type { get; set; }
    
    /// <summary>
    /// Gets or sets the string value
    /// </summary>
    public string StringValue { get; set; }
    
    /// <summary>
    /// Gets or sets the number value
    /// </summary>
    public double NumberValue { get; set; }
}