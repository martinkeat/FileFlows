using System.Runtime.InteropServices;
using FileFlows.Shared;

namespace FileFlows.ServerShared.Models;

/// <summary>
/// The registration model data used when registering a node with the FileFlows server
/// </summary>
public class RegisterModel
{
    /// <summary>
    /// Gets or sets the address (hostname or IP aaddres) of the node
    /// </summary>
    public string Address { get; set; } = null!;
    /// <summary>
    /// Gets or sets the temporary path used by this node
    /// </summary>
    public string TempPath { get; set; } = null!;
    // /// <summary>
    // /// Gets or sets the number of flow runners this node can run
    // /// </summary>
    // public int FlowRunners { get; set; }
    // /// <summary>
    // /// Gets or sets if this node is enabled
    // /// </summary>
    // public bool Enabled { get; set; }
    /// <summary>
    /// Gets or sets any mappings this node uses
    /// Mappings allow a file or folder local to the Server to be mapped to a location local to the Node
    /// </summary>
    public List<RegisterModelMapping>? Mappings { get; set; }
    /// <summary>
    /// Gets or sets the type of operating system this node is running on
    /// </summary>
    public OperatingSystemType OperatingSystem { get; set; }
    /// <summary>
    /// Gets or sets the architecture type
    /// </summary>
    public ArchitectureType Architecture { get; set; }
    /// <summary>
    /// Gets or sets the version of this node
    /// </summary>
    public string Version { get; set; } = null!;
}

/// <summary>
/// Mapping for server files and folders to files and folders local to a node 
/// </summary>
public class RegisterModelMapping
{
    /// <summary>
    /// Gets or sets the address on the server to map
    /// </summary>
    public string Server { get; set; } = null!;
    /// <summary>
    /// Gets or sets the local equivalent path on the node
    /// </summary>
    public string Local { get; set; } = null!;
}
