using FileFlows.Plugin;
using FileFlows.Server.Database;
using FileFlows.Server.Database.Managers;
using FileFlows.Server.Helpers;
using FileFlows.Server.Middleware;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Models;
using FileFlows.Shared.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace FileFlows.Server.Controllers;

/// <summary>
/// System log controller
/// </summary>
[Route("/api/log")]
public class LogController : Controller
{
    /// <summary>
    /// Gets the system log
    /// </summary>
    /// <returns>the system log</returns>
    [HttpGet]
    public string Get([FromQuery] Plugin.LogType logLevel = Plugin.LogType.Info)
    {
        if (Logger.Instance.TryGetLogger(out FileLogger logger))
        {
            string log = logger.GetTail(1000, logLevel);
            string html = LogToHtml.Convert(log);
            return FixLog(html);
        }
        return string.Empty;
    }
    
    private string FixLog(string log)
    => log.Replace("\\u0022", "\"")
            .Replace("\\u0027", "'");

    /// <summary>
    /// Get the available log sources
    /// </summary>
    /// <returns>the available log sources</returns>
    [HttpGet("log-sources")]
    public async Task<List<ListOption>> GetLogSources()
    {
        List<ListOption> sources = new();
        sources.Add(new() { Value = "", Label = "Server" });
        sources.Add(new() { Value = "DATABASE", Label = "Database" });

        var settings = await new SettingsController().Get();
        if(settings.LogEveryRequest)
            sources.Add(new() { Value = "HTTP", Label = "HTTP Requests" });

        var nodes = new NodeController().GetAll();
        foreach (var node in nodes)
        {
            if(node.Uid != Globals.InternalNodeUid) // internal logs to system log
                sources.Add(new() { Value = node.Uid.ToString(), Label = node.Name });
        }

        return sources;
    }

    /// <summary>
    /// Searches the log using the given filter
    /// </summary>
    /// <param name="filter">the search filter</param>
    /// <returns>the messages found in the log</returns>
    [HttpPost("search")]
    public async Task<string> Search([FromBody] LogSearchModel filter)
    {
        if (DbHelper.UseMemoryCache)
            return "Not using external database, cannot search";

        if (filter.Source == "HTTP")
            return LogToHtml.Convert(LoggingMiddleware.RequestLogger.GetTail(1000));
        if (filter.Source == "DATABASE")
            return LogToHtml.Convert(FlowDatabase.Logger.GetTail(1000));
        
        var messages = await DbHelper.SearchLog(filter);
        string log = string.Join("\n", messages.Select(x =>
        {
            string prefix = x.Type switch
            {
                LogType.Info => "INFO",
                LogType.Error => "ERRR",
                LogType.Warning => "WARN",
                LogType.Debug => "DBUG",
                _ => ""
            };

            return x.LogDate.ToString("yyyy-MM-dd HH:mm:ss.fff") + " [" + prefix + "] -> " + x.Message;
        }));
        string html = LogToHtml.Convert(log);
        return FixLog(html);
    }

    /// <summary>
    /// Downloads the full system log
    /// </summary>
    /// <param name="source">the source to download from</param>
    /// <returns>a download result of the full system log</returns>
    [HttpGet("download")]
    public IActionResult Download([FromQuery] string source)
    {
        if (source == "DATABASE")
        {
            string filename = FlowDatabase.Logger.GetLogFilename();
            byte[] content = System.IO.File.ReadAllBytes(filename);
            return File(content, "application/octet-stream", new FileInfo(filename).Name);
        }
        if (source == "HTTP")
        {
            string filename = LoggingMiddleware.RequestLogger.GetLogFilename();
            byte[] content = System.IO.File.ReadAllBytes(filename);
            return File(content, "application/octet-stream", new FileInfo(filename).Name);
        }
        
        if (Logger.Instance.TryGetLogger(out FileLogger logger))
        {
            string filename = logger.GetLogFilename();
            byte[] content = System.IO.File.ReadAllBytes(filename);
            
            return File(content, "application/octet-stream", new FileInfo(filename).Name);
        }
        
        string log = Logger.Instance.GetTail(10_000);
        byte[] data = System.Text.Encoding.UTF8.GetBytes(log);
        return File(data, "application/octet-stream", "FileFlows.log");
    }

    private readonly Dictionary<string, Guid> ClientUids = new (); 
        

    /// <summary>
    /// Logs a message to the server
    /// </summary>
    /// <param name="message">The log message to log</param>
    [HttpPost("message")]
    public async Task Log([FromBody] LogServiceMessage message)
    {
        if (message == null)
            return;
        if (string.IsNullOrEmpty(message.NodeAddress))
            return;

        if(ClientUids.TryGetValue(message.NodeAddress.ToLower(), out Guid clientUid) == false)
        {
            var nodes = new Services.NodeService().GetAll();
            foreach (var node in nodes)
            {
                if (node.Address.ToLower() == message.NodeAddress.ToLower())
                    clientUid = node.Uid;
                if (string.IsNullOrEmpty(node.Address) == false &&
                    ClientUids.ContainsKey(node.Address.ToLower()) == false)
                {
                    ClientUids.Add(node.Address.ToLower(), node.Uid);
                }
            }
        }

        if (clientUid == Guid.Empty)
        {
            Logger.Instance.ILog($"Failed to find client '{message.NodeAddress}', could not log message");
            return;
        }

        if (Logger.Instance.TryGetLogger(out DatabaseLogger logger))
        {
            await logger.Log(clientUid, message.Type, message.Arguments);
        }
    }
}
