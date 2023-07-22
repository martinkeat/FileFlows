using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Helpers;

/// <summary>
/// Helper class to download plugins from repos
/// </summary>
public class PluginDownloader
{
    /// <summary>
    /// Constructs a plugin download
    /// </summary>
    public PluginDownloader()
    {
    }
    

    /// <summary>
    /// Downloads a plugin binary from the repository
    /// </summary>
    /// <param name="version">the version of the plugin to download</param>
    /// <param name="packageName">the package name of the plugin to download</param>
    /// <returns>the download result</returns>
    internal (bool Success, byte[] Data) Download(Version version, string packageName)
    {
        Logger.Instance.ILog("Downloading Plugin Package: " + packageName);
        Version ffVersion = new Version(Globals.Version);
        try
        {
            string url = Globals.PluginBaseUrl + "/download/" + packageName + $"?version={version}&rand=" + DateTime.Now.ToFileTime();
            var dlResult = HttpHelper.Get<byte[]>(url).Result;
            if (dlResult.Success)
                return (true, dlResult.Data);
            throw new Exception(dlResult.Body?.EmptyAsNull() ?? "Unexpected error");
        }
        catch (Exception ex)
        {
            Logger.Instance.WLog($"Failed downloading plugin '{packageName}': " + ex.Message);
        }
        return (false, new byte[0]);
    }
}