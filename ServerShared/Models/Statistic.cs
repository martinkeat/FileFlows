namespace FileFlows.ServerShared.Models;

/// <summary>
/// A statistic
/// </summary>
public class Statistic
{
    /// <summary>
    /// Gets or sets the name of the statistic
    /// </summary>
    public string Name { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the type of statistic
    /// </summary>
    public StatisticType Type { get; set; }

    /// <summary>
    /// Gets or sets the value
    /// </summary>
    public object? Value { get; set; }
}