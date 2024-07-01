using FileFlows.Plugin;

namespace FileFlows.ServerShared.Models;

/// <summary>
/// Message that is saved to the database
/// </summary>
public class DbLogMessage
{
    /// <summary>
    /// Gets or sets the UID of the client, use Guid.Empty for the server
    /// </summary>
    public Guid ClientUid { get; set; }

    /// <summary>
    /// Gets or sets when the message was logged
    /// </summary>
    public DateTime LogDate { get; set; }

    /// <summary>
    /// Gets or sets the type of log message
    /// </summary>
    public LogType Type { get; set; }
    
    /// <summary>
    /// Gets or sets the message to log
    /// </summary>
    public string Message { get; set; } = null!;
}