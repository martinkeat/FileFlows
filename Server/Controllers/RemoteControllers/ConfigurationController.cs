using System.Text.RegularExpressions;
using FileFlows.Server.Authentication;
using FileFlows.Server.Services;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace FileFlows.Server.Controllers.RemoteControllers;

/// <summary>
/// Remote flow controllers
/// </summary>
[Route("/remote/configuration")]
[FileFlowsApiAuthorize]
[ApiExplorerSettings(IgnoreApi = true)]
public class ConfigurationController : Controller
{
    /// <summary>
    /// Gets the current configuration revision
    /// </summary>
    /// <returns>the current revision</returns>
    [HttpGet("revision")]
    public Task<int> GetCurrentConfigRevision()
        => ServiceLoader.Load<ISettingsService>().GetCurrentConfigurationRevision();
    
    /// <summary>
    /// Loads the current configuration
    /// </summary>
    /// <returns>the current configuration</returns>
    [HttpGet("current-config")]
    public Task<ConfigurationRevision?> GetCurrentConfig()
        => ServiceLoader.Load<ISettingsService>().GetCurrentConfiguration();
    
    /// <summary>
    /// Loads the system settings
    /// </summary>
    /// <returns>the system settings</returns>
    [HttpGet("settings")]
    public Task<Settings?> GetSettings()
        => ServiceLoader.Load<ISettingsService>().Get();
    
    /// <summary>
    /// Downloads a plugin
    /// </summary>
    /// <param name="name">the name of the plugin</param>
    /// <returns>the plugin file</returns>
    [HttpGet("download-plugin/{name}")]
    public IActionResult DownloadPlugin(string name)
    {
        Logger.Instance?.ILog("DownloadPlugin: " + name);
        if (Regex.IsMatch(name, @"^[a-zA-Z][a-zA-Z0-9\-\._+]+\.ffplugin$", RegexOptions.CultureInvariant) == false)
        {
            Logger.Instance?.WLog("DownloadPlugin.Invalid Plugin: " + name);
            return BadRequest("Invalid plugin: " + name);
        }

        var file = new FileInfo(Path.Combine(DirectoryHelper.PluginsDirectory, name));
        if (file.Exists == false)
            return NotFound(); // Plugin file not found

        var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return File(stream, "application/octet-stream");
    }

}