using FileFlows.Managers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// Audit service
/// </summary>
public class AuditService
{
    /// <summary>
    /// Performs a search of the audit log
    /// </summary>
    /// <param name="filter">the search filter</param>
    /// <returns>the result</returns>
    public Task<List<AuditEntry>> Search(AuditSearchFilter filter)
        => new AuditManager().Search(filter);

    /// <summary>
    /// Gets the audit history for a specific object
    /// </summary>
    /// <param name="type">the type of object</param>
    /// <param name="uid">the UID of the object</param>
    /// <returns>the audit history of the object</returns>
    public Task<List<AuditEntry>> ObjectHistory(string type, Guid uid)
        => new AuditManager().ObjectHistory(type, uid);
    
    #region Login Audits
    /// <summary>
    /// Audits a user login
    /// </summary>
    /// <param name="userUid">the UID of the user</param>
    /// <param name="userName">the name of the user</param>
    /// <param name="ipAddress">the IP address the user is connecting from</param>
    public async Task AuditLogin(Guid userUid, string userName, string ipAddress)
    {
        await new AuditManager().Audit(new ()
        {
            OperatorUid = userUid,
            OperatorName = userName,
            OperatorType = OperatorType.User,
            Action = AuditAction.Login,
            IPAddress = ipAddress,
            ObjectType = typeof(User).FullName!,
            ObjectUid = userUid
        });
    }

    /// <summary>
    /// Audits a failed user login
    /// </summary>
    /// <param name="userUid">the UID of the user</param>
    /// <param name="userName">the name of the user</param>
    /// <param name="ipAddress">the IP address the user is connecting from</param>
    public async Task AuditLoginFail(Guid userUid, string userName, string ipAddress)
    {
        await new AuditManager().Audit(new ()
        {
            OperatorUid = userUid,
            OperatorName = userName,
            OperatorType = OperatorType.User,
            Action = AuditAction.LoginFailed,
            IPAddress = ipAddress,
            ObjectType = typeof(User).FullName!,
            ObjectUid = userUid
        });
    } 

    /// <summary>
    /// Audits a failed user login
    /// </summary>
    /// <param name="userUid">the UID of the user</param>
    /// <param name="userName">the name of the user</param>
    /// <param name="ipAddress">the IP address the user is connecting from</param>
    public async Task AuditPasswordChange(Guid userUid, string userName, string ipAddress)
    {
        await new AuditManager().Audit(new ()
        {
            OperatorUid = userUid,
            OperatorName = userName,
            OperatorType = OperatorType.User,
            Action = AuditAction.ChangePassword,
            IPAddress = ipAddress,
            ObjectType = typeof(User).FullName!,
            ObjectUid = userUid
        });
    }

    /// <summary>
    /// Audits a password reset request
    /// </summary>
    /// <param name="userUid">the UID of the user</param>
    /// <param name="userName">the name of the user</param>
    /// <param name="ipAddress">the IP address the user is connecting from</param>
    public async Task AuditPasswordResetRequest(Guid userUid, string userName, string ipAddress)
    {
        await new AuditManager().Audit(new ()
        {
            OperatorUid = userUid,
            OperatorName = userName,
            OperatorType = OperatorType.User,
            Action = AuditAction.PasswordResetRequest,
            IPAddress = ipAddress,
            ObjectType = typeof(User).FullName!,
            ObjectUid = userUid
        });
    }

    /// <summary>
    /// Audits a password reset
    /// </summary>
    /// <param name="userUid">the UID of the user</param>
    /// <param name="userName">the name of the user</param>
    /// <param name="ipAddress">the IP address the user is connecting from</param>
    public async Task AuditPasswordReset(Guid userUid, string userName, string ipAddress)
    {
        await new AuditManager().Audit(new ()
        {
            OperatorUid = userUid,
            OperatorName = userName,
            OperatorType = OperatorType.User,
            Action = AuditAction.PasswordReset,
            IPAddress = ipAddress,
            ObjectType = typeof(User).FullName!,
            ObjectUid = userUid
        });
    }
    #endregion
}