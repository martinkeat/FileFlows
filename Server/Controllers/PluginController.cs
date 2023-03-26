using FileFlows.Server.Services;
using FileFlows.ServerShared.Services;

namespace FileFlows.Server.Controllers;

using System.ComponentModel;
using System.Dynamic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using FileFlows.Plugin.Attributes;
using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;
using FileFlows.Shared.Helpers;
using FileFlows.ServerShared.Helpers;
using System.Text.Json;

/// <summary>
/// Plugin Controller
/// </summary>
[Route("/api/plugin")]
public class PluginController : Controller
{
    internal const string PLUGIN_BASE_URL = "https://fileflows.com/api/plugin";

    /// <summary>
    /// Get a list of all plugins in the system
    /// </summary>
    /// <param name="includeElements">If data should contain all the elements for the plugins</param>
    /// <returns>a list of plugins</returns>
    [HttpGet]
    public async Task<IEnumerable<PluginInfoModel>> GetAll(bool includeElements = true)
    {
        var plugins = new Services.PluginService().GetAll()
            .Where(x => x.Deleted == false);
        List<PluginInfoModel> pims = new List<PluginInfoModel>();
        var packages = await GetPluginPackages();

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
                Url =  plugin.Url,
                PackageName = plugin.PackageName,
                Description = plugin.Description,   
                Elements = includeElements ? plugin.Elements : null
            };
            var package = packages.FirstOrDefault(x => x.Name.ToLower().Replace(" ", "") == plugin.Name.ToLower().Replace(" ", ""));
            pim.LatestVersion = package?.Version ?? "";
            pims.Add(pim);

