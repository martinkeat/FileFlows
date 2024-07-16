using FileFlows.Plugin;

namespace FileFlows.Shared.Models;


/// <summary>
/// A model used to search the log 
/// </summary>
public class LogSearchModel
{
    /// <summary>
    /// Gets or sets what to search for in the message
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Gets or sets what log type to search for
    /// </summary>
    public LogType? Type { get; set; }
    
    /// <summary>
    /// Gets or sets if the search results should include log messages greater than the specified type
    /// </summary>
    public bool TypeIncludeHigherSeverity { get; set; }

    /// <summary>
    /// Gets or sets the from date to search
    /// </summary>
    public DateTime FromDate { get; set; }

    /// <summary>
    /// Gets or sets the to date to search
    /// </summary>
    public DateTime ToDate { get; set; }
}