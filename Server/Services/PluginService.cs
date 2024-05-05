using System.Dynamic;
using System.Net;
using FileFlows.Managers;
using FileFlows.Plugin;
using FileFlows.Server.Helpers;
using FileFlows.Server.Controllers;
using FileFlows.ServerShared.Models;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;


namespace FileFlows.Server.Services;

/// <summary>
/// Plugin service
/// </summary>
public class PluginService
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


    /// <summary>
    /// Get all plugin infos
    /// </summary>
    /// <returns>all plugin infos</returns>
    public Task<List<PluginInfo>> GetAllAsync()
        => new PluginManager().GetAll();
    
    /// <summary>
    /// Updates plugin info
    /// </summary>
    /// <param name="pluginInfo">the plugin info</param>
    /// <param name="auditDetails">The audit details</param>
    /// <returns>the updated plugininfo</returns>
    public async Task<PluginInfo> Update(PluginInfo pluginInfo, AuditDetails? auditDetails)
    {
        var result = await new PluginManager().Update(pluginInfo, auditDetails, false);
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
    /// <param name="auditDetails">the audit details</param>
    public async Task Delete(Guid[] uids, AuditDetails auditDetails)
    {
        if (uids?.Any() != true)
            return;

        var manager = new PluginManager();
        
        var deleting = (await manager.GetAll()).Where(x => uids.Contains(x.Uid));
        await manager.Delete(uids, auditDetails);
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
        var result = await new PluginManager().GetPluginSettings(pluginSettingsType);
        if (result.IsFailed)
            return string.Empty;
        PluginSettingsModel? model = result.Value;
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
            .DistinctBy(x => x.Name)
            .ToDictionary(x => x.Name, x => x.Json);
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
                        text = je.GetString() ?? string.Empty;
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

    /// <summary>
    /// Sets the plugin settings
    /// </summary>
    /// <param name="name">the name of the plugin</param>
    /// <param name="json">the plugin json</param>
    /// <param name="auditDetails">The audit details</param>
    public Task SetSettingsJson(string name, string json, AuditDetails? auditDetails)
        => new PluginManager().SetPluginSettings(name, json, auditDetails);
    
    
    /// <summary>
    /// Get a list of all plugins in the system
    /// </summary>
    /// <param name="includeElements">If data should contain all the elements for the plugins</param>
    /// <returns>a list of plugins</returns>
    public async Task<IEnumerable<PluginInfoModel>> GetPluginInfoModels(bool includeElements = true)
    {
        var plugins = (await new Services.PluginService().GetAllAsync())
            .OrderBy(x => x.Name)
            .Where(x => x.Deleted == false);
        List<PluginInfoModel> pims = new List<PluginInfoModel>();
        var packagesResult = await GetPluginPackagesActual();
        var packages = packagesResult.IsFailed ? new () : packagesResult.Value;
        
        Dictionary<string, PluginInfoModel> pluginDict = new();
        foreach (var plugin in plugins)
        {
            var pim = new PluginInfoModel
            {
                Uid = plugin.Uid,
                Name = plugin.Name,
                DateCreated = plugin.DateCreated,
                DateModified = plugin.DateModified,
                Enabled = plugin.Enabled,
                Version = plugin.Version,
                Deleted = plugin.Deleted,
                Settings = plugin.Settings,
                Authors = plugin.Authors,
                Icon = plugin.Icon,
                Url = plugin.Url,
                PackageName = plugin.PackageName,
                Description = plugin.Description,   
                Elements = includeElements ? plugin.Elements : null
            };
            var package = packages.FirstOrDefault(x => x.Package.ToLowerInvariant().Replace(" ", "") == plugin.PackageName.ToLowerInvariant().Replace(" ", ""));
            pim.LatestVersion = VersionHelper.VersionDateString(package?.Version ?? string.Empty);
            pims.Add(pim);

            foreach (var ele in plugin.Elements)
            {
                pluginDict.TryAdd(ele.Uid, pim);
            }
        }

        string flowTypeName = typeof(Flow).FullName ?? string.Empty;
        var flows = await new Services.FlowService().GetAllAsync();
        foreach (var flow in flows)
        {
            foreach (var p in flow.Parts)
            {
                if (pluginDict.ContainsKey(p.FlowElementUid) == false)
                    continue;
                var plugin = pluginDict[p.FlowElementUid];
                if (plugin.UsedBy != null && plugin.UsedBy.Any(x => x.Uid == flow.Uid))
                    continue;
                plugin.UsedBy ??= new();
                plugin.UsedBy.Add(new ()
                {
                    Name = flow.Name,
                    Type = flowTypeName,
                    Uid = flow.Uid
                });
            }
        }
        return pims.OrderBy(x => x.Name.ToLowerInvariant());
    }
        
    
    /// <summary>
    /// Get the available plugin packages 
    /// </summary>
    /// <param name="missing">If only missing plugins should be included, ie plugins not installed</param>
    /// <returns>a list of plugins</returns>
    internal async Task<Result<List<PluginPackageInfo>>> GetPluginPackagesActual(bool missing = false)
    {
        List<PluginPackageInfo> data = new List<PluginPackageInfo>();
        try
        {
            string url = Globals.PluginBaseUrl + $"?version={Globals.Version}&rand={DateTime.UtcNow.ToFileTime()}";
            var plugins = await HttpHelper.Get<IEnumerable<PluginPackageInfo>>(url, timeoutSeconds: 10);
            if (plugins.Success == false)
            {
                if (plugins.StatusCode == HttpStatusCode.PreconditionFailed)
                    return Result<List<PluginPackageInfo>>.Fail("To access additional plugins, you must upgrade FileFlows to the latest version.");
            }
            foreach(var plugin in plugins.Data)
            {
                if (data.Any(x => x.Name == plugin.Name))
                    continue;

#if (!DEBUG)
                    if(string.IsNullOrWhiteSpace(plugin.MinimumVersion) == false)
                    {
                        Version ffVersion = new Version(Globals.Version);
                        if (ffVersion < new Version(plugin.MinimumVersion))
                            continue;
                    }
#endif
                data.Add(plugin);
            }
        }
        catch (Exception) { }

        // remove plugins already installed
        var installed = (await new PluginService().GetAllAsync())
            .Where(x => x.Deleted != true).Select(x => x.PackageName).ToList();
        
        if (missing)
        {
            data = data.Where(x => installed.Contains(x.Package) == false).ToList();
        }
        else
        {
            foreach (var d in data)
            {
                d.Installed = installed.Contains(d.Package);
            }
        }

        return data.OrderBy(x => x.Name).ToList();
    }

    /// <summary>
    /// Download pluges
    /// </summary>
    /// <param name="plugins">the plugins to download</param>
    public async Task DownloadPlugins(List<PluginPackageInfo> plugins)
    {
        var pluginDownloader = new PluginDownloader();
        foreach(var package in plugins)
        {
            try
            {
                var dlResult = await pluginDownloader.Download(Version.Parse(package.Version), package.Package);
                if (dlResult.Success)
                {
                    PluginScanner.UpdatePlugin(package.Package, dlResult.Data);
                }
            }
            catch (Exception ex)
            { 
                Logger.Instance?.ELog($"Failed downloading plugin package: '{package}' => {ex.Message}");
            }
        }
    }
}