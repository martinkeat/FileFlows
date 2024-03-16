using System.Runtime.InteropServices;
using FileFlows.Plugin;
using FileFlows.ServerShared;
using FileFlows.ServerShared.Helpers;

namespace FileFlows.Managers;

/// <summary>
/// An Manager for communicating with the server for all Processing Node related actions
/// </summary>
public class NodeManager : CachedManager<ProcessingNode>
{
    public override bool IncrementsConfiguration => false;

    /// <summary>
    /// Updates the last seen date for a node
    /// </summary>
    /// <param name="nodeUid">the UID node being updated</param>
    /// <returns>a task to await</returns>
    public async Task UpdateLastSeen(Guid nodeUid)
    {
        var lastSeen = DateTime.UtcNow;
        if (UseCache)
        {
            var node = _Data.FirstOrDefault(x => x.Uid == nodeUid);
            if(node != null)
                node.LastSeen = lastSeen;
        }

        string dt = lastSeen.ToString("o"); // same format as json
        await DatabaseAccessManager.Instance.ObjectManager.SetDataValue(nodeUid, typeof(ProcessingNode).FullName,
            nameof(ProcessingNode.LastSeen), dt);
    }
    
    /// <summary>
    /// Gets a processing node by its physical address
    /// </summary>
    /// <param name="address">The address (hostname or IP address) of the node</param>
    /// <returns>An instance of the processing node</returns>
    public async Task<ProcessingNode?> GetByAddress(string address)
    {
        if (address == "INTERNAL_NODE")
            return await GetByUid(Globals.InternalNodeUid);
        address = address.Trim().ToLowerInvariant();
        var all = await GetAll();
        return all.FirstOrDefault(x => x.Address.ToLowerInvariant() == address);
    }


    /// <summary>
    /// Updates the node version
    /// </summary>
    /// <param name="nodeUid">the UID of the node being updated</param>
    /// <param name="nodeVersion">the new version number</param>
    /// <returns>a task to await</returns>
    public Task UpdateVersion(Guid nodeUid, string nodeVersion)
        => DatabaseAccessManager.Instance.ObjectManager.SetDataValue(nodeUid, typeof(ProcessingNode).FullName,
            nameof(ProcessingNode.Version), nodeVersion);

    /// <summary>
    /// Ensures the internal processing node exists
    /// </summary>
    /// <returns>true if successful</returns>
    public async Task<Result<bool>> EnsureInternalNodeExists()
    {
        var manager  = DatabaseAccessManager.Instance.FileFlowsObjectManager;
        var node = await manager.Single<ProcessingNode>(Globals.InternalNodeUid);
        if (node.Failed(out string error))
            return Result<bool>.Fail(error);
        if (node.Value == null)
        {

            string tempPath;
            if (DirectoryHelper.IsDocker)
                tempPath = "/temp";
            else
                tempPath = Path.Combine(DirectoryHelper.BaseDirectory, "Temp");

            if (Directory.Exists(tempPath) == false)
                Directory.CreateDirectory(tempPath);

            node = new ProcessingNode
            {
                Uid = Globals.InternalNodeUid,
                Name = Globals.InternalNodeName,
                Address = Globals.InternalNodeName,
                AllLibraries = ProcessingLibraries.All,
                OperatingSystem = Globals.IsDocker ? OperatingSystemType.Docker :
                    Globals.IsWindows ? OperatingSystemType.Windows :
                    Globals.IsLinux ? OperatingSystemType.Linux :
                    Globals.IsMac ? OperatingSystemType.Mac :
                    OperatingSystemType.Unknown,
                Architecture = RuntimeInformation.ProcessArchitecture == Architecture.Arm ? ArchitectureType.Arm32 :
                    RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? ArchitectureType.Arm64 :
                    RuntimeInformation.ProcessArchitecture == Architecture.Arm ? ArchitectureType.Arm64 :
                    RuntimeInformation.ProcessArchitecture == Architecture.X64 ? ArchitectureType.x64 :
                    RuntimeInformation.ProcessArchitecture == Architecture.X86 ? ArchitectureType.x86 :
                    ArchitectureType.Unknown,
                Schedule = new string('1', 672),
                Enabled = true,
                FlowRunners = 1,
                TempPath = tempPath,
            };
        }
        else
        {
            node.Value.Version = Globals.Version;
        }

        await manager.AddOrUpdateObject((FileFlowObject)node.Value!);

        return true;
    }
}