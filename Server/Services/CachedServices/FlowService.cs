namespace FileFlows.Server.Services;

using FileFlows.Server.Controllers;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Models;
using System;
using System.Threading.Tasks;

/// <summary>
/// Service for communicating with FileFlows server for flows
/// </summary>
public class FlowService : CachedService<Flow>, IFlowService
{
    static FlowService()
        => new FlowService().Refresh();
    
    /// <summary>
    /// Gets the Failure Flow for a specific library
    /// This is the flow that is called if the flow fails 
    /// </summary>
    /// <param name="libraryUid">The UID of the library</param>
    /// <returns>An instance of the Failure Flow if found</returns>
    public Task<Flow?> GetFailureFlow(Guid libraryUid)
        => Task.FromResult(Data.FirstOrDefault(x => x.Type == FlowType.Failure && x.Enabled && x.Default));

}
