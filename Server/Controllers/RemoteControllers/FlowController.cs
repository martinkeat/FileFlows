using FileFlows.Server.Authentication;
using FileFlows.Server.Services;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace FileFlows.Server.Controllers.RemoteControllers;

/// <summary>
/// Remote flow controllers
/// </summary>
[Route("/remote/flow")]
[FileFlowsApiAuthorize]
[ApiExplorerSettings(IgnoreApi = true)]
public class FlowController : Controller
{
    /// <summary>
    /// Gets the flow
    /// </summary>
    /// <param name="uid">the UID of the flow</param>
    /// <returns>the flow</returns>
    [HttpGet("{uid}")]
    public Task<Flow?> GetFlow([FromRoute] Guid uid)
        => ServiceLoader.Load<FlowService>().GetByUidAsync(uid);
    
    /// <summary>
    /// Gets the failure flow for a particular library
    /// </summary>
    /// <param name="libraryUid">the UID of the library</param>
    /// <returns>the failure flow</returns>
    [HttpGet("failure-flow-by-library/{libraryUid}")]
    public Task<Flow?> GetFailureFlow([FromRoute] Guid libraryUid)
        => ServiceLoader.Load<FlowService>().GetFailureFlow(libraryUid);
}