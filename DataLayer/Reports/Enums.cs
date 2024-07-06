namespace FileFlows.DataLayer.Reports;


/// <summary>
/// Report direction
/// </summary>
public enum ReportDirection
{
    /// <summary>
    /// Input
    /// </summary>
    Input,
    /// <summary>
    /// Output
    /// </summary>
    Output
}


/// <summary>
/// Stream Types
/// </summary>
public enum StreamType
{
    /// <summary>
    /// Any type
    /// </summary>
    Any,
    /// <summary>
    /// Audio
    /// </summary>
    Audio,
    /// <summary>
    /// Video
    /// </summary>
    Video,
    /// <summary>
    /// Subtitle
    /// </summary>
    Subtitle
}

/// <summary>
/// Different processed statistics
/// </summary>
public enum ProcessedStatistic
{
    /// <summary>
    /// Number of files processed
    /// </summary>
    Count,
    /// <summary>
    /// Size of files processed
    /// </summary>
    Size,
    /// <summary>
    /// Processing duration
    /// </summary>
    Duration
}