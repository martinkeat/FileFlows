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
    Task<Flow> GetByUidAsync(Guid uid);
    
    /// <summary>
    /// Gets the Failure Flow for a specific library
    /// This is the flow that is called if the flow fails 
    /// </summary>
    /// <param name="libraryUid">The UID of the library</param>
    /// <returns>An instance of the Failure Flow if found</returns>
    Task<Flow?> GetFailureFlow(Guid libraryUid);
}

/// <summary>
/// Service for communicating with FileFlows server for flows
/// </summary>
public class FlowService : Service, IFlowService
{

    /// <summary>
    /// Gets or sets the function used to load an instance of the IFlowService
    /// </summary>
    public static Func<IFlowService> Loader { get; set; }

    
    /// <summary>
    /// Loads an instance of the IFlowService
    /// </summary>
    /// <returns>an instance of the IFlowService</returns>
    public static IFlowService Load()
    {
        if (Loader == null)
            return new FlowService();
        return Loader.Invoke();
    }

    /// <summary>
    /// Gets a flow by its UID
    /// </summary>
    /// <param name="uid">The UID of the flow</param>
    /// <returns>An instance of the flow if found, otherwise null</returns>
    public async Task<Flow> GetByUidAsync(Guid uid)
    {
        try
        {
            var result = await HttpHelper.Get<Flow>($"{ServiceBaseUrl}/api/flow/" + uid.ToString());
            if (result.Success == false)
                throw new Exception("Failed to locate flow: " + result.Body);
            return result.Data;
        }
        catch (Exception ex)
        {
            Logger.Instance?.WLog("Failed to get flow: " + uid + " => " + ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Gets the Failure Flow for a specific library
    /// This is the flow that is called if the flow fails 
    /// </summary>
    /// <param name="libraryUid">The UID of the library</param>
    /// <returns>An instance of the Failure Flow if found</returns>
    public async Task<Flow?> GetFailureFlow(Guid libraryUid)
    {
        try
        {
            var result = await HttpHelper.Get<Flow>($"{ServiceBaseUrl}/api/flow/failure-flow/by-library/" + libraryUid);
            if (result.Success == false)
                throw new Exception("Failed to locate flow: " + result.Body);
            return result.Data;
        }
        catch (Exception ex)
        {
            Logger.Instance?.WLog("Failed to get failure flow by library: " + libraryUid + " => " + ex.Message);
            return null;
        }
    }
}
