using FileFlows.Managers;
using FileFlows.Plugin;
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
    /// <returns>the update result</returns>
    public Task<Result<User>> Update(User user)
        => new UserManager().Update(user);

    /// <summary>
    /// Deletes the given user
    /// </summary>
    /// <param name="uids">the UID of the tasks to delete</param>
    /// <returns>a task to await</returns>
    public Task Delete(Guid[] uids)
        => new UserManager().Delete(uids);

    /// <summary>
    /// Validate the login
    /// </summary>
    /// <param name="username">the username</param>
    /// <param name="password">the password</param>
    /// <returns>the user if valid</returns>
    public async Task<Result<User>> ValidateLogin(string username, string password)
    {
        var user = await GetByName(username);
        if (user == null)
            return Result<User>.Fail("Pages.Login.InvalidUsernameOrPassword");

        LoginAttempts.TryGetValue(user.Uid, out LoginAttempt? loginAttempt);
        if (loginAttempt != null && loginAttempt.LockedOutUntil > DateTime.UtcNow)
            return Result<User>.Fail("Pages.Login.LockedOut");

        if (BCrypt.Net.BCrypt.Verify(password, user.Password) == false)
        {
            loginAttempt ??= new();
            loginAttempt.Attempt += 1;
            if (loginAttempt.Attempt >= 10)
                loginAttempt.LockedOutUntil = DateTime.UtcNow.AddMinutes(20);
            LoginAttempts[user.Uid] = loginAttempt;
            
            return Result<User>.Fail("Pages.Login.InvalidUsernameOrPassword");
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
        await new UserManager().Update(user);
        return true;
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
        public DateTime LockedOutUntil { get; set; }
    }
}