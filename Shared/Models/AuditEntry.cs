using NPoco;

namespace FileFlows.Shared.Models;

/// <summary>
/// Audit Entry
/// </summary>
public class AuditEntry
{
    /// <summary>
    /// Gets or sets the operators UID
    /// </summary>
    public Guid OperatorUid { get; set; }
    /// <summary>
    /// Gets or sets the operators name
    /// </summary>
    public string OperatorName { get; set; }
    /// <summary>
    /// Gets or sets the operator type
    /// </summary>
    public OperatorType OperatorType{ get; set; }
    /// <summary>
    /// Gets or sets the IP Address making this audit
    /// </summary>
    public string IPAddress { get; set; }
    /// <summary>
    /// Gets or sets the the date this was audited in UTC
    /// </summary>
    public DateTime LogDate { get; set; }
    /// <summary>
    /// Gets or sets the audit action
    /// </summary>
    public AuditAction Action { get; set; }
    /// <summary>
    /// Gets or sets the type of object being audited
    /// </summary>
    public string ObjectType { get; set; }
    /// <summary>
    /// Gets or sets the objects UID
    /// </summary>
    public Guid ObjectUid { get; set; }
    /// <summary>
    /// Gets or sets the optional revision UID of the change
    /// </summary>
    public Guid? RevisionUid { get; set; }
    /// <summary>
    /// Gets or sets the summary parameters
    /// </summary>
    [SerializedColumn]
    public Dictionary<string, object> Parameters { get; set; }
    /// <summary>
    /// Gets or sets the changes made to the object
    /// </summary>
    [SerializedColumn]
    public Dictionary<string, object> Changes { get; set; }
    
    /// <summary>
    /// Gets or sets the summary of the audit
    /// </summary>
    [DbIgnore]
    [Ignore]
    public string Summary { get; set; }
}

/// <summary>
/// Operator type
/// </summary>
public enum OperatorType
{
    /// <summary>
    /// User operator
    /// </summary>
    User = 1,
    /// <summary>
    /// System operator
    /// </summary>
    System = 2
}

/// <summary>
/// Audit actions
/// </summary>
public enum AuditAction
{
    /// <summary>
    /// Item was added
    /// </summary>
    Added = 1,
    /// <summary>
    /// Item was updated
    /// </summary>
    Updated = 2,
    /// <summary>
    /// Item was deleted
    /// </summary>
    Deleted = 3,
    
    /// <summary>
    /// User logged in
    /// </summary>
    Login = 11,
    /// <summary>
    /// User logged in failed
    /// </summary>
    LoginFailed = 12,
    /// <summary>
    /// User changed their password
    /// </summary>
    ChangePassword = 13,
    /// <summary>
    /// User made a password reset request
    /// </summary>
    PasswordResetRequest = 14,
    /// <summary>
    /// User completed a password reset
    /// </summary>
    PasswordReset = 15
}