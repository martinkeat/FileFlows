using FileFlows.Server.Controllers;
using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Upgrade;

/// <summary>
/// Upgrade to FileFlows v0.8.1
/// </summary>
public class Upgrade_0_8_1
{
    /// <summary>
    /// Runs the update
    /// </summary>
    /// <param name="settings">the settings</param>
    public void Run(Settings settings)
    {
        Logger.Instance.ILog("Upgrade running, running 0.8.1 upgrade script");
        DownloadLegacyVideoNodes(settings);
    }
    
    private void DownloadLegacyVideoNodes(Settings settings)
    {
        // get the plugins we need 
        var pluginDir = PluginScanner.GetPluginDirectory();

        string file = Path.Combine(pluginDir, "VideoLegacyNodes.ffplugin");
        if (File.Exists(file))
            return;
        
        var repos = settings.PluginRepositoryUrls?.Select(x => x)?.ToList() ?? new List<string>();
        if (repos.Contains(PluginController.PluginBaseUrl) == false)
            repos.Add(PluginController.PluginBaseUrl);
        
        foreach(var repo in repos)
            Logger.Instance.ILog("Plugin repository: " + repo);
        
        var pluginDownloader = new PluginDownloader(repos);
        
        // now download it
        var result = pluginDownloader.Download("VideoLegacyNodes");
        if (result.Success == false)
        {
            Logger.Instance.WLog("Failed download VideoLegacyNodes plugin");
            return;
        }

        File.WriteAllBytes(file, result.Data);
        Shared.Logger.Instance.ILog("Successfully downloaded VideoLegacyNodes plugin");
        // now scan the plugins again
        PluginScanner.Scan();
    }
}