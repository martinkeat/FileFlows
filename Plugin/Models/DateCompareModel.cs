namespace FileFlows.Plugin.Models;

/// <summary>
/// Model used for date compares
/// </summary>
public class DateCompareModel
{
    /// <summary>
    /// Gets or sets the comparison mode
    /// </summary>
    public DateCompareMode Comparison { get; set; }
    /// <summary>
    /// Gets or sets the first minute value
    /// </summary>
    public int Value1 { get; set; }
    /// <summary>
    /// Gets or sets the second minute value
    /// </summary>
    public int Value2 { get; set; }
    /// <summary>
    /// Gets or sets the date value for comparisons
    /// </summary>
    public DateTime DateValue { get; set; }
}

[Flags]
public enum DateCompareMode
{
    /// <summary>
    /// Any date
    /// </summary>
    Any = 1,
    /// <summary>
    /// Greater than the specified number of minutes
    /// </summary>
    GreaterThan = 2,
    /// <summary>
    /// Less than the specified number of minutes
    /// </summary>
    LessThan = 4,
    /// <summary>
    /// Between than the specified number of minutes
    /// </summary>
    Between = 8,
    /// <summary>
    /// Not between than the specified number of minutes
    /// </summary>
    NotBetween = 16,
    /// <summary>
    /// After the specified date
    /// </summary>
    After = 32,
    /// <summary>
    /// Before the specified date
    /// </summary>
    Before = 64
}