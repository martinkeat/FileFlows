using FileFlows.Managers;
using FileFlows.Plugin;
using FileFlows.ServerShared.Models;

namespace FileFlows.Server.Services;

using FileFlows.Server.Controllers;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Models;
using System;
using System.Threading.Tasks;

/// <summary>
/// Service for communicating with FileFlows server for flows
/// </summary>
public class FlowService : IFlowService
{
    /// <inheritdoc />
    public Task<Flow?> GetByUidAsync(Guid uid)
        => new FlowManager().GetByUid(uid);

    /// <inheritdoc />
    public Task<Flow?> GetFailureFlow(Guid libraryUid)
        => new FlowManager().GetFailureFlow(libraryUid);
    
    /// <summary>
    /// Get all the flows in the system
    /// </summary>
    /// <returns></returns>
    public Task<List<Flow>> GetAllAsync()
        => new FlowManager().GetAll();

    /// <summary>
    /// Gets if a UID is in use
    /// </summary>
    /// <param name="uid">the UID to check</param>
    /// <returns>true if in use</returns>
    public Task<bool> UidInUse(Guid uid)
        => new FlowManager().UidInUse(uid);

    /// <summary>
    /// Gets a new unique name
    /// </summary>
    /// <param name="name">the name to to base it off</param>
    /// <returns>a new unique name</returns>
    public Task<string> GetNewUniqueName(string name)
        => new FlowManager().GetNewUniqueName(name);

    /// <summary>
    /// Updates a flow
    /// </summary>
    /// <param name="flow">the flow to update</param>
    /// <param name="auditDetails">The audit details</param>
    /// <returns>the result of the update</returns>
    public Task<Result<Flow>> Update(Flow flow, AuditDetails auditDetails)
        => new FlowManager().Update(flow, auditDetails);

    /// <summary>
    /// Deletes the given flows
    /// </summary>
    /// <param name="uids">the UID of the flows to delete</param>
    /// <param name="auditDetails">the audit details</param>
    /// <returns>a task to await</returns>
    public Task Delete(Guid[] uids, AuditDetails auditDetails)
        => new FlowManager().Delete(uids, auditDetails);

    
    /// <summary>
    /// Gets if there are any flows in the system
    /// </summary>
    /// <returns>true if there are any flows</returns>
    public Task<bool> HasAny()
        => new FlowManager().HasAny();

    /// <summary>
    /// Checks to see if a name is in use
    /// </summary>
    /// <param name="uid">the Uid of the item</param>
    /// <param name="name">the name of the item</param>
    /// <returns>true if name is in use</returns>
    public Task<bool> NameInUse(Guid uid, string name)
        => new FlowManager().NameInUse(uid, name);
}
