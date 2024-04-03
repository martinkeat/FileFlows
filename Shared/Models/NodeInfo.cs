namespace FileFlows.Shared.Models;

/// <summary>
/// Very basic and minimum node information
/// To be used on other than the nodes pages
/// </summary>
public class NodeInfo
{
    /// <summary>
    /// Gets or sets the UID of the node
    /// </summary>
    public Guid Uid { get; set; }
    /// <summary>
    /// Gets or sets the name of the node
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Gets or sets the operating system of the node
    /// </summary>
    public OperatingSystemType OperatingSystem { get; set; }
}