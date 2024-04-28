using FileFlows.Managers;
using FileFlows.Plugin;
using FileFlows.ServerShared.Models;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// Service for DockerMods
/// </summary>
public class DockerModService
{
    /// <summary>
    /// Gets a DockerMod by its UID
    /// </summary>
    /// <param name="uid">the UID of the DockerMod</param>
    /// <returns>the DockerMod if found, otherwise null</returns>
    public Task<DockerMod?> GetByUid(Guid uid)
        => new DockerModManager().GetByUid(uid);

    /// <summary>
    /// Gets all DockerMods in the system
    /// </summary>
    /// <returns>all DockerMods in the system</returns>
    public Task<List<DockerMod>> GetAll()
        => new DockerModManager().GetAll();

    
    /// <summary>
    /// Saves a DockerMod
    /// </summary>
    /// <param name="mod">The DockerMod to save</param>
    /// <param name="auditDetails">The audit details</param>
    /// <returns>the saved DockerMod instance</returns>
    public Task<Result<DockerMod>> Save(DockerMod mod, AuditDetails? auditDetails)
        => new DockerModManager().Update(mod, auditDetails);

}