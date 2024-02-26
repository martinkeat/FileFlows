namespace FileFlows.Plugin.Types;

/// <summary>
/// A object that can contain either a number or a percent value
/// </summary>
public class NumberPercent
{
    /// <summary>
    /// Gets or sets its value
    /// </summary>
    public int Value { get; set; }
    
    /// <summary>
    /// Gets or sets if this value is a percentage or a number
    /// </summary>
    public bool Percentage { get; set; }
}