            foreach (var ele in plugin.Elements)
            {
                if (pluginDict.ContainsKey(ele.Uid) == false)
                    pluginDict.Add(ele.Uid, pim);
            }
        }

        string flowTypeName = typeof(Flow).FullName ?? string.Empty;
        var flows = new Services.FlowService().GetAll();
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
    /// Get the plugin info for a specific plugin
    /// </summary>
    /// <param name="uid">The uid of the plugin</param>
    /// <returns>The plugin info for the plugin</returns>
    [HttpGet("{uid}")]
    public PluginInfo Get([FromRoute] Guid uid)
        => new Services.PluginService().GetByUid(uid) ?? new();

    /// <summary>
    /// Get the plugin info for a specific plugin by package name
    /// </summary>
    /// <param name="name">The package name of the plugin</param>
    /// <returns>The plugin info for the plugin</returns>
    [HttpGet("by-package-name/{name}")]
    public PluginInfo GetByPackageName([FromRoute] string name)
        => new Services.PluginService().GetByPackageName(name);

    /// <summary>
    /// Get the plugins translation file
    /// </summary>
    /// <param name="langCode">The language code to get the translations for</param>
    /// <returns>The json plugin translation file</returns>
    [HttpGet("language/{langCode}.json")]
    public IActionResult LanguageFile([FromQuery] string langCode = "en")
        => File("i18n/plugins.en.json", "text/json");

    /// <summary>
    /// Get the available plugin packages 
    /// </summary>
    /// <param name="missing">If only missing plugins should be included, ie plugins not installed</param>
    /// <returns>a list of plugins</returns>
    [HttpGet("plugin-packages")]
    public async Task<IEnumerable<PluginPackageInfo>> GetPluginPackages([FromQuery] bool missing = false)
    {
        Version ffVersion = Globals.Version;
        List<PluginPackageInfo> data = new List<PluginPackageInfo>();
        var repos = GetRepositories();
        foreach (string repo in repos)
        {
            try
            {
                var plugins = await HttpHelper.Get<IEnumerable<PluginPackageInfo>>(repo + "?rand=" + System.DateTime.Now.ToFileTime());
                if (plugins.Success == false)
                    continue;
                foreach(var plugin in plugins.Data)
                {
                    if (data.Any(x => x.Name == plugin.Name))
                        continue;

#if (!DEBUG)
                    if(string.IsNullOrWhiteSpace(plugin.MinimumVersion) == false)
                    {
                        if (ffVersion < new Version(plugin.MinimumVersion))
                            continue;
                    }
#endif
                    data.Add(plugin);
                }
            }
            catch (Exception) { }
        }

#if (DEBUG)
#endif

        if (missing)
        {
            // remove plugins already installed
            var installed = new Services.PluginService().GetAll()
                .Where(x => x.Deleted != true).Select(x => x.PackageName).ToList();
            return data.Where(x => installed.Contains(x.Package) == false);
        }

        return data.OrderBy(x => x.Name);
    }

    /// <summary>
    /// Download the latest updates for plugins from the Plugin Repository
    /// </summary>
    /// <param name="model">The list of plugins to update</param>
    /// <returns>if the updates were successful or not</returns>
    [HttpPost("update")]
    public async Task<bool> Update([FromBody] ReferenceModel<Guid> model)
    {
        bool updated = false;
        var plugins = await GetPluginPackages();

        var pluginDownloader = new PluginDownloader(this.GetRepositories());
        foreach (var uid in model?.Uids ?? new Guid[] { })
        {
            var plugin = new Services.PluginService().GetByUid(uid);
            if (plugin == null)
                continue;

            var ppi = plugins.FirstOrDefault(x => x.Name.Replace(" ", "").ToLower() == plugin.Name.Replace(" ", "").ToLower());

            if (ppi == null)
            {
                Logger.Instance.WLog("PluginUpdate: No plugin info found for plugin: " + plugin.Name);
                continue;
            }

            if (Version.Parse(ppi.Version) <= Version.Parse(plugin.Version))
            {
                // no new version, cannot update
                Logger.Instance.WLog("PluginUpdate: No newer version to download for plugin: " + plugin.Name);
                continue;
            }

            var dlResult = pluginDownloader.Download(ppi.Package);
            if (dlResult.Success == false)
            {
                Logger.Instance.WLog("PluginUpdate: Failed to download plugin");
                continue;
            }

            // save the ffplugin file
            bool success = PluginScanner.UpdatePlugin(ppi.Package, dlResult.Data);
            if(success)
                Logger.Instance.ILog("PluginUpdate: Successfully updated plugin: " + plugin.Name);
            else
                Logger.Instance.WLog("PluginUpdate: Failed to updated plugin: " + plugin.Name);

            updated |= success;
        }
        
        return updated;
    }

    /// <summary>
    /// Delete plugins from the system
    /// </summary>
    /// <param name="model">A reference model containing UIDs to delete</param>
    /// <returns>an awaited task</returns>
    [HttpDelete]
    public Task Delete([FromBody] ReferenceModel<Guid> model)
        => new Services.PluginService().Delete(model.Uids);

    /// <summary>
    /// Download plugins into the FileFlows system
    /// </summary>
    /// <param name="model">A list of plugins to download</param>
    /// <returns>an awaited task</returns>
    [HttpPost("download")]
    public void Download([FromBody] DownloadModel model)
    {
        if (model == null || model.Packages?.Any() != true)
            return; // nothing to delete

        var pluginDownloader = new PluginDownloader(GetRepositories());
        foreach(var package in model.Packages)
        {
            try
            {
                var dlResult = pluginDownloader.Download(package);
                if (dlResult.Success)
                {
                    PluginScanner.UpdatePlugin(package, dlResult.Data);
                }
            }
            catch (Exception ex)
            { 
                Logger.Instance?.ELog($"Failed downloading plugin package: '{package}' => {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Download the plugin ffplugin file.  Only intended to be used by the FlowRunner
    /// </summary>
    /// <param name="package">The plugin package name to download</param>
    /// <returns>A download stream of the ffplugin file</returns>
    [HttpGet("download-package/{package}")]
    public FileStreamResult DownloadPackage([FromRoute] string package)
    {
        if (string.IsNullOrEmpty(package))
        {
            Logger.Instance?.ELog("Download Package Error: package not set");
            throw new ArgumentNullException(nameof(package));
        }
        if (package.EndsWith(".ffplugin") == false)
            package += ".ffplugin";

        if(System.Text.RegularExpressions.Regex.IsMatch(package, "^[a-zA-Z0-9_\\-]+\\.ffplugin$") == false)
        {
            Logger.Instance?.ELog("Download Package Error: invalid package: " + package);
            throw new Exception("Download Package Error: invalid package: " + package);
        }

        string dir = PluginScanner.GetPluginDirectory();
        string file = Path.Combine(dir, package);

        if (System.IO.File.Exists(file) == false)
        {
            Logger.Instance?.ELog("Download Package Error: File not found => " + file);
            throw new Exception("File not found");
        }

        try
        {
            return File(System.IO.File.OpenRead(file), "application/octet-stream");
        }
        catch(Exception ex)
        {
            Logger.Instance?.ELog("Download Package Error: Failed to read data => " + ex.Message); ;
            throw;
        }
    }

    /// <summary>
    /// Gets the json plugin settings for a plugin
    /// </summary>
    /// <param name="packageName">The full plugin name</param>
    /// <returns>the plugin settings json</returns>
    [HttpGet("{packageName}/settings")]
    public Task<string> GetPluginSettings([FromRoute] string packageName)
        => new Services.PluginService().GetSettingsJson(packageName);

    /// <summary>
    /// Sets the json plugin settings for a plugin
    /// </summary>
    /// <param name="packageName">The full plugin name</param>
    /// <param name="json">the settings json</param>
    /// <returns>an awaited task</returns>
    [HttpPost("{packageName}/settings")]
    public async Task SetPluginSettingsJson([FromRoute] string packageName, [FromBody] string json)
    {
        // need to decode any passwords
        if (string.IsNullOrEmpty(json) == false)
        {
            try
            {
                var plugin = GetByPackageName(packageName);
                if (string.IsNullOrEmpty(plugin?.Name) == false)
                {
                    bool updated = false;

                    IDictionary<string, object> dict = JsonSerializer.Deserialize<ExpandoObject>(json) as IDictionary<string, object> ?? new Dictionary<string, object>();
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

                            dict[key] = Helpers.Decrypter.Encrypt(text);
                            updated = true;
                        }
                    }
                    if (updated)
                        json = JsonSerializer.Serialize(dict);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.WLog("Failed to encrypting passwords in plugin settings: " + ex.Message);
            }
        }

        var obj = await DbHelper.SingleByName<Models.PluginSettingsModel>("PluginSettings_" + packageName);
        obj ??= new Models.PluginSettingsModel();
        obj.Name = "PluginSettings_" + packageName;
        var newJson = json ?? string.Empty;
        if (newJson != obj.Json)
        {
            obj.Json = json ?? String.Empty;
            await DbHelper.Update(obj);
            // need to increment the revision increment so these plugin settings are pushed to the nodes
            await new Services.SettingsService().RevisionIncrement();
        }
    }

    
    /// <summary>
    /// Set state of a processing node
    /// </summary>
    /// <param name="uid">The UID of the processing node</param>
    /// <param name="enable">Whether or not this node is enabled and will process files</param>
    /// <returns>an awaited task</returns>
    [HttpPut("state/{uid}")]
    public async Task<PluginInfo> SetState([FromRoute] Guid uid, [FromQuery] bool? enable)
    {
        var service = new Services.PluginService();
        var plugin = service.GetByUid(uid);
        if (plugin == null)
            throw new Exception("Node not found.");
        if (enable != null && plugin.Enabled != enable.Value)
        {
            plugin.Enabled = enable.Value;
            await service.Update(plugin);
        }

        return plugin;
    }
    
    /// <summary>
    /// Download model
    /// </summary>
    public class DownloadModel
    {
        /// <summary>
        /// A list of plugin packages to download
        /// </summary>
        public List<string> Packages { get; set; }
    }

    internal List<string> GetRepositories()
    {
        var repos = new SettingsController().Get().Result.PluginRepositoryUrls?.Select(x => x)?.ToList() ?? new List<string>();
        if (repos.Contains(PLUGIN_BASE_URL) == false)
            repos.Add(PLUGIN_BASE_URL);
        return repos;
    }
}
