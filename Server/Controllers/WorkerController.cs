using FileFlows.Server.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using FileFlows.Shared.Models;
using FileFlows.Server.Helpers;
using FileFlows.Server.Hubs;
using FileFlows.Server.Services;

namespace FileFlows.Server.Controllers;

/// <summary>
/// This controller will be responsible for knowing about the workers and the nodes
/// When a worker starts, this needs to be informed, when its finished, it needs to be told too
/// This needs to be able to kill a worker running on any node
/// </summary>
[Route("/api/worker")]
[FileFlowsAuthorize]
public class WorkerController : Controller
{
    /// <summary>
    /// the flow hub context
    /// </summary>
    private IHubContext<FlowHub> Context;
    
    /// <summary>
    /// Initialises a new worker controller
    /// </summary>
    /// <param name="context">the flow hub context</param>
    public WorkerController(IHubContext<FlowHub> context)
    {
        this.Context = context;
    }

    // /// <summary>
    // /// Start work, tells the server work has started on a flow runner
    // /// </summary>
    // /// <param name="info">The info about the work starting</param>
    // /// <returns>the updated info</returns>
    // [HttpPost("work/start")]
    // public Task<FlowExecutorInfo> StartWork([FromBody] FlowExecutorInfo info)
    //     => ServiceLoader.Load<FlowRunnerService>().Start(info);
    //
    // /// <summary>
    // /// Finish work, tells the server work has finished on a flow runner
    // /// </summary>
    // /// <param name="info">the flow executor info</param>
    // [HttpPost("work/finish")]
    // public Task FinishWork([FromBody] FlowExecutorInfo info)
    //     => ServiceLoader.Load<FlowRunnerService>().Finish(info);
    
    // /// <summary>
    // /// Update work, tells the server about updated work on a flow runner
    // /// </summary>
    // /// <param name="info">The updated work information</param>
    // [HttpPost("work/update")]
    // public Task UpdateWork([FromBody] FlowExecutorInfo info)
    //     => ServiceLoader.Load<FlowRunnerService>().Update(info);
    
    // /// <summary>
    // /// Clear all workers from a node.  Intended for clean up in case a node restarts.  
    // /// This is called when a node first starts.
    // /// </summary>
    // /// <param name="nodeUid">The UID of the processing node</param>
    // /// <returns>an awaited task</returns>
    // [HttpPost("clear/{nodeUid}")]
    // public Task Clear([FromRoute] Guid nodeUid)
    //     => ServiceLoader.Load<FlowRunnerService>().Clear(nodeUid);

    /// <summary>
    /// Get all running flow executors
    /// </summary>
    /// <returns>A list of all running flow executors</returns>
    [HttpGet]
    public async Task<IEnumerable<FlowExecutorInfo>> GetAll()
    {
        if (HttpContext?.Response != null)
        {
            var settings = await ServiceLoader.Load<ISettingsService>().Get();
            if (settings.IsPaused)
            {
                HttpContext.Response.Headers.TryAdd("x-paused", "1");
            }
        }

        // we don't want to return the logs here, too big
        var liveExecutors = FlowRunnerService.Executors.Values.Where(x => x != null).ToList();
        var results = liveExecutors.Select(x => new FlowExecutorInfo
        {
            // have to create a new object, otherwise if we change the log we change the log on the shared object
            LibraryFile = x.LibraryFile,
            CurrentPart = x.CurrentPart,
            CurrentPartName = x.CurrentPartName,
            CurrentPartPercent = x.CurrentPartPercent,
            Library = x.Library,
            NodeUid = x.NodeUid,
            NodeName = x.NodeName,
            RelativeFile = x.RelativeFile,
            StartedAt = x.StartedAt,
            TotalParts = x.TotalParts,
            Uid = x.Uid,
            WorkingFile = x.WorkingFile
        }).ToList();
        return results;
    }

    /// <summary>
    /// Gets the log of a library file
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    /// <param name="lineCount">The number of lines to fetch, 0 to fetch them all</param>
    /// <returns>The log of a library file</returns>
    [HttpGet("{uid}/log")]
    public string Log([FromRoute] Guid uid, [FromQuery] int lineCount = 0)
        => LibraryFileLogHelper.GetLog(uid);

    /// <summary>
    /// Abort work by library file
    /// </summary>
    /// <param name="uid">The UID of the library file to abort</param>
    /// <returns>An awaited task</returns>
    [HttpDelete("by-file/{uid}")]
    public async Task AbortByFile(Guid uid)
    {
        // get the runner that processing this file
        var service = ServiceLoader.Load<FlowRunnerService>();
        var runner = await service.FindRunner(uid);
        if (runner.IsFailed)
        {
            Logger.Instance.WLog("Failed to find runner to cancel file: " + uid);
            // fall back abort, shouldn't happen, but this is a way to abort
            await service.AbortByFile(uid);
        }
        else
        {
            // call the same abort as from the dashboard
            await Abort(runner.Value, uid);
        }
    }

    /// <summary>
    /// Abort work 
    /// </summary>
    /// <param name="uid">The UID of the executor</param>
    /// <param name="libraryFileUid">the UID of the library file</param>
    /// <returns>an awaited task</returns>
    [HttpDelete("{uid}")]
    public async Task Abort([FromRoute] Guid uid, [FromQuery] Guid libraryFileUid)
    {
        await ServiceLoader.Load<FlowRunnerService>().Abort(uid, libraryFileUid);
        try
        {
            await this.Context?.Clients?.All?.SendAsync("AbortFlow", uid);
        }
        catch (Exception ex)
        {
            Logger.Instance?.WLog("Failed sending AbortFlow " + uid + " => " + ex.Message);
        }
        try
        {
            await this.Context?.Clients?.All?.SendAsync("AbortFlow", libraryFileUid);
        }
        catch (Exception ex)
        {
            Logger.Instance?.WLog("Failed sending AbortFlow to library file UID " + uid + " => " + ex.Message);
        }
    }

    /// <summary>
    /// Receives a hello from the flow runner, indicating its still alive and executing
    /// </summary>
    /// <param name="runnerUid">the UID of the flow runner</param>
    /// <param name="info">the flow execution info</param>
    internal Task<bool> Hello(Guid runnerUid, FlowExecutorInfo info)
        => ServiceLoader.Load<FlowRunnerService>().Hello(runnerUid, info);
}