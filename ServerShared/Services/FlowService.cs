namespace FileFlows.ServerShared.Services;

using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;

/// <summary>
/// Interface for communicating with FileFlows server for flows
/// </summary>
public interface IFlowService
{
    /// <summary>
    /// Gets a flow by its UID
    /// </summary>
    /// <param name="uid">The UID of the flow</param>
    /// <returns>An instance of the flow if found, otherwise null</returns>
    Task<Flow?> GetByUidAsync(Guid uid);
    
    /// <summary>
    /// Gets the Failure Flow for a specific library
    /// This is the flow that is called if the flow fails 
    /// </summary>
    /// <param name="libraryUid">The UID of the library</param>
    /// <returns>An instance of the Failure Flow if found</returns>
    Task<Flow?> GetFailureFlow(Guid libraryUid);
}