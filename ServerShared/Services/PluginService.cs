using FileFlows.Shared.Models;

namespace FileFlows.ServerShared.Services;

/// <summary>
/// Plugin Service interface
/// </summary>
public interface IPluginService
{
    /// <summary>
    /// Get all plugin infos
    /// </summary>
    /// <returns>all plugin infos</returns>
    Task<List<PluginInfo>> GetAllAsync();

    /// <summary>
    /// Updates plugin info
    /// </summary>
    /// <param name="pluginInfo">the plugin info</param>
    /// <returns>the updated plugininfo</returns>
    Task<PluginInfo> Update(PluginInfo pluginInfo);

    /// <summary>
    /// Download a plugin
    /// </summary>
    /// <param name="plugin">the plugin to download</param>
    /// <returns>the byte data of the plugin</returns>
    Task<byte[]> Download(PluginInfo plugin);
    /// <summary>
    /// Gets the settings json for a plugin
    /// </summary>
    /// <param name="pluginPackageName">the name of the plugin package</param>
    /// <returns>the settings json</returns>
    Task<string> GetSettingsJson(string pluginPackageName);
}
