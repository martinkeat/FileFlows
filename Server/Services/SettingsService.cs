using FileFlows.Managers;
using FileFlows.Plugin;
using FileFlows.Server.Helpers;
using FileFlows.ServerShared.Models;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// An instance of the Settings Service which allows accessing of the system settings
/// </summary>
public class SettingsService // : ISettingsService
{
    /// <summary>
    /// Gets the system settings
    /// </summary>
    /// <returns>the system settings</returns>
    public async Task<Settings> Get()
    {
        var settings = await new SettingsManager().Get();
        settings.IsWindows = OperatingSystem.IsWindows();
        settings.IsDocker = Application.Docker;

        if (LicenseHelper.IsLicensed() == false)
            settings.LogFileRetention = 2;
        
        return settings;
    }

    /// <summary>
    /// Gets the current configuration revision number
    /// </summary>
    /// <returns>the current configuration revision number</returns>
    public Task<int> GetCurrentConfigurationRevision()
        => new SettingsManager().GetCurrentConfigurationRevision();

    /// <summary>
    /// Gets the current configuration revision
    /// </summary>
    /// <returns>the current configuration revision</returns>
    public async Task<ConfigurationRevision> GetCurrentConfiguration()   
    {
        var settings = await new SettingsManager().Get();
        var cfg = new ConfigurationRevision();
        cfg.Revision = settings.Revision;
        var scriptService = new ScriptService();
        cfg.FlowScripts = (await scriptService.GetAllByType(ScriptType.Flow)).ToList();
        cfg.SystemScripts = (await scriptService.GetAllByType(ScriptType.System)).ToList();
        cfg.SharedScripts = (await scriptService.GetAllByType(ScriptType.Shared)).ToList();
        cfg.Variables = (await ServiceLoader.Load<VariableService>().GetAllAsync()).ToDictionary(x => x.Name, x => x.Value);
        cfg.Flows = await ServiceLoader.Load<FlowService>().GetAllAsync();
        cfg.Libraries = await ServiceLoader.Load<LibraryService>().GetAllAsync();
        cfg.Enterprise = LicenseHelper.IsLicensed(LicenseFlags.Enterprise);
        cfg.AllowRemote = settings.FileServerDisabled == false && LicenseHelper.IsLicensed(LicenseFlags.FileServer);
        cfg.PluginSettings = await new PluginService().GetAllPluginSettings();
        cfg.MaxNodes = LicenseHelper.IsLicensed() ? 250 : 30;
        cfg.KeepFailedFlowTempFiles = settings.KeepFailedFlowTempFiles;
        cfg.Enterprise = LicenseHelper.IsLicensed(LicenseFlags.Enterprise);
        var pluginInfos = (await ServiceLoader.Load<PluginService>().GetPluginInfoModels(true))
            .Where(x => x.Enabled)
            .ToDictionary(x => x.PackageName + ".ffplugin", x => x);
        var plugins = new List<string>();
        var pluginNames = new List<string>();
        List<string> flowElementsInUse = cfg.Flows.SelectMany(x => x.Parts.Select(x => x.FlowElementUid)).ToList();
        
        // Logger.Instance.DLog("Plugin, Flow Elements in Use: \n" + string.Join("\n", flowElementsInUse));

        foreach (var file in new DirectoryInfo(DirectoryHelper.PluginsDirectory).GetFiles("*.ffplugin"))
        {
            Logger.Instance.DLog($"Plugin found '{file.Name}'");
            if (pluginInfos.ContainsKey(file.Name) == false)
            {
                Logger.Instance.DLog($"Plugin '{file.Name}' not enabled skipping for configuration.");
                continue; // not enabled, skipped
            }

            var pluginInfo = pluginInfos[file.Name];
            
            var inUse = pluginInfo.Elements.Any(x => flowElementsInUse.Contains(x.Uid));
            if (inUse == false)
            {
                Logger.Instance.DLog($"Plugin '{pluginInfo.Name}' not in use by any flow, skipping");
                Logger.Instance.DLog("Plugin not using flow parts: " + string.Join(", ", pluginInfo.Elements.Select(x => x.Uid)));
                continue; // plugin not used, skipping
            }

            Logger.Instance.DLog($"Plugin '{pluginInfo.Name}' is used in configuration.");
            plugins.Add(file.Name);//, await System.IO.File.ReadAllBytesAsync(file.FullName));
            pluginNames.Add(pluginInfo.Name);
        }

        cfg.Plugins = plugins;
        cfg.PluginNames = pluginNames;
        Logger.Instance.DLog($"Plugin list that is used in configuration:", string.Join(", ", plugins));
        
        return cfg;
    }

    /// <inheritdoc />
    public Task<Result<string>> DownloadPlugin(string name, string destinationPath)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Increments the revision
    /// </summary>
    public Task RevisionIncrement()
        => new SettingsManager().RevisionIncrement();

    /// <summary>
    /// Saves the settings
    /// </summary>
    /// <param name="model">the settings model</param>
    /// <param name="auditDetails">the audit details</param>
    public async Task Save(Settings model, AuditDetails auditDetails)
    {
        var settings = await Get() ?? model;
        model.Name = settings.Name;
        model.Uid = settings.Uid;
        model.DateCreated = settings.DateCreated;
        model.IsWindows = OperatingSystem.IsWindows();
        model.IsDocker = Application.Docker;
        await new SettingsManager().Update(model, auditDetails);
    }
}