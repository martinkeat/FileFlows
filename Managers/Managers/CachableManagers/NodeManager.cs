using System.Runtime.InteropServices;

namespace FileFlows.Managers;

/// <summary>
/// An Service for communicating with the server for all Processing Node related actions
/// </summary>
public class NodeManager : CachedManager<ProcessingNode>
{
    public override bool IncrementsConfiguration => false;

    /// <summary>
    /// Updates the last seen date for a node
    /// </summary>
    /// <param name="node">the node being updated</param>
    /// <returns>a task to await</returns>
    public async Task UpdateLastSeen(ProcessingNode node)
    {
        string dt = DateTime.Now.ToString("o"); // same format as json
        await DatabaseAccessManager.Instance.DbObjectManager.SetDataValue(node.Uid, node.GetType().FullName,
            nameof(node.LastSeen), dt);
    }
}