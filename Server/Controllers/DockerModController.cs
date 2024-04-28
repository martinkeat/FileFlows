using FileFlows.Server.Authentication;
using FileFlows.Server.Services;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace FileFlows.Server.Controllers;

/// <summary>
/// Library controller
/// </summary>
[Route("/api/dockermod")]
[FileFlowsAuthorize(UserRole.DockerMods)]
public class DockerModController : BaseController
{
    /// <summary>
    /// Gets all DockerMods in the system
    /// </summary>
    /// <returns>a list of all DockerMods</returns>
    [HttpGet]
    public async Task<IEnumerable<DockerMod>> GetAll() 
        => (await ServiceLoader.Load<DockerModService>().GetAll()).OrderBy(x => x.Name.ToLowerInvariant());

    /// <summary>
    /// Saves a DockerMod
    /// </summary>
    /// <param name="mod">The DockerMod to save</param>
    /// <returns>the saved DockerMod instance</returns>
    [HttpPost]
    public async Task<DockerMod> Save([FromBody] DockerMod mod)
    {
        ++mod.Revision;
        var result = await ServiceLoader.Load<DockerModService>().Save(mod, await GetAuditDetails());
        if (result.Failed(out string error))
            BadRequest(error);
        return result.Value;
    }
}