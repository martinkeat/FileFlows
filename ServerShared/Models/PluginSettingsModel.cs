using FileFlows.Shared.Models;

namespace FileFlows.ServerShared.Models;

/// <summary>
/// Plugin Settings Model
/// </summary>
public class PluginSettingsModel : FileFlowObject
{
    /// <summary>
    /// Gets or sets the JSON for hte plugin settings
    /// </summary>
    public string Json { get; set; } = null!;
}