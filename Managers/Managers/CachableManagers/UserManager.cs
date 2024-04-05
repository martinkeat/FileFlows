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
}