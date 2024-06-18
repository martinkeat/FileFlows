using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace FileFlows.FlowRunner;

using System;
using System.Linq;
using FileFlows.Plugin;
using FileFlows.Shared.Models;

/// <summary>
/// Logger specifically for the Flow execution
/// </summary>
public class FlowLogger : ILogger
{
    /// <summary>
    /// Gets or sets the log file used by this logger
    /// </summary>
    List<string> log = new List<string>();
    
    /// <summary>
    /// Logs a debug message
    /// </summary>
    /// <param name="args">the arguments for the log message</param>
    public void DLog(params object[] args) => Log(LogType.Debug, args);
    /// <summary>
    /// Logs a error message
    /// </summary>
    /// <param name="args">the arguments for the log message</param>
    public void ELog(params object[] args) => Log(LogType.Error, args);

    /// <summary>
    /// The run instance running this
    /// </summary>
    private readonly RunInstance runInstance;

    /// <summary>
    /// The communicator used to send messages to the server
    /// </summary>
    IFlowRunnerCommunicator? Communicator;
    
    /// <summary>
    /// Constructs a new instance of the flow logger
    /// </summary>
    /// <param name="runInstance">the run instance running this</param>
    /// <param name="communicator">a communicator to report messages to the FileFlows server</param>
    public FlowLogger(RunInstance runInstance, IFlowRunnerCommunicator communicator)
    {
        this.runInstance = runInstance;
        this.Communicator = communicator;
    }

    /// <summary>
    /// Logs an image
    /// </summary>
    /// <param name="path">the path to the image</param>
    public void Image(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || System.IO.File.Exists(path) == false)
            return;
        try
        {
            // Convert the image to base64 encoded JPEG
            string base64Image;
            // Load the image
            using (var image = SixLabors.ImageSharp.Image.Load(path))
            {
                // Downscale the image while preserving aspect ratio to fit within 640x480
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new(640, 480)
                }));

                using (MemoryStream stream = new MemoryStream())
                {
                    image.SaveAsJpeg(stream);
                    byte[] bytes = stream.ToArray();
                    base64Image = Convert.ToBase64String(bytes);
                }
            }

            string message = "data:image/jpeg;base64," + base64Image + ":640x480";
            
#if(DEBUG) // we dont need this in the log here, only dev when we dont capture the console output
            log.Add(message);
#endif
            
            Console.WriteLine(message);
            lock (Messages)
            {
                Messages.Append(message);
            }

            _ = Flush();
        }
        catch (Exception ex)
        {
            WLog($"Failed logging image '{path}': {ex.Message}");
        }
    }

    /// <summary>
    /// Logs a information message
    /// </summary>
    /// <param name="args">the arguments for the log message</param>
    public void ILog(params object[] args) => Log(LogType.Info, args);
    /// <summary>
    /// Logs a warning message
    /// </summary>
    /// <param name="args">the arguments for the log message</param>
    public void WLog(params object[] args) => Log(LogType.Warning, args);

    /// <summary>
    /// Gets or sets the library file this flow is executing
    /// </summary>
    public LibraryFile File { get; set; }
    
    /// <summary>
    /// The type of log message
    /// </summary>
    private enum LogType
    {
        /// <summary>
        /// A error message
        /// </summary>
        Error, 
        /// <summary>
        /// a warning message
        /// </summary>
        Warning,
        /// <summary>
        /// A informational message
        /// </summary>
        Info,
        /// <summary>
        /// A debug message
        /// </summary>
        Debug
    }

    public void SetCommunicator(IFlowRunnerCommunicator communicator)
    {
        this.Communicator = communicator;
        if (log.Any())
            Flush().Wait();
    }

    private StringBuilder Messages = new StringBuilder();

    /// <summary>
    /// Logs a message
    /// </summary>
    /// <param name="type">the type of message to log</param>
    /// <param name="args">the log message arguments</param>
    private void Log(LogType type, params object[] args)
    {
        if (args == null || args.Length == 0)
            return;
        string prefix = type switch
        {
            LogType.Info => "INFO",
            LogType.Error => "ERRR",
            LogType.Warning => "WARN",
            LogType.Debug => "DBUG",
            _ => ""
        };

        string message = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " [" + prefix + "] -> " +
            string.Join(", ", args.Select(x =>
            x == null ? "null" :
            x.GetType().IsPrimitive || x is string ? x.ToString() :
            JsonSerializer.Serialize(x)));
        log.Add(message);
        Console.WriteLine(message);

        int count = 0;
        lock (Messages)
        {
            Messages.Append(message);
            count = Messages.Length;
        }

        if (count > 10)
            _ = Flush();
    }

    private SemaphoreSlim semaphore = new(1);
    /// <summary>
    /// Flushes any log message to the server
    /// </summary>
    public async Task Flush()
    {
        await semaphore.WaitAsync();
        string messages;
        lock (Messages)
        {
            messages = Messages.ToString();
            Messages.Clear();
        }

        try
        {
            if(Communicator != null)
                await Communicator.LogMessage(runInstance.Uid, messages);
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Gets the full log as a string
    /// </summary>
    /// <returns>the full log as a string</returns>
    public override string ToString()
        => string.Join(Environment.NewLine, log);

    /// <summary>
    /// Gets the last number of log lines
    /// </summary>
    /// <param name="length">The maximum number of lines to grab</param>
    /// <returns>The last number of log lines</returns>
    public string GetTail(int length = 50)
    {
        if (length <= 0)
            length = 50;

        var noLines = log.Where(x => x.Contains("======================================================================") == false);
        if (noLines.Count() <= length)
            return String.Join(Environment.NewLine, noLines);
        return String.Join(Environment.NewLine, noLines.Skip(noLines.Count() - length));
    }
}
