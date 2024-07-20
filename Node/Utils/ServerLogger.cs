using FileFlows.Plugin;
using FileFlows.RemoteServices;
using FileFlows.ServerShared;
using FileFlows.ServerShared.Services;

namespace FileFlows.Node.Utils;

/// <summary>
/// Log writer that sends log messages to the FileFlows server
/// </summary>
public class ServerLogger:ILogWriter
{
    /// <summary>
    /// Constructs a server logger
    /// </summary>
    public ServerLogger()
    {
        Logger.Instance.RegisterWriter(this);
    }
    
    /// <summary>
    /// Logs a message
    /// </summary>
    /// <param name="type">the type of log message</param>
    /// <param name="args">the arguments for the log message</param>
    public Task Log(LogType type, params object[] args)
    {
        if (string.IsNullOrEmpty(AppSettings.Instance.ServerUrl))
            return Task.CompletedTask; // not registered
        
        // we do not await this!
        Task.Run(() =>
        {
            var service = ServiceLoader.Load<ILogService>();
            return service.LogMessage(new()
            {
                NodeAddress = AppSettings.Instance.HostName,
                Type = type,
                Arguments = args
            });
        });
        return Task.CompletedTask;
    }
}