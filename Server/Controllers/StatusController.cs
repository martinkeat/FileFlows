using FileFlows.Server.Authentication;
using FileFlows.Server.Services;
using SettingsService = FileFlows.Server.Services.SettingsService;
using Microsoft.AspNetCore.Mvc;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Controllers;

/// <summary>
/// Status controller
/// </summary>
[Route("/api/status")]
[FileFlowsAuthorize]
public class StatusController : Controller
{
    /// <summary>
    /// Gets if an update is available
    /// </summary>
    /// <returns>True if there is an update</returns>
    [HttpGet("update-available")]
    public Task<object> UpdateAvailable()
        => new StatusService().UpdateAvailable();
    
    /// <summary>
    /// Get the current status
    /// </summary>
    /// <returns>the current status</returns>
    [HttpGet]
    public Task<StatusModel> Get()
        => new StatusService().Get();

}

