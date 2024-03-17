namespace FileFlows.Plugin;

/// <summary>
/// Interface used by plugins to FileFlows
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// Gets the UID of the plugin
    /// </summary>
    Guid Uid { get; }
    
    /// <summary>
    /// Gets the name of the plugin
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the icon of the plugin
    /// </summary>
    virtual string? Icon => null;
    
    /// <summary>
    /// Gets the minimum support FileFlows version of this plugin
    /// </summary>
    string MinimumVersion { get; }

    /// <summary>
    /// Initializes the plugin
    /// </summary>
    void Init();
}

/// <summary>
/// Interface used to specify that a plugin has settings associated with it
/// </summary>
public interface IPluginSettings
{

}
