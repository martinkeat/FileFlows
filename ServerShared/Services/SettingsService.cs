using FileFlows.ServerShared.Models;

namespace FileFlows.ServerShared.Services;

using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;

/// <summary>
/// Interface for the Settings service which allows accessing of all the system settings
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets the system settings
    /// </summary>
    /// <returns>the system settings</returns>
    Task<Settings> Get();
    
    /// <summary>
    /// Gets the file flows status
    /// </summary>
    /// <returns>the file flows status</returns>
    Task<FileFlowsStatus> GetFileFlowsStatus();

    /// <summary>
    /// Gets the current configuration revision number
    /// </summary>
    /// <returns>the current configuration revision number</returns>
    Task<int> GetCurrentConfigurationRevision();

    /// <summary>
    /// Gets the current configuration revision
    /// </summary>
    /// <returns>the current configuration revision</returns>
    Task<ConfigurationRevision> GetCurrentConfiguration();
}

/// <summary>
/// An instance of the Settings Service which allows accessing of the system settings
/// </summary>
public class SettingsService : Service, ISettingsService
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
    public async Task<Settings> Get()
    {
        try
        {
            var result = await HttpHelper.Get<Settings>($"{ServiceBaseUrl}/api/settings");
            if (result.Success == false)
                throw new Exception("Failed to get settings: " + result.Body);
            return result.Data;
        }
        catch (Exception ex)
        {
            Logger.Instance?.WLog("Failed to get settings: " + ex.Message);
            return null;
        }
    }

    public async Task<FileFlowsStatus> GetFileFlowsStatus()
    {
        try
        {
            var result = await HttpHelper.Get<FileFlowsStatus>($"{ServiceBaseUrl}/api/settings/fileflows-status");
            if (result.Success == false)
                throw new Exception("Failed to get FileFlows status: " + result.Body);
            return result.Data;
        }
        catch (Exception ex)
        {
            Logger.Instance?.WLog("Failed to get FileFlows status: " + ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Gets the current configuration revision number
    /// </summary>
    /// <returns>the current configuration revision number</returns>
    public async Task<int> GetCurrentConfigurationRevision()
    {
        try
        {
            var result = await HttpHelper.Get<int>($"{ServiceBaseUrl}/api/settings/current-config/revision");
            if (result.Success == false)
                throw new Exception("Failed to get FileFlows current configuration revision: " + result.Body);
            return result.Data;
        }
        catch (Exception ex)
        {
            Logger.Instance?.WLog("Failed to get FileFlows current configuration revision: " + ex.Message);
            return -1;
        }
    }

    /// <summary>
    /// Gets the current configuration revision
    /// </summary>
    /// <returns>the current configuration revision</returns>
    public async Task<ConfigurationRevision> GetCurrentConfiguration()
    {
        try
        {
            var result = await HttpHelper.Get<ConfigurationRevision>($"{ServiceBaseUrl}/api/settings/current-config");
            if (result.Success == false)
                throw new Exception("Failed to get FileFlows current configuration: " + result.Body);
            return result.Data;
        }
        catch (Exception ex)
        {
            Logger.Instance?.WLog("Failed to get FileFlows current configuration: " + ex.Message);
            return null;
        }
    }
}
