using FileFlows.Server.Helpers;
using FileFlows.ServerShared.Models;

namespace FileFlows.Server.Services;

using FileFlows.Server.Controllers;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Models;
using System;
using System.Threading.Tasks;

/// <summary>
/// An instance of the Settings Service which allows accessing of the system settings
/// </summary>
public class SettingsService : ISettingsService
{
    /// <summary>
    /// A loader to load an instance of the Settings
    /// </summary>
    public static Func<ISettingsService> Loader { get; set; }

    /// <summary>
    /// Loads an instance of the settings service
    /// </summary>
    /// <returns>an instance of the settings service</returns>
    public static ISettingsService Load()
    {
        if (Loader == null)
            return new SettingsService();
        return Loader.Invoke();
    }
    
    /// <summary>
    /// Gets the system settings
    /// </summary>
    /// <returns>the system settings</returns>
    public Task<Settings> Get() => new SettingsController().Get();

    /// <summary>
    /// Gets the file flows status
    /// </summary>
    /// <returns>the file flows status</returns>
    public Task<FileFlowsStatus> GetFileFlowsStatus() =>
        Task.FromResult(new SettingsController().GetFileFlowsStatus());


    /// <summary>
    /// Gets the current configuration revision number
    /// </summary>
    /// <returns>the current configuration revision number</returns>
    public Task<int> GetCurrentConfigurationRevision()
        => Task.FromResult(new SettingsController().GetCurrentConfigRevision());

    /// <summary>
    /// Gets the current configuration revision
    /// </summary>
    /// <returns>the current configuration revision</returns>
    public Task<ConfigurationRevision> GetCurrentConfiguration()
        => new SettingsController().GetCurrentConfig();
    
    /// <summary>
    /// Increments the revision
    /// </summary>
    public async Task RevisionIncrement()
    {
        var controller = new SettingsController();
        var settings = await controller.Get();
        settings.Revision += 1;
        await DbHelper.Update(settings);
    }
}