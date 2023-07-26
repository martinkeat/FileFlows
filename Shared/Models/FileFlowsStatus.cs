namespace FileFlows.Shared.Models;

/// <summary>
/// The system status of FileFlows
/// </summary>
public class FileFlowsStatus
{
    /// <summary>
    /// Gets or sets the configuration status of FileFLows
    /// </summary>
    public ConfigurationStatus ConfigurationStatus { get; set; }

    /// <summary>
    /// Gets or sets if FileFlows is using an external database
    /// </summary>
    public bool ExternalDatabase { get; set; }
    
    /// <summary>
    /// Gets or sets if FileFlows is licensed
    /// </summary>
    public bool Licensed { get; set; }
    
    /// <summary>
    /// Gets or sets if license allowed for custom dashboards
    /// </summary>
    public bool LicenseDashboards { get; set; }
    
    /// <summary>
    /// Gets or sets if license allowed for revisions
    /// </summary>
    public bool LicenseRevisions { get; set; }
    
    /// <summary>
    /// Gets or sets if license allowed for tasks
    /// </summary>
    public bool LicenseTasks { get; set; }
    
    /// <summary>
    /// Gets or sets if license allowed for webhooks
    /// </summary>
    public bool LicenseWebhooks { get; set; }
    
    /// <summary>
    /// Gets or sets if license allowed for auto updates
    /// </summary>
    public bool LicenseAutoUpdates { get; set; }
}

/// <summary>
/// The configuration status of the system
/// </summary>
[Flags]
public enum ConfigurationStatus
{
    /// <summary>
    /// Flows are configured
    /// </summary>
    Flows = 1,
    /// <summary>
    /// Libraries are configured
    /// </summary>
    Libraries = 2
}