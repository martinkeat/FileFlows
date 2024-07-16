using System.Text.Json;
using FileFlows.Plugin;
using FileFlows.Shared;

namespace FileFlows.ServerShared;

/// <summary>
/// A Logger that writes its output to file
/// </summary>
public class FileLogger : ILogWriter
{
    private string LogPrefix;
    private string LoggingPath;

    private DateOnly LogDate = DateOnly.MinValue;

    private SemaphoreSlim mutex = new SemaphoreSlim(1);

    /// <summary>
    /// Gets an instance of the FileLogger
    /// </summary>
    public static FileLogger Instance { get; private set; } = null!;

    /// <summary>
    /// Creates a file logger
    /// </summary>
    /// <param name="loggingPath">The path where to save the log file to</param>
    /// <param name="logPrefix">The prefix to use for the log file name</param>
    /// <param name="register">if this logger should be registered</param>
    public FileLogger(string loggingPath, string logPrefix, bool register = true)
    {
        this.LoggingPath = loggingPath;
        this.LogPrefix = logPrefix;
        if (register)
        {
            Shared.Logger.Instance.RegisterWriter(this);
            Instance = this;
        }
    }
    
    /// <summary>
    /// Logs a message
    /// </summary>
    /// <param name="type">the type of log message</param>
    /// <param name="args">the arguments for the log message</param>
    public async Task Log(LogType type, params object[] args)
    {
        await mutex.WaitAsync();
        try
        {
            string logFile = GetLogFilename();
            string prefix = type switch
            {
                LogType.Info => " [INFO]",
                LogType.Error => " [ERRR]",
                LogType.Warning => " [WARN]",
                LogType.Debug => " [DBUG]",
                _ => ""
            };

            string text = string.Join(
                ", ", args.Select(x =>
                {
                    if (x == null)
                        return "null";
                    if (x.GetType().IsPrimitive)
                        return x.ToString();
                    if (x is string str)
                        return str;
                    if (x is JsonElement je)
                    {
                        if (je.ValueKind == JsonValueKind.True)
                            return "true";
                        if (je.ValueKind == JsonValueKind.False)
                            return "false";
                        if (je.ValueKind == JsonValueKind.String)
                            return je.GetString();
                        if (je.ValueKind == JsonValueKind.Number)
                            return je.GetInt64().ToString();
                        return je.ToString();
                    }

                    return JsonSerializer.Serialize(x);
                }));

            string message = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + prefix + " -> " + text;
            if (message.IndexOf((char)0) >= 0)
            {
                message = message.Replace(new string((char)0, 1), string.Empty);
            }
            Console.WriteLine(message);
            
            
            if (File.Exists(logFile) == false)
            {
                await File.WriteAllTextAsync(logFile, message + Environment.NewLine);
            }
            else
            {
                using var fs = new FileStream(logFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                using StreamWriter sr = new StreamWriter(fs);
                await sr.WriteLineAsync(message);
                await sr.FlushAsync();
            }
        }
        finally
        {
            mutex.Release();
        }
    }
    
    /// <summary>
    /// Gets a tail of the log
    /// </summary>
    /// <param name="length">The number of lines to fetch</param>
    /// <returns>a tail of the log</returns>
    public string GetTail(int length = 50) => GetTail(length, Plugin.LogType.Info);

    /// <summary>
    /// Gets a tail of the log
    /// </summary>
    /// <param name="length">The number of lines to fetch</param>
    /// <param name="logLevel">the log level</param>
    /// <returns>a tail of the log</returns>
    public string GetTail(int length = 50, Plugin.LogType logLevel = Plugin.LogType.Info)
    {
        if (length <= 0 || length > 1000)
            length = 1000;

        mutex.Wait();
        try
        {
            return GetTailActual(length, logLevel);
        }
        finally
        {
            mutex.Release();
        }
    }

    private string GetTailActual(int length, Plugin.LogType logLevel)
    {
        string logFile = GetLogFilename();
        if (string.IsNullOrEmpty(logFile) || File.Exists(logFile) == false)
            return string.Empty;
        StreamReader reader = new StreamReader(logFile);
        reader.BaseStream.Seek(0, SeekOrigin.End);
        int count = 0;
        int max = length;
        if (logLevel != Plugin.LogType.Debug)
            max = 5000;
        while ((count < max) && (reader.BaseStream.Position > 0))
        {
            reader.BaseStream.Position--;
            int c = reader.BaseStream.ReadByte();
            if (reader.BaseStream.Position > 0)
                reader.BaseStream.Position--;
            if (c == Convert.ToInt32('\n'))
            {
                ++count;
            }
        }

        string str = reader.ReadToEnd();
        if (logLevel == Plugin.LogType.Debug)
            return str;
        
        string[] arr = str.Replace("\r", "").Split('\n');
        arr = arr.Where(x =>
        {
            if (logLevel < Plugin.LogType.Debug && x.Contains("DBUG"))
                return false;
            if (logLevel < Plugin.LogType.Info && x.Contains("INFO"))
                return false;
            if (logLevel < Plugin.LogType.Warning && x.Contains("WARN"))
                return false;
            return true;
        }).Take(length).ToArray();
        reader.Close();
        return string.Join("\n", arr);
    }

    
    /// <summary>
    /// Gets the name of the filename to log to
    /// </summary>
    /// <returns>the name of the filename to log to</returns>
    public string GetLogFilename()
    {
        string file = Path.Combine(LoggingPath, LogPrefix + "-" + DateTime.Now.ToString("MMMdd"));
        string latestAcceptableFile = string.Empty;
        for (int i = 99; i >= 0; i--)
        {
            FileInfo fi = new(file + "-" + i.ToString("D2") + ".log");
            if (fi.Exists == false)
            {
                latestAcceptableFile = fi.FullName;
            }
            else if (fi.Length < 10_000_000)
            {
                latestAcceptableFile = fi.FullName;
                break;
            }
            else
            {
                break;
            }
        }
        return latestAcceptableFile;
    }
}
