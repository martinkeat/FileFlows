using System.Dynamic;
using FileFlows.Server.Helpers;
using FileFlows.Server.Controllers;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Models;


namespace FileFlows.Server.Services;
/// <summary>
/// Plugin service
/// </summary>
public class PluginService : CachedService<PluginInfo>, IPluginService
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
    
    /// <summary>
    /// Updates plugin info
    /// </summary>
    /// <param name="pluginInfo">the plugin info</param>
    /// <returns>the updated plugininfo</returns>
    public Task<PluginInfo> Update(PluginInfo pluginInfo)
    {
        Update(pluginInfo, false);
        return Task.FromResult(pluginInfo);
    }

    /// <summary>
    /// Download a plugin
    /// </summary>
    /// <param name="plugin">the plugin to download</param>
    /// <returns>the byte data of the plugin</returns>
    public Task<byte[]> Download(PluginInfo plugin)
    {
        var result = new PluginController().DownloadPackage(plugin.PackageName);
        using var ms = new MemoryStream();
        result.FileStream.CopyTo(ms);
        return Task.FromResult(ms.ToArray());
    }

    /// <summary>
    /// Deletes items matching the UIDs
    /// </summary>
    /// <param name="uids">the UIDs of the items to delete</param>
    public override async Task Delete(params Guid[] uids)
    {
        if (uids?.Any() != true)
            return;
        
        var deleting = Data.Where(x => uids.Contains(x.Uid));
        await DbHelper.Delete(uids);
        foreach(var item in deleting)
        {
            PluginScanner.Delete(item.PackageName);
            // delete the plugin settings
            await DbHelper.Execute( $"delete from DbObject where Type = '{typeof(PluginSettingsModel).FullName}' and Name = 'PluginSettings_{item.PackageName}'");
        }
        IncrementConfigurationRevision();
        
        Refresh();
    }

    /// <summary>
    /// Gets the settings json for a plugin
    /// </summary>
    /// <param name="pluginSettingsType">the name of the plugin package</param>
    /// <returns>the settings json</returns>
    public async Task<string> GetSettingsJson(string pluginSettingsType)
    {
        var obj = await DbHelper.SingleByName<Models.PluginSettingsModel>("PluginSettings_" + pluginSettingsType);
        if (obj == null)
            return string.Empty;

        // need to decode any passwords
        if (string.IsNullOrEmpty(obj.Json) == false)
            obj.Json = DecryptPluginJson(pluginSettingsType, obj.Json);

        return obj.Json ?? string.Empty;
    }
    
    
    /// <summary>
    /// Gets all plugin settings with and the plugin settings
    /// </summary>
    /// <returns>all plugin settings with and the plugin settings</returns>
    internal async Task<Dictionary<string, string>> GetAllPluginSettings()
    {
        string sqlPluginSettings = "select Name, " +
                                   SqlHelper.JsonValue("Data", "Json") +
                                   " from DbObject where Type = 'FileFlows.Server.Models.PluginSettingsModel'";
        var plugins = (await DbHelper.GetDbManager()
                .Fetch<(string Name, string Json)>(sqlPluginSettings))
            .DistinctBy(x => x.Name.Replace("PluginSettings_", string.Empty))
            .ToDictionary(x => x.Name.Replace("PluginSettings_", string.Empty), x => x.Json);
        foreach (var plg in plugins)
        {
            if (string.IsNullOrEmpty(plg.Value))
                continue;
            plugins[plg.Key] = DecryptPluginJson(plg.Key, plg.Value);
        }

        return plugins;
    }
    
    private string DecryptPluginJson(string packageName, string json)
    {   
        try
        {
            var plugin = GetByPackageName(packageName);
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