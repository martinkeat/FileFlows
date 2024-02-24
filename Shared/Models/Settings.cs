namespace FileFlows.Shared.Models;

/// <summary>
/// Settings for FileFlows
/// </summary>
public class Settings : FileFlowObject
{
    /// <summary>
    /// Gets or sets if plugins should automatically be updated when new version are available online
    /// </summary>
    public bool AutoUpdatePlugins { get; set; }

    /// <summary>
    /// Gets or sets if the server should automatically update when a new version is available online
    /// </summary>
    public bool AutoUpdate { get; set; }

    /// <summary>
    /// Gets or sets if nodes should be automatically updated when the server is updated
    /// </summary>
    public bool AutoUpdateNodes { get; set; }
    
    /// <summary>
    /// Gets or sets the language code for the system
    /// </summary>
    public string Language { get; set; }

    /// <summary>
    /// Gets or sets if telemetry should be disabled
    /// </summary>
    public bool DisableTelemetry { get; set; }
    
    /// <summary>
    /// Gets or sets the number of seconds to check for a new file to process
    /// </summary>
    public int ProcessFileCheckInterval { get; set; }

    /// <summary>
    /// Gets or sets if temporary files from a failed flow should be kept
    /// </summary>
    public bool KeepFailedFlowTempFiles { get; set; }

    /// <summary>
    /// Gets or sets if the Queue messages should be logged
    /// </summary>
    public bool LogQueueMessages { get; set; }
    
    /// <summary>
    /// Gets or sets if the notifications for file added should be shown
    /// </summary>
    public bool ShowFileAddedNotifications { get; set; }
    
    /// <summary>
    /// Gets or sets if the notifications for processing started added should not be shown
    /// </summary>
    public bool HideProcessingStartedNotifications { get; set; }
    
    /// <summary>
    /// Gets or sets if the notifications for processing finished added should not be shown
    /// </summary>
    public bool HideProcessingFinishedNotifications { get; set; }

    /// <summary>
    /// Gets or sets the revision of the configuration
    /// </summary>
    public int Revision { get; set; }

    /// <summary>
    /// Gets or sets if this is running on Windows
    /// </summary>
    public bool IsWindows { get; set; }
    
    /// <summary>
    /// Gets or sets if this is running inside Docker
    /// </summary>
    public bool IsDocker { get; set; }
    
    /// <summary>
    /// Gets or sets the FileFlows version number
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    /// Gets or sets if when the system is paused until
    /// </summary>
    public DateTime PausedUntil { get; set; } = DateTime.MinValue;

    /// <summary>
    /// Gets if the system is paused
    /// </summary>
    public bool IsPaused => DateTime.Now < PausedUntil;

    /// <summary>
    /// Gets or sets the number of log files to keep
    /// </summary>
    public int LogFileRetention { get; set; }
    
    /// <summary>
    /// Gets or sets the number of log entries to keep
    /// </summary>
    public int LogDatabaseRetention { get; set; }
    
    /// <summary>
    /// Gets or sets if every request to the server should be logged
    /// </summary>
    public bool LogEveryRequest { get; set; }
    
    /// <summary>
    /// Gets or sets if the file server is disabled
    /// </summary>
    public bool FileServerDisabled { get; set; }
    
    /// <summary>
    /// Gets or sets the file permissions to set on the file/folders
    /// Only used on Unix based systems
    /// </summary>
    public int FileServerFilePermissions { get; set; }
    
    /// <summary>
    /// Gets or sets the owner group to use
    /// </summary>
    public string FileServerOwnerGroup { get; set; }
    
    /// <summary>
    /// Gets or sets the allowed paths for the file server
    /// </summary>
    public string[] FileServerAllowedPaths { get; set; }
}

/// <summary>
/// The types of Databases supported
/// </summary>
public enum DatabaseType
{
    /// <summary>
    /// SQLite Database
    /// </summary>
    Sqlite = 0,
    /// <summary>
    /// Microsoft SQL Server
    /// </summary>
    SqlServer = 1,
    /// <summary>
    /// MySql / MariaDB
    /// </summary>
    MySql = 2
}