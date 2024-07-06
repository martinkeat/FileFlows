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
    
    /// <summary>
    /// Gets or sets when the user last logged in, in UTC time
    /// </summary>
    public DateTime LastLoggedIn { get; set; }
    
    /// <summary>
    /// Gets or sets address of the last login
    /// </summary>
    public string LastLoggedInAddress { get; set; }
}

/// <summary>
/// User Role
/// </summary>
[Flags]
public enum UserRole
{
    /// <summary>
    /// Files role
    /// </summary>
    Files = 1,
    /// <summary>
    /// Flows role
    /// </summary>
    Flows = 2,
    /// <summary>
    /// Libraries role
    /// </summary>
    Libraries = 4,
    /// <summary>
    /// Nodes role
    /// </summary>
    Nodes = 8,
    /// <summary>
    /// Log role
    /// </summary>
    Log = 16,
    /// <summary>
    /// Plugins role
    /// </summary>
    Plugins = 32,
    /// <summary>
    /// Scripts role
    /// </summary>
    Scripts = 64,
    /// <summary>
    /// Variables role
    /// </summary>
    Variables = 128,
    /// <summary>
    /// Revisions role
    /// </summary>
    Revisions = 256,
    /// <summary>
    /// Tasks role
    /// </summary>
    Tasks = 512,
    /// <summary>
    /// Webhooks role
    /// </summary>
    Webhooks = 1024,
    /// <summary>
    /// Pause Processing role
    /// </summary>
    PauseProcessing = 2048,
    /// <summary>
    /// Docker Mod role
    /// </summary>
    DockerMods = 4096,
    /// <summary>
    /// Reports
    /// </summary>
    Reports = 8192,
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