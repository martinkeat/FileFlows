using FileFlows.Plugin;
using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.ServerShared.Services;

/// <summary>
/// Log Service interface
/// </summary>
public interface ILogService
{
    /// <summary>
    /// Logs a message to the server
    /// </summary>
    /// <param name="message">The log message to log</param>
    Task LogMessage(LogServiceMessage message);
}

/// <summary>
/// Model used when sending messages to the server
/// </summary>
public class LogServiceMessage
{
    /// <summary>
    /// Gets or sets address of the node this message came from
    /// </summary>
    public string NodeAddress { get; set; } = null!;
    /// <summary>
    /// Gets or sets the type of log message
    /// </summary>
    public LogType Type { get; set; }
    /// <summary>
    /// Gets or sets the arguments for the log
    /// </summary>
    public object[]? Arguments { get; set; }
}