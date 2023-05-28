using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Helpers;

/// <summary>
/// Helper class to download plugins from repos
/// </summary>
public class PluginDownloader
{
    private List<string> Repositories;
    
    /// <summary>
    /// Constructs a plugin download
    /// </summary>
    /// <param name="repositories">the available repositories to download a plugin from</param>
    public PluginDownloader(List<string> repositories)
    {
        this.Repositories = repositories;
    }
    

    /// <summary>
    /// Downloads a plugin binary from the repository
    /// </summary>
    /// <param name="packageName">the package name of the plugin to download</param>
    /// <returns>the download result</returns>
    internal (bool Success, byte[] Data) Download(string packageName)
    {
        Logger.Instance.ILog("Downloading Plugin Package: " + packageName);
        Version ffVersion = new Version(Globals.Version);
        foreach (string repo in Repositories)
        {
            try
            {
                var plugins = HttpHelper.Get<IEnumerable<PluginPackageInfo>>(repo + "?rand=" + DateTime.Now.ToFileTime()).Result;
                if (plugins.Success == false)
                {
                    Logger.Instance.ILog("Plugin repository failed to download.");
                    continue;
                }

                var plugin = plugins?.Data?.Where(x => x.Package.Replace(".ffplugin", string.Empty).ToLower() == packageName.Replace(".ffplugin", string.Empty).ToLower())?.FirstOrDefault();
                if (plugin == null)
                {
                    Logger.Instance.ILog("Plugin not found in repository: " + packageName);
                    continue;
                }

                if(string.IsNullOrWhiteSpace(plugin.MinimumVersion) == false)
                {
                    if (ffVersion < Version.Parse(plugin.MinimumVersion))
                        continue;
                }

                string url = repo + "/download/" + packageName;
                if (url.EndsWith(".ffplugin") == false)
                    url += ".ffplugin";
                Logger.Instance.ILog("Downloading plugin from: " + url);
                var dlResult = HttpHelper.Get<byte[]>(url).Result;
                if (dlResult.Success)
                    return (true, dlResult.Data);
                throw new Exception(dlResult.Body);
            }
            catch (Exception ex)
            {
                Logger.Instance.WLog("Failed downloading plugin: " + ex.Message);
            }
        }
        return (false, new byte[0]);
    }
}