using System.Threading.Tasks;
using FileFlows.Plugin;
using FileFlows.Shared;

namespace FileFlows.ServerShared;

/// <summary>
/// A logger that logs to a List&gt;string&lt; in memory
/// </summary>
public class StringLogger : ILogger
{
    private readonly List<string> Messages = new();
    private readonly bool AddPrefix;

    /// <summary>
    /// Constructs a new string logger
    /// </summary>
    /// <param name="addPrefix">if the prefix should be added to the messages</param>
    public StringLogger(bool addPrefix = true)
    {
        AddPrefix = addPrefix;
    }

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

    /// <summary>
    /// Gets the tail of the log
    /// </summary>
    /// <param name="length">the number of messages to get</param>
    public string GetTail(int length = 50)
    {
        if (Messages.Count <= length)
            return string.Join(Environment.NewLine, Messages);
        return string.Join(Environment.NewLine, Messages.TakeLast(50));
    }

    /// <summary>
    /// Logs a message
    /// </summary>
    /// <param name="type">the type of log to record</param>
    /// <param name="args">the arguments of the message</param>
    private void Log(LogType type, params object[] args)
    {
        string prefix = AddPrefix ? type + " -> " : string.Empty;
        string message = prefix + string.Join(", ", args.Select(x =>
            x == null ? "null" :
            x.GetType().IsPrimitive ? x.ToString() :
            x is string ? x.ToString() :
            System.Text.Json.JsonSerializer.Serialize(x)));
        Messages.Add(message);
    }

    /// <summary>
    /// Returns the entire log as a string
    /// </summary>
    /// <returns>the entire log</returns>
    public override string ToString()
        => string.Join(Environment.NewLine, Messages);
}