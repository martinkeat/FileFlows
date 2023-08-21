namespace FileFlows.ServerShared.Services;

using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;

/// <summary>
/// Interface for a Flow Runner, which is responsible for executing a flow and processing files
/// </summary>
public interface IFlowRunnerService
{
    /// <summary>
    /// Called when a flow execution starts
    /// </summary>
    /// <param name="info">The information about the flow execution</param>
    /// <returns>The updated information</returns>
    Task<FlowExecutorInfo> Start(FlowExecutorInfo info);
    /// <summary>
    /// Called when the flow execution has completed
    /// </summary>
    /// <param name="info">The information about the flow execution</param>
    /// <param name="log">the full log for the file</param>
    /// <returns>a completed task</returns>
    Task Complete(FlowExecutorInfo info, string log);
    /// <summary>
    /// Called to update the status of the flow execution on the server
    /// </summary>
    /// <param name="info">The information about the flow execution</param>
    /// <returns>a completed task</returns>
    Task Update(FlowExecutorInfo info);
}

/// <summary>
/// A flow runner which is responsible for executing a flow and processing files
/// </summary>
public class FlowRunnerService : Service, IFlowRunnerService
{

    /// <summary>
    /// Gets or sets the function that will load the flow runner when Load is called
    /// This is used in unit testing to mock this runner
    /// </summary>
    public static Func<IFlowRunnerService> Loader { get; set; }

    /// <summary>
    /// Loads a Flow Runner instance and returns it
    /// </summary>
    /// <returns>a flow runner instance</returns>
    public static IFlowRunnerService Load()
    {
        if (Loader == null)
            return new FlowRunnerService();
        return Loader.Invoke();
    }

    /// <summary>
    /// Called when a flow execution starts
    /// </summary>
    /// <param name="info">The information about the flow execution</param>
    /// <param name="log">the full log for the file</param>
    /// <returns>The updated information</returns>
    public async Task Complete(FlowExecutorInfo info, string log)
    {
        try
        {
            var result = await HttpHelper.Post($"{ServiceBaseUrl}/api/worker/work/finish", new
            {
                Info = info,
                Log = log
            });
            if (result.Success == false)
                throw new Exception("Failed to finish work: " + result.Body);
        }
        catch (Exception ex)
        {
            Logger.Instance?.WLog("Failed to finish work: " + ex.Message);
        }
    }

    /// <summary>
    /// Called when the flow execution has completed
    /// </summary>
    /// <param name="info">The information about the flow execution</param>
    /// <returns>a completed task</returns>
    public async Task<FlowExecutorInfo> Start(FlowExecutorInfo info)
    {
        try
        {
            var result = await HttpHelper.Post<FlowExecutorInfo>($"{ServiceBaseUrl}/api/worker/work/start", info);
            if (result.Success == false)
                throw new Exception("Failed to start work: " + result.Body);
            return result.Data;
        }
        catch (Exception ex)
        {
            Logger.Instance?.WLog("Failed to start work: " + ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Called to update the status of the flow execution on the server
    /// </summary>
    /// <param name="info">The information about the flow execution</param>
    /// <returns>a completed task</returns>
    public async Task Update(FlowExecutorInfo info)
    {
        try
        {
            var result = await HttpHelper.Post($"{ServiceBaseUrl}/api/worker/work/update", info, noLog: true);
            if (result.Success == false)
                throw new Exception("Failed to update work: " + result.Body);
        }
        catch (Exception ex)
        {
            Logger.Instance?.WLog("Failed to update work: " + ex.Message);
        }
    }
}
