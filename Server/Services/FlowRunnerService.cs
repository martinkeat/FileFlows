namespace FileFlows.Server.Services;

using FileFlows.Server.Controllers;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Models;
using System;
using System.Threading.Tasks;

/// <summary>
/// A flow runner which is responsible for executing a flow and processing files
/// </summary>
public class FlowRunnerService : IFlowRunnerService
{
    
    /// <summary>
    /// Called when the flow execution has completed
    /// </summary>
    /// <param name="info">The information about the flow execution</param>
    /// <returns>a completed task</returns>
    public Task<FlowExecutorInfo> Start(FlowExecutorInfo info) =>
        Task.FromResult(new WorkerController(null).StartWork(info));
    
    
    /// <summary>
    /// Called when a flow execution starts
    /// </summary>
    /// <param name="info">The information about the flow execution</param>
    /// <param name="log">The full log for the file</param>
    /// <returns>The updated information</returns>
    public Task Complete(FlowExecutorInfo info, string log)
    {
        new WorkerController(null).FinishWork(new() { Info = info, Log = log });
        return Task.CompletedTask;
    }


    /// <summary>
    /// Called to update the status of the flow execution on the server
    /// </summary>
    /// <param name="info">The information about the flow execution</param>
    /// <returns>a completed task</returns>
    public async Task Update(FlowExecutorInfo info)
    {
        await new WorkerController(null).UpdateWork(info);
    }
}