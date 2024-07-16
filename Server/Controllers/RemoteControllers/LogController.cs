using System.Text.RegularExpressions;
using FileFlows.Plugin;
using FileFlows.Server.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace FileFlows.Server.Controllers.RemoteControllers;

/// <summary>
/// Log controller
/// </summary>
[Route("/remote/log")]
[FileFlowsApiAuthorize]
[ApiExplorerSettings(IgnoreApi = true)]
public class LogController : Controller
{
    /// <summary>
    /// The loggers
    /// </summary>
    private static Dictionary<string, FileLogger> Loggers = new ();

    /// <summary>
    /// The semaphore to allow the loggers to be thread safe
    /// </summary>
    private static FairSemaphore _semaphoreSlim = new (1);
        

    /// <summary>
    /// Logs a message to the server
    /// </summary>
    /// <param name="message">The log message to log</param>
    [HttpPost]
    public async Task Log([FromBody] LogServiceMessage message)
    {
        if (message == null)
            return;
        if (string.IsNullOrEmpty(message.NodeAddress))
            return;
        
        await _semaphoreSlim.WaitAsync();
        try
        {
            if (Loggers.TryGetValue(message.NodeAddress, out var logger) == false)
            {
                string name = message.NodeAddress;
                if (IsValidFileName(name) == false)
                    return;
                
                Loggers[message.NodeAddress] = new (DirectoryHelper.LoggingDirectory, "Node-" + name, false);
                logger = Loggers[message.NodeAddress];
            }

            await logger.Log(message.Type, message.Arguments);
        }
        finally
        {
            _semaphoreSlim.Release();
        }

        // if (Logger.Instance.TryGetLogger(out DatabaseLogger logger))
        // {
        //     await logger.Log(clientUid, message.Type, message.Arguments);
        // }
    }
    /// <summary>
    /// Checks if a file name is valid.
    /// </summary>
    /// <param name="fileName">The file name to validate.</param>
    /// <returns>True if the file name is valid; otherwise, false.</returns>
    /// <remarks>
    /// Valid file names consist only of alphanumeric characters, hyphens, and underscores.
    /// They do not contain sequences like '..' or '/' to prevent directory traversal.
    /// </remarks>
    public bool IsValidFileName(string fileName)
    {
        // Match only alphanumeric characters, hyphen, and underscore
        // Ensure it does not contain '..' or '/'
        return Regex.IsMatch(fileName, @"^[a-zA-Z0-9-_]+$") && fileName.Contains("..") == false && fileName.Contains("/") == false;
    }

}