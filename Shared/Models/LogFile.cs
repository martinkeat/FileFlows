namespace FileFlows.Shared.Models;
/// <summary>
/// Represents a log file entry.
/// </summary>
public class LogFile
{
    /// <summary>
    /// Gets or sets the source of the log file.
    /// </summary>
    public string Source { get; set; }

    /// <summary>
    /// Gets or sets the date associated with the log file.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Gets or sets the revision number of the log file.
    /// </summary>
    public int Revision { get; set; }

    /// <summary>
    /// Gets or sets the short file name of the log file.
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// Gets or sets the short name or identifier of the log file.
    /// </summary>
    public string ShortName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the log file is active.
    /// </summary>
    public bool Active { get; set; }
}
