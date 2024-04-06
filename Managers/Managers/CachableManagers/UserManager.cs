namespace FileFlows.Managers;

/// <summary>
/// User Manager
/// </summary>
public class UserManager : CachedManager<User>
{
    /// <summary>
    /// Tasks do not need to update the configuration
    /// as they do not effect configuration on a Flow Runner
    /// </summary>
    public override bool IncrementsConfiguration => false;

    /// <summary>
    /// Finds a user by their username or email address
    /// </summary>
    /// <param name="usernameOrEmail">their username or email</param>
    /// <returns>the user if found</returns>
    public async Task<User?> FindUser(string usernameOrEmail)
    {
        usernameOrEmail = usernameOrEmail.ToLowerInvariant();
        return (await GetData()).FirstOrDefault(x => x.Name.ToLowerInvariant() == usernameOrEmail
                                                     || x.Email.ToLowerInvariant() == usernameOrEmail);
    }

    /// <summary>
    /// Gets if there are any users in the system
    /// </summary>
    /// <returns>true if there are any users</returns>
    public Task<bool> HasAny()
        => DatabaseAccessManager.Instance.ObjectManager.Any(typeof(User).FullName!);

    /// <summary>
    /// Records a login for a user
    /// </summary>
    /// <param name="user">the user to record the login for</param>
    public async Task RecordLogin(User user)
    {
        if (user == null)
            return;
        user.LastLoggedIn = DateTime.UtcNow;
        await DatabaseAccessManager.Instance.ObjectManager.SetDataValue(
            user.Uid,
            typeof(User).FullName!,
            nameof(user.LastLoggedIn),
            user.LastLoggedIn);
    }
}