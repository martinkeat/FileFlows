using FileFlows.Managers;
using FileFlows.Plugin;
using FileFlows.ServerShared.Models;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// User Service
/// </summary>
public class UserService
{
    /// <summary>
    /// Gets all users
    /// </summary>
    /// <returns>The users</returns>
    public Task<List<User>> GetAll()
        => new UserManager().GetAll();
    
    /// <summary>
    /// Gets a user by their UID
    /// </summary>
    /// <param name="uid">The UID of the user</param>
    /// <returns>The user</returns>
    public Task<User?> GetByUid(Guid uid)
        => new UserManager().GetByUid(uid);
    
    /// <summary>
    /// Gets a user by their name
    /// </summary>
    /// <param name="name">The name of the user</param>
    /// <returns>The user</returns>
    public Task<User?> GetByName(string name)
        => new UserManager().GetByName(name);
    
    /// <summary>
    /// Gets if there are any users in the system
    /// </summary>
    /// <returns>true if there are any users</returns>
    public Task<bool> HasAny()
        => new UserManager().HasAny();

    /// <summary>
    /// Finds a user by their username or email address
    /// </summary>
    /// <param name="usernameOrEmail">their username or email</param>
    /// <returns>the user if found</returns>
    public Task<User?> FindUser(string usernameOrEmail)
        => new UserManager().FindUser(usernameOrEmail);
    
    /// <summary>
    /// Updates a user
    /// </summary>
    /// <param name="user">the user to update</param>
    /// <param name="auditDetails">The audit details</param>
    /// <returns>the update result</returns>
    public Task<Result<User>> Update(User user, AuditDetails? auditDetails)
        => new UserManager().Update(user, auditDetails);

    /// <summary>
    /// Deletes the given user
    /// </summary>
    /// <param name="uids">the UID of the tasks to delete</param>
    /// <param name="auditDetails">the audit details</param>
    /// <returns>a task to await</returns>
    public Task Delete(Guid[] uids, AuditDetails auditDetails)
        => new UserManager().Delete(uids, auditDetails);

    /// <summary>
    /// Validate the login
    /// </summary>
    /// <param name="username">the username</param>
    /// <param name="password">the password</param>
    /// <param name="ipAddress">the IP address the request came from</param>
    /// <returns>the user if valid</returns>
    public async Task<Result<User>> ValidateLogin(string username, string password, string ipAddress)
    {
        var user = await GetByName(username);
        if (user == null)
            return Result<User>.Fail(Translater.Instant("Pages.Login.Messages.InvalidUsernameOrPassword"));

        LoginAttempts.TryGetValue(user.Uid, out LoginAttempt? loginAttempt);
        if (loginAttempt != null && loginAttempt.LockedOutUntilUtc > DateTime.UtcNow)
        {
            await ServiceLoader.Load<AuditService>().AuditLoginFail(user.Uid, user.Name, ipAddress);
            return Result<User>.Fail(Translater.Instant("Pages.Login.Messages.LockedOut"));
        }

        if (BCrypt.Net.BCrypt.Verify(password, user.Password) == false)
        {
            var settings = await ServiceLoader.Load<ISettingsService>().Get();
            loginAttempt ??= new();
            int minutes = settings.LoginLockoutMinutes < 1 ? 20 : settings.LoginLockoutMinutes;
            
            if (loginAttempt.LastAttemptUtc < DateTime.UtcNow.AddMinutes(-minutes))
                loginAttempt.Attempt = 0; // rest the attempts
            
            loginAttempt.Attempt += 1;
            loginAttempt.LastAttemptUtc = DateTime.UtcNow;
            int maxAttempts = settings.LoginMaxAttempts < 1 ? 10 : settings.LoginMaxAttempts;
            if (loginAttempt.Attempt >= maxAttempts)
            {
                loginAttempt.LockedOutUntilUtc = DateTime.UtcNow.AddMinutes(minutes);
            }

            await ServiceLoader.Load<AuditService>().AuditLoginFail(user.Uid, user.Name, ipAddress);
            LoginAttempts[user.Uid] = loginAttempt;
            
            return Result<User>.Fail(Translater.Instant("Pages.Login.Messages.InvalidUsernameOrPassword"));
        }

        if(loginAttempt != null)
            LoginAttempts.Remove(user.Uid);
        return user;
    }

    /// <summary>
    /// Changes a users password
    /// </summary>
    /// <param name="user">the user</param>
    /// <param name="oldPassword">the old password</param>
    /// <param name="newPassword">the new password</param>
    /// <returns>if the password was changed</returns>
    public async Task<bool> ChangePassword(User user, string oldPassword, string newPassword)
    {
        if (BCrypt.Net.BCrypt.Verify(oldPassword, user.Password) == false)
            return false;
        user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await new UserManager().Update(user, null); // nul here since we already audit the password change
        return true;
    }

    /// <summary>
    /// Records a login for a user
    /// </summary>
    /// <param name="user">the user to record the login for</param>
    /// <param name="ipAddress">the IP Address</param>
    /// <returns>a task to await</returns>
    public async Task RecordLogin(User user, string ipAddress)
    {
        await new UserManager().RecordLogin(user, ipAddress);
        await ServiceLoader.Load<AuditService>().AuditLogin(user.Uid, user.Name, ipAddress);
    }

    /// <summary>
    /// Login attempts
    /// </summary>
    private static Dictionary<Guid, LoginAttempt> LoginAttempts = new();

    /// <summary>
    /// A login attempt
    /// </summary>
    class LoginAttempt
    {
        /// <summary>
        /// Gets or sets the failed login attempts
        /// </summary>
        public int Attempt { get; set; }
        /// <summary>
        /// Gets or sets the date to lock ou that user until
        /// </summary>
        public DateTime LockedOutUntilUtc { get; set; }
        
        /// <summary>
        /// Gets or sets the last attempt
        /// </summary>
        public DateTime LastAttemptUtc { get; set; }
    }
}