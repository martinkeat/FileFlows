using System.Runtime.InteropServices;
using FileFlows.ServerShared;

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
        string dt = DateTime.UtcNow.ToString("o"); // same format as json
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
}