namespace FileFlows.Shared.Models;

/// <summary>
/// An access control list entry
/// </summary>
public class AccessControlEntry : FileFlowObject
{
    /// <summary>
    /// Gets or sets an optional description
    /// </summary>
    public string Description { get; set; }
    /// <summary>
    /// Gets or sets the start range
    /// </summary>
    public string Start { get; set; }

    /// <summary>
    /// Gets or sets the end range
    /// </summary>
    public string End { get; set; }

    /// <summary>
    /// Gets or sets if this range is allowed
    /// </summary>
    public bool Allow { get; set; }
    
    /// <summary>
    /// Gets or sets the order of the access control
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Gets or sets the type
    /// </summary>
    public AccessControlType Type { get; set; }
}

/// <summary>
/// Type of access control entry
/// </summary>
public enum AccessControlType
{
    /// <summary>
    /// Access control for the web console
    /// </summary>
    Console = 0,
    /// <summary>
    /// Access control for remote services
    /// </summary>
    RemoteServices = 1
}