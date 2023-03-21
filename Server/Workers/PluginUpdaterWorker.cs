using FileFlows.Server.Helpers;
using FileFlows.Server.Controllers;
using FileFlows.ServerShared.Workers;

namespace FileFlows.Server.Workers;

/// <summary>
/// Worker to update plugins
/// </summary>
public class PluginUpdaterWorker : Worker
{
    /// <summary>
    /// Constructs a new plugin update worker
    /// </summary>
    public PluginUpdaterWorker() : base(ScheduleType.Daily, 5)
    {
        Trigger();
    }

    /// <summary>
    /// Executes the worker
    /// </summary>
    protected override void Execute()
    {
        var settings = new SettingsController().Get().Result;
#if (DEBUG)
        settings = null;
#endif
        if (settings?.AutoUpdatePlugins != true)
            return;

        Logger.Instance?.ILog("Plugin Updater started");
        var controller = new PluginController();
        var plugins = controller.GetAll().Result;
        var latestPackages = controller.GetPluginPackages().Result;

        var pluginDownloader = new PluginDownloader(controller.GetRepositories());
        
        foreach(var plugin in plugins)
        {
            try
            {
                var package = latestPackages?.Where(x => x?.Package == plugin?.PackageName)?.FirstOrDefault();
                if (package == null)
                    continue; // no plugin, so no update

                if (Version.Parse(package.Version) <= Version.Parse(plugin.Version))
                {
                    // no new version, cannot update
                    continue;
                }

                var dlResult = pluginDownloader.Download(package.Package);

                if (dlResult.Success == false)
                {
                    Logger.Instance.WLog($"Failed to download package '{plugin.PackageName}' update");
                    continue;
                }
                Helpers.PluginScanner.UpdatePlugin(package.Package, dlResult.Data);
            }
            catch(Exception ex)
            {
                Logger.Instance.WLog($"Failed to update plugin '{plugin.PackageName}': " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }
        Logger.Instance?.ILog("Plugin Updater finished");
    }
}
