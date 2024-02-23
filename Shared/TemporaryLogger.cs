using System.Threading.Tasks;
using FileFlows.Plugin;
using FileFlows.Shared;

namespace FileFlows.ServerShared;

/// <summary>
/// A logger that should be used temporary to log messages into memory then can be iterated later to add to another log
/// </summary>
public class TemporaryLogger : ILogger
{
    /// <summary>
    /// Gets the messages that have been logged
    /// </summary>
    public readonly List<(LogType Type, string Message)> Messages = new();

    /// <summary>
    /// Writes an information log message
    /// </summary>
    /// <param name="args">the log parameters</param>
    public void ILog(params object[] args)
        => Log(LogType.Info, args);

    /// <summary>
    /// Writes an debug log message
    /// </summary>
    /// <param name="args">the log parameters</param>
    public void DLog(params object[] args)
        => Log(LogType.Debug, args);

    /// <summary>
    /// Writes an warning log message
    /// </summary>
    /// <param name="args">the log parameters</param>
    public void WLog(params object[] args)
        => Log(LogType.Warning, args);

    /// <summary>
    /// Writes an error log message
    /// </summary>
    /// <param name="args">the log parameters</param>
    public void ELog(params object[] args)
        => Log(LogType.Error, args);

    /// <inheritdoc />
    public string GetTail(int length = 50)
        => "Not available";


    /// <summary>
    /// Logs a message
    /// </summary>
    /// <param name="type">the type of log to record</param>
    /// <param name="args">the arguments of the message</param>
    private void Log(LogType type, params object[] args)
    {
        string message = string.Join(", ", args.Select(x =>
            x == null ? "null" :
            x.GetType().IsPrimitive ? x.ToString() :
            x is string ? x.ToString() :
            System.Text.Json.JsonSerializer.Serialize(x)));
        Messages.Add((type, message));
    }

    /// <summary>
    /// Writes these messages to another log
    /// </summary>
    /// <param name="other">the other logger</param>
    public void WriteToLog(ILogger other)
    {
        if (other == null)
            return;
        
        foreach (var msg in Messages)
        {
            switch (msg.Type)
            {
                case LogType.Debug:
                    other.DLog(msg.Message);
                    break;
                case LogType.Info:
                    other.ILog(msg.Message);
                    break;
                case LogType.Warning:
                    other.WLog(msg.Message);
                    break;
                case LogType.Error:
                    other.ELog(msg.Message);
                    break;
            }
        }

    }
}