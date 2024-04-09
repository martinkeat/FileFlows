namespace FileFlows.ServerShared.Models;

/// <summary>
/// Audit details
/// </summary>
public class AuditDetails
{
    /// <summary>
    /// Gets or sets the IP Address this was performed from
    /// </summary>
    public string IPAddress { get; init; }
    /// <summary>
    /// Gets or sets the UID of the user performing this action
    /// </summary>
    public Guid UserUid { get; init; }
    /// <summary>
    /// Gets or sets the name of the user performing this action
    /// </summary>
    public string UserName { get; init; }

    /// <summary>
    /// Gets the audit details for the server
    /// </summary>
    /// <returns>the server</returns>
    public static AuditDetails ForServer()
        => new AuditDetails()
        {
            UserName = Globals.OperatorFileFlowsServerName,
            UserUid = Globals.OperatorFileFlowsServerUid,
            IPAddress = ""
        };
}