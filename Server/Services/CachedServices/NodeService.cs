using System.Runtime.InteropServices;
using FileFlows.Server.Helpers;
using FileFlows.Server.Controllers;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// An Service for communicating with the server for all Processing Node related actions
/// </summary>
public class NodeService : CachedService<ProcessingNode>, INodeService
{
    public override bool IncrementsConfiguration => false;

    static NodeService()
    {
        if (Globals.IsUnitTesting)
            return;
        _Data = DbHelper.Select<ProcessingNode>().Result.ToList();
        
        var internalNode = _Data.FirstOrDefault(x => x.Uid == Globals.InternalNodeUid);
        if (internalNode != null)
        {
            bool update = false;
            if (internalNode.Version != Globals.Version.ToString())
            {
                internalNode.Version = Globals.Version.ToString();
                update = true;
            }

            if (internalNode.OperatingSystem == OperatingSystemType.Unknown)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    internalNode.OperatingSystem = OperatingSystemType.Windows;
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    internalNode.OperatingSystem = OperatingSystemType.Mac;
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    internalNode.OperatingSystem = OperatingSystemType.Linux;
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
                    internalNode.OperatingSystem = OperatingSystemType.Linux;

                if (internalNode.OperatingSystem != OperatingSystemType.Unknown)
                    update = true;
            }

            if (update)
                DbHelper.Update(internalNode);
        }
    }


    /// <summary>
    /// A loader to load an instance of the Node service
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
    public Task ClearWorkersAsync(Guid nodeUid) => new WorkerController(null).Clear(nodeUid);

    /// <summary>
    /// Gets an instance of the internal processing node
    /// </summary>
    /// <returns>an instance of the internal processing node</returns>
    public async Task<ProcessingNode> GetServerNodeAsync()
    {
        var node = Data.FirstOrDefault(x => x.Uid == Globals.InternalNodeUid);
        if (node != null)
            return node;
        
        Logger.Instance.ILog("Adding Internal Processing Node");
        bool windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);                
        node = await DbHelper.Update(new ProcessingNode
        {
        
            Uid = Globals.InternalNodeUid,
            Name = Globals.InternalNodeName,
            Address = Globals.InternalNodeName,
            Schedule = new string('1', 672),
            Enabled = true,
            FlowRunners = 1,
            Version = Globals.Version.ToString(),
            AllLibraries = ProcessingLibraries.All,
#if (DEBUG)
            TempPath = windows ? @"d:\videos\temp" : Path.Combine(DirectoryHelper.BaseDirectory, "Temp"),
#else
            TempPath = DirectoryHelper.IsDocker ? "/temp" : Path.Combine(DirectoryHelper.BaseDirectory, "Temp"),
#endif
        });
        node.SignalrUrl = "flow";
        return node;
    } 

    /// <summary>
    /// Gets a tool path by name
    /// </summary>
    /// <param name="name">The name of the tool</param>
    /// <returns>a tool path</returns>
    public Task<string> GetVariableAsync(string name)
    {
        var result = new VariableController().GetByName(name);
        return Task.FromResult(result?.Value ?? string.Empty);
    }

    /// <summary>
    /// Gets a processing node by its physical address
    /// </summary>
    /// <param name="address">The address (hostname or IP address) of the node</param>
    /// <returns>An instance of the processing node</returns>
    public async Task<ProcessingNode> GetByAddressAsync(string address)
    {
        if (address == "INTERNAL_NODE")
            return await GetServerNodeAsync();
        address = address.Trim().ToLowerInvariant();
        var node = Data.FirstOrDefault(x => x.Address.ToLowerInvariant() == address);
        return node!;
    }

    /// <summary>
    /// Changes the temp path of a node
    /// </summary>
    /// <param name="address">the nodes address</param>
    /// <param name="path">the new temp path</param>
    public void ChangeTempPath(string address, string path)
    {
        if (string.IsNullOrEmpty(address) || string.IsNullOrEmpty(path))
            return;
        var node = Data.FirstOrDefault(x =>
            string.Equals(x.Address, address, StringComparison.InvariantCultureIgnoreCase));
        if (node == null)
            return;
        if (node.TempPath == path)
            return;
        node.TempPath = path;
        Update(node);
    }


    /// <summary>
    /// Updates an item
    /// </summary>
    /// <param name="item">the item being updated</param>
    /// <param name="dontIncrementConfigRevision">if this is a revision object, if the revision should be updated</param>
    public override void Update(ProcessingNode item, bool dontIncrementConfigRevision = false)
    {
        base.Update(item, dontIncrementConfigRevision: dontIncrementConfigRevision);
        var cached = GetByUid(item.Uid);
        if(item != cached)
            CopyInto(source: item, destination: cached);
    }

    private void CopyInto(ProcessingNode source, ProcessingNode destination)
    {
        Logger.Instance.WLog("Having to copy data into Processing Node for: " + source.Name + " , dest: " + destination.Name);
        destination.Name = source.Name?.EmptyAsNull() ?? destination.Name;
        destination.Address = source.Address?.EmptyAsNull() ?? destination.Address;
        destination.TempPath = source.TempPath?.EmptyAsNull() ?? destination.TempPath;
        destination.Enabled = source.Enabled;
        destination.FlowRunners = source.FlowRunners;
        destination.Priority = source.Priority;
        destination.PreExecuteScript = source.PreExecuteScript;
        destination.Schedule = source.Schedule?.EmptyAsNull()  ?? destination.Schedule;
        destination.Mappings = source.Mappings ?? new();
        destination.AllLibraries = source.AllLibraries;
        destination.Libraries = source.Libraries;
        destination.MaxFileSizeMb = source.MaxFileSizeMb;
        destination.DontChangeOwner = source.DontChangeOwner;
        destination.DontSetPermissions = source.DontSetPermissions;
        destination.Permissions = source.Permissions;
    }

    /// <summary>
    /// Updates the last seen date for a node
    /// </summary>
    /// <param name="node">the node being updated</param>
    /// <returns>a task to await</returns>
    public Task UpdateLastSeen(ProcessingNode node)
        => DbHelper.UpdateNodeLastSeen(node.Uid);
}