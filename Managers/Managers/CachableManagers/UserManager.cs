using FileFlows.Plugin;

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

    /// <inheritdoc />
    protected override async Task<Result<User>> CustomUpdateValidate(User item)
    {
        var other = (await GetAll()).FirstOrDefault(x =>
            x.Email.ToLowerInvariant() == item.Email.ToLowerInvariant() && x.Uid != item.Uid);
        if (other != null)
            return Result<User>.Fail(Translater.Instant("Pages.Users.Messages.EmailInUse",
                new { email = other.Email, user = other.Name }));

        return item;
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
    /// <param name="ipAddress">the IP Address</param>
    public async Task RecordLogin(User user, string ipAddress)
    {
        if (user == null)
            return;
        user.LastLoggedIn = DateTime.UtcNow;
        user.LastLoggedInAddress = ipAddress ?? string.Empty;
        await DatabaseAccessManager.Instance.ObjectManager.SetDataValue(
            user.Uid,
            typeof(User).FullName!,
            nameof(user.LastLoggedIn),
            user.LastLoggedIn);
        await DatabaseAccessManager.Instance.ObjectManager.SetDataValue(
            user.Uid,
            typeof(User).FullName!,
            nameof(user.LastLoggedInAddress),
            user.LastLoggedInAddress);
    }
}