using System.Text.Json.Serialization;
using FileFlows.Shared.Models;

namespace FileFlows.Server;

/// <summary>
/// Application settings that are saved to the appsettings.json file 
/// </summary>
internal class AppSettings
{
    /// <summary>
    /// Gets or sets the database connection to use
    /// </summary>
    public string DatabaseConnection { get; set; }
    
    /// <summary>
    /// Gets or sets the database type
    /// </summary>
    public DatabaseType DatabaseType { get; set; }

    /// <summary>
    /// Gets or sets if the database should recreate if it already exists
    /// </summary>
    public bool RecreateDatabase{ get; set; }

    /// <summary>
    /// Gets or sets the encryption key to use
    /// </summary>
    public string EncryptionKey { get; set; }

    /// <summary>
    /// Gets or sets the license email
    /// </summary>
    public string LicenseEmail { get; set; }

    /// <summary>
    /// Gets or sets the licensed key
    /// </summary>
    public string LicenseKey { get; set; }

    /// <summary>
    /// Gets or sets the license code
    /// </summary>
    public string LicenseCode { get; set; }
    
    /// <summary>
    /// Gets or sets the server port to use 
    /// </summary>
    public int? ServerPort { get; set; }

    /// <summary>
    /// Gets or sets if the app should start minimized
    /// </summary>
    public bool StartMinimized { get; set; }

    /// <summary>
    /// Gets or sets an optional FileFlows.com URL
    /// </summary>
    public string FileFlowsDotComUrl { get; set; }

    /// <summary>
    /// Gets or sets the connection string of where to migrate the data to
    /// This will be checked at startup and if found, the data will be migrated then this
    /// setting will be reset
    /// </summary>
    public string? DatabaseMigrateConnection { get; set; }

    /// <summary>
    /// Gets or sets the connection string of where to migrate the data to
    /// This will be checked at startup and if found, the data will be migrated then this
    /// setting will be reset
    /// </summary>
    public DatabaseType? DatabaseMigrateType { get; set; }
    
    /// <summary>
    /// Gets or sets the security mode
    /// </summary>
    public SecurityMode Security { get; set; }
    
    /// <summary>
    /// Gets or sets if DockerMods should run on the server on startup/when enabled
    /// </summary>
    public bool DockerModsOnServer { get; set; }

    /// <summary>
    /// Gets or sets if backups should not be taken on upgrades.
    /// This is only used when using an external database
    /// </summary>
    public bool DontBackupOnUpgrade { get; set; }
}