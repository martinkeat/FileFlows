using FileFlows.Managers;
using FileFlows.Server.Authentication;
using FileFlows.Server.Services;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace FileFlows.Server.Controllers.RemoteControllers;

/// <summary>
/// Basic info controller
/// </summary>
[Route("/remote/info")]
[FileFlowsApiAuthorize]
[ApiExplorerSettings(IgnoreApi = true)]
public class InfoController : Controller
{
    /// <summary>
    /// Gets the shrinkage groups
    /// </summary>
    /// <returns>the shrinkage groups</returns>
    [HttpGet("shrinkage-groups")]
    public async Task<List<ShrinkageData>> GetShrinkageGroups()
    {
        var groups = await new LibraryFileManager().GetShrinkageGroups();
        return groups;
    }
    
    /// <summary>
    /// Get the current status
    /// </summary>
    /// <returns>the current status</returns>
    [HttpGet("status")]
    public Task<StatusModel> Get()
        => new StatusService().Get();
    
    /// <summary>
    /// Gets if an update is available
    /// </summary>
    /// <returns>True if there is an update</returns>
    [HttpGet("update-available")]
    public Task<object> UpdateAvailable()
        => new StatusService().UpdateAvailable();
}