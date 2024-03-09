namespace FileFlows.Shared.Models;

/// <summary>
/// Gets the system information about FileFlows
/// </summary>
public class SystemInfo
{
    /// <summary>
    /// Gets the amount of memory used by FileFlows
    /// </summary>
    public long MemoryUsage { get; set; }
    
    /// <summary>
    /// Gets the how much CPU is used by FileFlows
    /// </summary>
    public float CpuUsage { get; set; }

    /// <summary>
    /// Gets or sets if the system is paused
    /// </summary>
    public bool IsPaused { get; set; }

    /// <summary>
    /// Gets or sets when the system is paused until
    /// </summary>
    public DateTime PausedUntil { get; set; }

    /// <summary>
    /// Gets the current time on the server
    /// </summary>
    public DateTime CurrentTime => DateTime.UtcNow;
}