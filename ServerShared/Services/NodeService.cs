using FileFlows.ServerShared.Models;
using FileFlows.Shared.Models;

namespace FileFlows.ServerShared.Services;

/// <summary>
/// An interface for communicating with the server for all Processing Node related actions
/// </summary>
public interface INodeService
{
    /// <summary>
    /// Gets a processing node by its physical address
    /// </summary>
    /// <param name="address">The address (hostname or IP address) of the node</param>
    /// <returns>An instance of the processing node</returns>
    Task<ProcessingNode?> GetByAddressAsync(string address);
    
    /// <summary>
    /// Gets a processing node by UID
    /// </summary>
    /// <param name="uid">The UID of the node</param>
    /// <returns>An instance of the processing node</returns>
    Task<ProcessingNode?> GetByUidAsync(Guid uid);

    /// <summary>
    /// Gets an instance of the internal processing node
    /// </summary>
    /// <returns>an instance of the internal processing node</returns>
    Task<ProcessingNode?> GetServerNodeAsync();
    
    /// <summary>
    /// Clears all workers on the node.
    /// This is called when a node first starts up, if a node crashed when workers were running this will reset them
    /// </summary>
    /// <param name="nodeUid">The UID of the node</param>
    /// <returns>a completed task</returns>
    Task ClearWorkersAsync(Guid nodeUid);

    /// <summary>
    /// Registers a node with FileFlows
    /// </summary>
    /// <param name="serverUrl">The URL of the FileFlows Server</param>
    /// <param name="address">The address (Hostname or IP Address) of the node</param>
    /// <param name="tempPath">The temporary path location of the node</param>
    /// <param name="mappings">Any mappings for the node</param>
    /// <returns>An instance of the registered node</returns>
    /// <exception cref="Exception">If fails to register, an exception will be thrown</exception>
    Task<ProcessingNode?> Register(string serverUrl, string address, string tempPath,
        List<RegisterModelMapping> mappings);

    /// <summary>
    /// Gets the version the node can update to
    /// </summary>
    /// <returns>the version the node can update to</returns>
    Task<Version> GetNodeUpdateVersion();

    /// <summary>
    /// Gets if the processing nodes should auto update
    /// </summary>
    /// <returns>true if they should auto update</returns>
    Task<bool> AutoUpdateNodes();

    /// <summary>
    /// Get a 
    /// </summary>
    /// <returns></returns>
    Task<byte[]> GetNodeUpdater();

    /// <summary>
    /// Records the node system statistics to the server
    /// </summary>
    /// <param name="args">the node system statistics</param>
    /// <returns>the task to await</returns>
    Task RecordNodeSystemStatistics(NodeSystemStatistics args);

    /// <summary>
    /// Pauses the system for the given time
    /// </summary>
    /// <param name="minutes">the minutes to pause the system for</param>
    /// <returns>the task to await</returns>
    Task Pause(int minutes);

    /// <summary>
    /// Gets if the system is running and not paused
    /// </summary>
    /// <returns>true if running and not paused, otherwise false</returns>
    Task<bool> GetSystemIsRunning();
}

