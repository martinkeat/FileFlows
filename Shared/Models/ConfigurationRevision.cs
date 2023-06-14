namespace FileFlows.Shared.Models;

/// <summary>
/// The FileFlows configuration as a specific revision
/// </summary>
public class ConfigurationRevision
{
    /// <summary>
    /// Gets or sets the Revision
    /// </summary>
    public int Revision { get; set; }

    /// <summary>
    /// Gets or sets the maximum nodes that can be executed
    /// </summary>
    public int MaxNodes { get; set; }
    
    /// <summary>
    /// Gets or sets if temporary files should be kept if the flow fails
    /// </summary>
    public bool KeepFailedFlowTempFiles { get; set; }

    /// <summary>
    /// Gets or sets the system variables
    /// </summary>
    public Dictionary<string, string> Variables { get; set; }

    /// <summary>
    /// Gets or sets the shared Scripts
    /// </summary>
    public List<Script> SharedScripts { get; set; }
    
    /// <summary>
    /// Gets or sets the flow Scripts
    /// </summary>
    public List<Script> FlowScripts { get; set; }
    
    /// <summary>
    /// Gets or sets the system Scripts
    /// </summary>
    public List<Script> SystemScripts { get; set; }

    /// <summary>
    /// Gets or sets all the flows in the system
    /// </summary>
    public List<Flow> Flows { get; set; }

    /// <summary>
    /// Gets or sets all the libraries in the system
    /// </summary>
    public List<Library> Libraries { get; set; }

    /// <summary>
    /// Gets or sets the plugin settings which is dictionary of the Plugin name and the settings JSON
    /// </summary>
    public Dictionary<string, string> PluginSettings { get; set; }

    /// <summary>
    /// Gets or sets a dictionary of plugin names and their binary data in the system
    /// </summary>
    public Dictionary<string, byte[]> Plugins { get; set; }
}