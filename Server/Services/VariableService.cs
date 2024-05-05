using FileFlows.Managers;
using FileFlows.Plugin;
using FileFlows.ServerShared.Models;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// Service for communicating with FileFlows server for variables
/// </summary>
public class VariableService : IVariableService
{
    /// <inheritdoc />
    public async Task<List<Variable>?> GetAllAsync()
        => await new VariableManager().GetAll();

    /// <summary>
    /// Deletes the given variables
    /// </summary>
    /// <param name="uids">the UID of the variables to delete</param>
    /// <param name="auditDetails">the audit details</param>
    /// <returns>a task to await</returns>
    public Task Delete(Guid[] uids, AuditDetails auditDetails)
        => new VariableManager().Delete(uids, auditDetails);

    /// <summary>
    /// Gets a variable by its UID
    /// </summary>
    /// <param name="uid">the UID of the variable</param>
    /// <returns>the variable</returns>
    public Task<Variable?> GetByUidAsync(Guid uid)
        => new VariableManager().GetByUid(uid);


    /// <summary>
    /// Gets a variable by it's name
    /// </summary>
    /// <param name="name">the variable of the item</param>
    /// <param name="ignoreCase">if case should be ignored</param>
    /// <returns>the variable</returns>
    public Task<Variable?> GetByName(string name, bool ignoreCase = false)
        => new VariableManager().GetByName(name, ignoreCase);

    /// <summary>
    /// Updates a variable
    /// </summary>
    /// <param name="variable">the variable to update</param>
    /// <param name="auditDetails">The audit details</param>
    /// <returns>the result</returns>
    public Task<Result<Variable>> Update(Variable variable, AuditDetails? auditDetails)
        => new VariableManager().Update(variable, auditDetails);
}