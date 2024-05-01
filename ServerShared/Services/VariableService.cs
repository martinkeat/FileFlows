using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.ServerShared.Services;

/// <summary>
/// Interface for communicating with FileFlows server for variables
/// </summary>
public interface IVariableService
{
    /// <summary>
    /// Gets all variables in the system
    /// </summary>
    /// <returns>all variables in the system</returns>
    Task<List<Variable>?> GetAllAsync();
}
