namespace FileFlows.Shared.Models;

/// <summary>
/// Information about a plugin package
/// </summary>
public class PluginPackageInfo
{
    /// <summary>
    /// Gets or sets the name of the plugin
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Gets or sets the version of the plugin
    /// </summary>
    public string Version { get; set; }
    
    /// <summary>
    /// Gets or sets the authors of the plugin
    /// </summary>
    public string Authors { get; set; }
    
    /// <summary>
    /// Gets or sets the URL for the plugin
    /// </summary>
    public string Url { get; set; }
    
    /// <summary>
    /// Gets or sets the description of the plugin
    /// </summary>
    public string Description { get; set; }
    
    /// <summary>
    /// Gets or sets the package name of the plugin
    /// The .ffplugin file
    /// </summary>
    public string Package { get; set; }
    
    /// <summary>
    /// Gets or sets the minimum FileFlows version this plugin needs
    /// </summary>
    public string MinimumVersion { get; set; }
    
    /// <summary>
    /// Gets or sets the available flow elements/node in this plugin
    /// </summary>
    public string[] Elements { get; set; }
    
    /// <summary>
    /// Gets or sets the icon
    /// </summary>
    public string Icon { get; set; }
    
    /// <summary>
    /// Gets or sets if the plugin is installed
    /// </summary>
    public bool Installed { get; set; }
}
