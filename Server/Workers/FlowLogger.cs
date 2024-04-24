// namespace FileFlows.Server.Workers;
//
// using System;
// using System.Linq;
// using FileFlows.Plugin;
// using FileFlows.Shared.Models;
//
// /// <summary>
// /// Logger specifically for the Flow execution
// /// </summary>
// public class FlowLogger : ILogger
// {
//     /// <summary>
//     /// Gets or sets the log file used by this logger
//     /// </summary>
//     public string LogFile { get; set; }
//     
//     List<string> log = new List<string>();
//     
//     /// <summary>
//     /// Logs a debug message
//     /// </summary>
//     /// <param name="args">the arguments for the log message</param>
//     public void DLog(params object[] args) => Log(LogType.Debug, args);
//     /// <summary>
//     /// Logs a error message
//     /// </summary>
//     /// <param name="args">the arguments for the log message</param>
//     public void ELog(params object[] args) => Log(LogType.Error, args);
//
//     /// <summary>
//     /// Logs a information message
//     /// </summary>
//     /// <param name="args">the arguments for the log message</param>
//     public void ILog(params object[] args) => Log(LogType.Info, args);
//     /// <summary>
//     /// Logs a warning message
//     /// </summary>
//     /// <param name="args">the arguments for the log message</param>
//     public void WLog(params object[] args) => Log(LogType.Warning, args);
//
//     /// <summary>
//     /// Gets or sets the library file this flow is logging against
//     /// </summary>
//     public LibraryFile File { get; set; }
//     private enum LogType
//     {
//         Error, Warning, Info, Debug
//     }
//
//     private void Log(LogType type, params object[] args)
//     {
//         if (args == null || args.Length == 0)
//             return;
//         string message = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " - " + type + " -> " +
//             string.Join(", ", args.Select(x =>
//             x == null ? "null" :
//             x.GetType().IsPrimitive || x is string ? x.ToString() :
//             System.Text.Json.JsonSerializer.Serialize(x)));
//         log.Add(message);
//         if(type != LogType.Debug)
//             Console.WriteLine(message);
//         if (string.IsNullOrEmpty(LogFile) == false)
//             System.IO.File.AppendAllText(LogFile, message + Environment.NewLine);
//     }
//
//     /// <summary>
//     /// Returns the full log of as a string
//     /// </summary>
//     /// <returns>The full log</returns>
//     public override string ToString() => String.Join(Environment.NewLine, log);
//
//     /// <summary>
//     /// Gets the last number of log lines
//     /// </summary>
//     /// <param name="length">The maximum number of lines to grab</param>
//     /// <returns>The last number of log lines</returns>
//     public string GetTail(int length = 50)
//     {
//         lock (log)
//         {
//             if (length > 0 && log.Count < length)
//                 return String.Join(Environment.NewLine, log.Skip(log.Count - length));
//             return String.Join(Environment.NewLine, log);
//         }
//     }
// }
