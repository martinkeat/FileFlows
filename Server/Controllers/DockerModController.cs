using System.Text;
using FileFlows.Server.Authentication;
using FileFlows.Server.Services;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using YamlDotNet.Serialization;

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

    /// <summary>
    /// Exports a DockerMod
    /// </summary>
    /// <param name="uid">the UID of the DockerMod</param>
    /// <returns>The file download result</returns>
    [HttpGet("export/{uid}")]
    public async Task<IActionResult> Export([FromRoute] Guid uid)
    {
        var mod = await ServiceLoader.Load<DockerModService>().Export(uid);
        if (mod.IsFailed)
            return NotFound();
        
        var data = Encoding.UTF8.GetBytes(mod.Value.Content);
        return File(data, "application/octet-stream", mod.Value.Name + ".sh");
    }
}