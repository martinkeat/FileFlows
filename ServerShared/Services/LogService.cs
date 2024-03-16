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
    public string NodeAddress { get; set; }
    /// <summary>
    /// Gets or sets the type of log message
    /// </summary>
    public LogType Type { get; set; }
    /// <summary>
    /// Gets or sets the arguments for the log
    /// </summary>
    public object[] Arguments { get; set; }
}

/// <summary>
/// A service used to log messages to the server
/// </summary>
public class LogService:Service, ILogService
{

    /// <summary>
    /// Gets or sets a function used to load new instances of the service
    /// </summary>
    public static Func<ILogService> Loader { get; set; }

    /// <summary>
    /// Loads an instance of the script service
    /// </summary>
    /// <returns>an instance of the script service</returns>
    public static ILogService Load()
    {
        if (Loader == null)
            return new LogService();
        return Loader.Invoke();
    }

    /// <summary>
    /// Logs a message to the server
    /// </summary>
    /// <param name="message">The log message to log</param>
    public async Task LogMessage(LogServiceMessage message)
    {
        try
        {
            await HttpHelper.Post($"{ServiceBaseUrl}/api/fileflows-log/message", message, noLog: true);
        }
        catch (Exception)
        {
            // silent fail
        }
    }
}