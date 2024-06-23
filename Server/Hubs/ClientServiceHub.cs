using FileFlows.Plugin;
using FileFlows.Server.Services;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.SignalR;

namespace FileFlows.Server.Hubs;

/// <summary>
/// Controller for WebSocket communication with clients.
/// </summary>
public class ClientServiceHub : Hub
{
    /// <summary>
    /// Broadcast a message
    /// </summary>
    /// <param name="command">the command to send in the message</param>
    /// <param name="data">the data to go along with the command</param>
    public async Task BroadcastMessage(string command, string data)
    {
        await Clients.All.SendAsync("ReceiveMessage", command, data);
    }
}

/// <summary>
/// Manager for the client service hub
/// </summary>
public class ClientServiceManager
{
    /// <summary>
    /// The hub context
    /// </summary>
    private readonly IHubContext<ClientServiceHub> _hubContext;
    
    /// <summary>
    /// Gets the static instance of the Client Service Manager
    /// </summary>
    public static ClientServiceManager Instance { get; private set; }
    
    /// <summary>
    /// Creates an instance of the Client Service Manager
    /// </summary>
    /// <param name="hubContext">the hub context</param>
    public ClientServiceManager(IHubContext<ClientServiceHub> hubContext)
    {
        _hubContext = hubContext;
        Instance = this;
    }

    /// <summary>
    /// Sends a toast to the clients
    /// </summary>
    /// <param name="type">the type of toast to show</param>
    /// <param name="message">the message of the toast</param>
    public void SendToast(LogType type, string message)
        => _hubContext.Clients.All.SendAsync("Toast", new { Type = type, Message = message });

    /// <summary>
    /// Sends a notification to the clients
    /// </summary>
    /// <param name="severity">the severity of notification</param>
    /// <param name="title">the title of the notification</param>
    public void SendNotification(NotificationSeverity severity, string title)
        => _hubContext.Clients.All.SendAsync("Notification", new { Severity = severity, Title = title });

    /// <summary>
    /// A semaphore to ensure only one update is set at a time
    /// </summary>
    private SemaphoreSlim UpdateSemaphore = new SemaphoreSlim(1);
    
    /// <summary>
    /// Update executes
    /// </summary>
    /// <param name="executors">the executors</param>
    public async Task UpdateExecutors(Dictionary<Guid, FlowExecutorInfo> executors)
    {
        if (await UpdateSemaphore.WaitAsync(50) == false)
            return;

        try
        {
            Dictionary<Guid, FlowExecutorInfoMinified> minified = new();
            // Make a copy of the keys to avoid modifying the collection during enumeration
            var keys = new List<Guid>(executors.Keys);

            foreach (var key in keys)
            {
                if (executors.TryGetValue(key, out var executor) == false)
                    continue;

                minified[key] = new()
                {
                    Uid = key,
                    DisplayName = ServiceLoader.Load<FileDisplayNameService>().GetDisplayName(executor.LibraryFile.Name,
                        executor.LibraryFile.RelativePath,
                        executor.Library.Name),
                    LibraryName = executor.Library.Name,
                    LibraryFileUid = executor.LibraryFile.Uid,
                    LibraryFileName = executor.LibraryFile.Name,
                    RelativeFile = executor.RelativeFile,
                    NodeName = executor.NodeName,
                    CurrentPartName = executor.CurrentPartName,
                    StartedAt = executor.StartedAt,
                    CurrentPart = executor.CurrentPart,
                    TotalParts = executor.TotalParts,
                    CurrentPartPercent = executor.CurrentPartPercent,
                    Additional = executor.AdditionalInfos
                        ?.Where(x => x.Value.Expired == false)
                        ?.Select(x => new object[]
                        {
                            x.Key, x.Value.Value
                        })?.ToArray()
                };
            }

            await _hubContext.Clients.All.SendAsync("UpdateExecutors", minified);
            await Task.Delay(500); // creates a 500 ms delay between messages to the client
        }
        catch (Exception ex)
        {
            Logger.Instance.WLog("Failed updating executors: " + ex.Message);
        }
        finally
        {
            UpdateSemaphore.Release();
        }
    }

    /// <summary>
    /// Updates the file status
    /// </summary>
    public async Task UpdateFileStatus()
    {
        var status = await ServiceLoader.Load<LibraryFileService>().GetStatus();
        await _hubContext.Clients.All.SendAsync("UpdateFileStatus", status);
    }
    /// <summary>
    /// Called when a system is paused/unpaused
    /// </summary>
    /// <param name="minutes">how many minutes to pause the system for</param>
    public void SystemPaused(int minutes)
        => _hubContext.Clients.All.SendAsync("SystemPaused", minutes);

    /// <summary>
    /// Called when a file starts processing
    /// </summary>
    /// <param name="file">the file that's starting processing</param>
    public void StartProcessing(LibraryFile file)
        => _hubContext.Clients.All.SendAsync("StartProcessing", file);
    
    /// <summary>
    /// Called when a file finish processing
    /// </summary>
    /// <param name="file">the file that's finished processing</param>
    public void FinishProcessing(LibraryFile file)
        => _hubContext.Clients.All.SendAsync("FinishProcessing", file);
}