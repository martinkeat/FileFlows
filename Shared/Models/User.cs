namespace FileFlows.Shared.Models;

/// <summary>
/// A user
/// </summary>
public class User : FileFlowObject
{
    /// <summary>
    /// Gets or sets the password
    /// </summary>
    public string Password { get; set; }
    
    /// <summary>
    /// Gets or sets the email
    /// </summary>
    public string Email { get; set; }
    
    /// <summary>
    /// Gets or sets the user role
    /// </summary>
    public UserRole Role { get; set; }
}

/// <summary>
/// User Role
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Basic role
    /// </summary>
    Basic = 1,
    /// <summary>
    /// Administrator
    /// </summary>
    Admin = 65535
}

/// <summary>
/// Lock out status
/// </summary>
public enum LockOutStatus
{
    /// <summary>
    /// Not locked out
    /// </summary>
    NotLockedOut = 0,
    /// <summary>
    /// Pending lockout
    /// </summary>
    LockoutPending = 1,
    /// <summary>
    /// Locked out
    /// </summary>
    LockedOut = 2,
}