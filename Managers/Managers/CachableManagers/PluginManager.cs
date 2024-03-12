using System.Dynamic;
using System.Text.Json;
using FileFlows.DataLayer.Helpers;
using FileFlows.Plugin;
using FileFlows.ServerShared.Models;

namespace FileFlows.Managers;

/// <summary>
/// Plugin manager
/// </summary>
public class PluginManager : CachedManager<PluginInfo>
{
    /// <summary>
    /// Get the plugin info for a specific plugin by package name
    /// </summary>
    /// <param name="name">The package name of the plugin</param>
    /// <returns>The plugin info for the plugin</returns>
    public async Task<PluginInfo?> GetByPackageName(string name)
        => (await GetData()).FirstOrDefault(x => x.PackageName == name);

    /// <summary>
    /// Deletes plugin settings from the package name
    /// </summary>
    /// <param name="name">the package name</param>
    public Task DeletePluginSettings(string name)
        => DatabaseAccessManager.Instance.ObjectManager.DeleteByTypeAndName(typeof(PluginSettingsModel).FullName!, name);

    /// <summary>
    /// Gets all plugin settings with and the plugin settings, does not decrypt the data
    /// </summary>
    /// <returns>all plugin settings with and the plugin settings</returns>
    public Task<List<PluginSettingsModel>> GetAllPluginSettings()
        => DatabaseAccessManager.Instance.FileFlowsObjectManager.Select<PluginSettingsModel>();
    
    /// <summary>
    /// Gets all plugin settings with and the plugin settings, does not decrypt the data
    /// </summary>
    /// <param name="name">the name of the plugin settings</param>
    /// <returns>all plugin settings with and the plugin settings</returns>
    public Task<Result<PluginSettingsModel>> GetPluginSettings(string name)
        => DatabaseAccessManager.Instance.FileFlowsObjectManager.GetByName<PluginSettingsModel>(name);

    /// <summary>
    /// Sets the plugin settings
    /// </summary>
    /// <param name="name">the name of the plugin</param>
    /// <param name="json">the plugin json</param>
    public async Task SetPluginSettings(string name, string json)
    {
        var manager = DatabaseAccessManager.Instance.FileFlowsObjectManager;
        var existing = await GetPluginSettings(name);
        if (existing.IsFailed == false && existing.Value != null)
        {
            existing.Value.Json = json;
            await manager.Update(existing.Value);
            return;
        }
        
        await manager.AddOrUpdateObject(new PluginSettingsModel() {
            Name = name,
            DateCreated = DateTime.UtcNow,
            Json = json
        }, saveRevision: true);
    }
}