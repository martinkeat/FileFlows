using System.Dynamic;


namespace FileFlows.Managers;

/// <summary>
/// Plugin service
/// </summary>
public class PluginService : CachedManager<PluginInfo>
{
    /// <summary>
    /// Get the plugin info for a specific plugin by package name
    /// </summary>
    /// <param name="name">The package name of the plugin</param>
    /// <returns>The plugin info for the plugin</returns>
    public PluginInfo GetByPackageName(string name)
    {
        var pi = Data.FirstOrDefault(x => x.PackageName == name);  
        return pi ?? new();
    }
}