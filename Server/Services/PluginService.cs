using System.Dynamic;
using FileFlows.DataLayer.Helpers;
using FileFlows.Managers;
using FileFlows.Plugin;
using FileFlows.Server.Helpers;
using FileFlows.Server.Controllers;
using FileFlows.ServerShared.Models;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Models;


namespace FileFlows.Server.Services;
/// <summary>
/// Plugin service
/// </summary>
public class PluginService : IPluginService
{
    /// <summary>
    /// Get the plugin info for a specific plugin by package name
    /// </summary>
    /// <param name="name">The package name of the plugin</param>
    /// <returns>The plugin info for the plugin</returns>
    public Task<PluginInfo?> GetByPackageName(string name)
        => new PluginManager().GetByPackageName(name);

    /// <summary>
    /// Gets a plugin by its UID
    /// </summary>
    /// <param name="uid">the UID of the plugin</param>
    /// <returns>the plugin</returns>
    public Task<PluginInfo?> GetByUid(Guid uid)
        => new PluginManager().GetByUid(uid);


    /// <inheritdoc />
    public Task<List<PluginInfo>> GetAllAsync()
        => new PluginManager().GetAll();
    
    /// <inheritdoc />
    public async Task<PluginInfo> Update(PluginInfo pluginInfo)
    {
        var result = await new PluginManager().Update(pluginInfo, false);
        return result.IsFailed ? null : result.Value;
    }

    /// <summary>
    /// Download a plugin
    /// </summary>
    /// <param name="plugin">the plugin to download</param>
    /// <returns>the byte data of the plugin</returns>
    public Task<byte[]> Download(PluginInfo plugin)
    {
        var result = new PluginController(null).DownloadPackage(plugin.PackageName);
        using var ms = new MemoryStream();
        result.FileStream.CopyTo(ms);
        return Task.FromResult(ms.ToArray());
    }

    /// <summary>
    /// Deletes items matching the UIDs
    /// </summary>
    /// <param name="uids">the UIDs of the items to delete</param>
    public async Task Delete(params Guid[] uids)
    {
        if (uids?.Any() != true)
            return;

        var manager = new PluginManager();
        
        var deleting = (await manager.GetAll()).Where(x => uids.Contains(x.Uid));
        await manager.Delete(uids);
        foreach(var item in deleting)
        {
            PluginScanner.Delete(item.PackageName);
            await manager.DeletePluginSettings(item.PackageName);
        }
        await manager.IncrementConfigurationRevision(force: true);
    }

    /// <summary>
    /// Gets the settings json for a plugin
    /// </summary>
    /// <param name="pluginSettingsType">the name of the plugin package</param>
    /// <returns>the settings json</returns>
    public async Task<string> GetSettingsJson(string pluginSettingsType)
    {
        var result = await new PluginManager().GetPluginSettings("PluginSettings_" + pluginSettingsType);
        if (result.IsFailed)
            return string.Empty;
        PluginSettingsModel model = result.Value;
        if (string.IsNullOrEmpty(model.Json))
            return string.Empty;
        
        // need to decode any passwords
        return await DecryptPluginJson(pluginSettingsType, model.Json);
    }
    
    
    /// <summary>
    /// Gets all plugin settings with and the plugin settings
    /// </summary>
    /// <returns>all plugin settings with and the plugin settings</returns>
    internal async Task<Dictionary<string, string>> GetAllPluginSettings()
    {
        var all = await new PluginManager().GetAllPluginSettings();
        var plugins = all
            .DistinctBy(x => x.Name.Replace("PluginSettings_", string.Empty))
            .ToDictionary(x => x.Name.Replace("PluginSettings_", string.Empty), x => x.Json);
        foreach (var plg in plugins)
        {
            if (string.IsNullOrEmpty(plg.Value))
                continue;
            plugins[plg.Key] = await DecryptPluginJson(plg.Key, plg.Value);
        }

        return plugins;
    }
    
    private async Task<string> DecryptPluginJson(string packageName, string json)
    {   
        try
        {
            var plugin = await GetByPackageName(packageName);
            if (string.IsNullOrEmpty(plugin?.Name))
                return json;
            
            bool updated = false;

            json = json.Replace("\\u0022", "\"");
            if (json.StartsWith("\"") && json.EndsWith("\""))
                json = json[1..^1];

            IDictionary<string, object> dict = JsonSerializer.Deserialize<ExpandoObject>(json) 
                as IDictionary<string, object> ?? new Dictionary<string, object>();
            foreach (var key in dict.Keys.ToArray())
            {
                if (plugin.Settings.Any(x => x.Name == key && x.InputType == Plugin.FormInputType.Password))
                {
                    // its a password, decrypt 
                    string text = string.Empty;
                    if (dict[key] is JsonElement je)
                    {
                        text = je.GetString() ?? String.Empty;
                    }
                    else if (dict[key] is string str)
                    {
                        text = str;
                    }

                    if (string.IsNullOrEmpty(text))
                        continue;

                    dict[key] = Helpers.Decrypter.Decrypt(text);
                    updated = true;
                }
            }
            if (updated)
                return JsonSerializer.Serialize(dict);
            return json;
        }
        catch (Exception ex)
        {
            Logger.Instance.WLog("Failed to decrypt passwords in plugin settings: " + ex.Message + Environment.NewLine + json);
        }

        return json;
    }
}