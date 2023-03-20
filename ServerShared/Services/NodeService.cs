namespace FileFlows.ServerShared.Services;

using FileFlows.ServerShared.Models;
using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;
using System.Runtime.InteropServices;

/// <summary>
/// An interface for communicating with the server for all Processing Node related actions
/// </summary>
public interface INodeService
{
    /// <summary>
    /// Gets all processing nodes
    /// </summary>
    /// <returns>all processing nodes</returns>
    Task<List<ProcessingNode>> GetAllAsync();
    
    /// <summary>
    /// Gets a processing node by its UID
    /// </summary>
    /// <param name="uid">The UID of the node</param>
    /// <returns>An instance of the processing node</returns>
    Task<ProcessingNode> GetByUidAsync(Guid uid);
    
    /// <summary>
    /// Gets a processing node by its physical address
    /// </summary>
    /// <param name="address">The address (hostname or IP address) of the node</param>
    /// <returns>An instance of the processing node</returns>
    Task<ProcessingNode> GetByAddressAsync(string address);

    /// <summary>
    /// Gets an instance of the internal processing node
    /// </summary>
    /// <returns>an instance of the internal processing node</returns>
    Task<ProcessingNode> GetServerNodeAsync();

    /// <summary>
    /// Gets a variable value
    /// </summary>
    /// <param name="name">The name of the variable</param>
    /// <returns>a variable value</returns>
    Task<string> GetVariableAsync(string name);

    /// <summary>
    /// Clears all workers on the node.
    /// This is called when a node first starts up, if a node crashed when workers were running this will reset them
    /// </summary>
    /// <param name="nodeUid">The UID of the node</param>
    /// <returns>a completed task</returns>
    Task ClearWorkersAsync(Guid nodeUid);
}


/// <summary>
/// An Service for communicating with the server for all Processing Node related actions
/// </summary>
public class NodeService : Service, INodeService
{   
    /// <summary>
    /// Gets or sets a function used to load new instances of the service
    /// </summary>
    public static Func<INodeService> Loader { get; set; }

    /// <summary>
    /// Loads an instance of the node service
    /// </summary>
    /// <returns>an instance of the node service</returns>
    public static INodeService Load()
    {
        if (Loader == null)
            return new NodeService();
        return Loader.Invoke();
    }

    /// <summary>
    /// Clears all workers on the node.
    /// This is called when a node first starts up, if a node crashed when workers were running this will reset them
    /// </summary>
    /// <param name="nodeUid">The UID of the node</param>
    /// <returns>a completed task</returns>
    public async Task ClearWorkersAsync(Guid nodeUid)
    {
        try
        {
            await HttpHelper.Post(ServiceBaseUrl + "/api/worker/clear/" + Uri.EscapeDataString(nodeUid.ToString()));
        }
        catch (Exception)
        {
            return;
        }
    }

    /// <summary>
    /// Gets all processing nodes
    /// </summary>
    /// <returns>all processing nodes</returns>
    public async Task<List<ProcessingNode>> GetAllAsync()
    {
        try
        {
            var result = await HttpHelper.Get<List<ProcessingNode>>(ServiceBaseUrl + "/api/node");
            return result.Data;
        }
        catch (Exception ex)
        {
            Logger.Instance?.ELog("Failed to locate server node: " + ex.Message);
            return null;
        }
    }
    
    /// <summary>
    /// Gets a processing node by its UID
    /// </summary>
    /// <param name="uid">The UID of the node</param>
    /// <returns>An instance of the processing node</returns>
    public async Task<ProcessingNode> GetByUidAsync(Guid uid)
    {
        try
        {
            var result = await HttpHelper.Get<ProcessingNode>(ServiceBaseUrl + "/api/node/" + uid);
            return result.Data;
        }
        catch (Exception ex)
        {
            Logger.Instance?.ELog("Failed to locate server node: " + ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Gets an instance of the internal processing node
    /// </summary>
    /// <returns>an instance of the internal processing node</returns>
    public async Task<ProcessingNode> GetServerNodeAsync()
    {
        try
        {
            var result = await HttpHelper.Get<ProcessingNode>(ServiceBaseUrl + "/api/node/by-address/INTERNAL_NODE");
            return result.Data;
        }
        catch (Exception ex)
        {
            Logger.Instance?.ELog("Failed to locate server node: " + ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Gets a variable value
    /// </summary>
    /// <param name="name">The name of the variable</param>
    /// <returns>a variable value</returns>
    public async Task<string> GetVariableAsync(string name)
    {
        try
        {
            var result = await HttpHelper.Get<Variable>(ServiceBaseUrl + "/api/variable/name/" + Uri.EscapeDataString(name));
            return result.Data.Value;
        }
        catch (Exception ex)
        {
            Logger.Instance?.ELog("Failed to locate variable: " + name + " => " + ex.Message);
            return string.Empty;
        }
    }

    /// <summary>
    /// Gets a processing node by its physical address
    /// </summary>
    /// <param name="address">The address (hostname or IP address) of the node</param>
    /// <returns>An instance of the processing node</returns>
    public async Task<ProcessingNode> GetByAddressAsync(string address)
    {
        try
        {
            var result = await HttpHelper.Get<ProcessingNode>(ServiceBaseUrl + "/api/node/by-address/" + Uri.EscapeDataString(address) + "?version=" + Globals.Version);
            if (result.Success == false)
                throw new Exception("Failed to get node: " + result.Body);                
            if(result.Data == null)
            {
                // node does not exist
                Logger.Instance.ILog("Node does not exist: " + address);
                return null;
            }
            result.Data.SignalrUrl = ServiceBaseUrl + "/" + result.Data.SignalrUrl;
            return result.Data;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to get node by address: " + ex.Message + Environment.NewLine + ex.StackTrace);
            throw;
        }
    }
    
    /// <summary>
    /// Registers a node with FileFlows
    /// </summary>
    /// <param name="serverUrl">The URL of the FileFlows Server</param>
    /// <param name="address">The address (Hostname or IP Address) of the node</param>
    /// <param name="tempPath">The temporary path location of the node</param>
    /// <param name="runners">The amount of flow runners this node can execute</param>
    /// <param name="enabled">If this node is enabled or not</param>
    /// <param name="mappings">Any mappings for the node</param>
    /// <returns>An instance of the registered node</returns>
    /// <exception cref="Exception">If fails to register, an exception will be thrown</exception>
    public async Task<ProcessingNode> Register(string serverUrl, string address, string tempPath, int runners, bool enabled, List<RegisterModelMapping> mappings)
    {
        if(serverUrl.EndsWith("/"))
            serverUrl = serverUrl.Substring(0, serverUrl.Length - 1);

        bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        bool isMacOs = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
            isLinux = true;

        var result = await HttpHelper.Post<ProcessingNode>(serverUrl + "/api/node/register", new RegisterModel
        {
            Address = address,
            TempPath = tempPath,
            FlowRunners = runners,
            Enabled = enabled,
            Mappings = mappings,
            Version = Globals.Version.ToString(),
            OperatingSystem = isWindows ? Shared.OperatingSystemType.Windows : 
                 isLinux ? Shared.OperatingSystemType.Linux :       
                 isMacOs ? Shared.OperatingSystemType.Mac :
                 Shared.OperatingSystemType.Unknown
        }, timeoutSeconds: 15);

        if (result.Success == false)
            throw new Exception("Failed to register node: " + result.Body);

        return result.Data;
    }
}
