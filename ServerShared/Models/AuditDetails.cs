using FileFlows.Shared.Models;

namespace FileFlows.ServerShared.Models;

/// <summary>
/// Audit details
/// </summary>
public class AuditDetails
{
    /// <summary>
    /// Gets or sets the IP Address this was performed from
    /// </summary>
    public string IPAddress { get; init; } = null!;
    /// <summary>
    /// Gets or sets the UID of the user performing this action
    /// </summary>
    public Guid UserUid { get; init; }
    /// <summary>
    /// Gets or sets the name of the user performing this action
    /// </summary>
    public string UserName { get; init; } = null!;
    /// <summary>
    /// Gets or sets the operator type
    /// </summary>
    public OperatorType OperatorType { get; set; } = OperatorType.User;

    /// <summary>
    /// Gets the audit details for the server
    /// </summary>
    /// <returns>the server</returns>
    public static AuditDetails ForServer()
        => new ()
        {
            UserName = Globals.OperatorFileFlowsServerName,
            UserUid = Globals.OperatorFileFlowsServerUid,
            OperatorType = OperatorType.System,
            IPAddress = ""
        };
}