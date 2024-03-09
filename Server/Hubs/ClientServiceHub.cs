using FileFlows.Plugin;
using FileFlows.Server.Services;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.SignalR;
using Org.BouncyCastle.Tls;


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

            foreach (var executor in executors)
            {
                minified[executor.Key] = new()
                {
                    Uid = executor.Key,
                    DisplayName = FileDisplayNameService.GetDisplayName(executor.Value.LibraryFile.Name, 
                        executor.Value.LibraryFile.RelativePath,
                        executor.Value.Library.Name),
                    LibraryName = executor.Value.Library.Name,
                    LibraryFileUid = executor.Value.LibraryFile.Uid,
                    LibraryFileName = executor.Value.LibraryFile.Name,
                    RelativeFile = executor.Value.RelativeFile,
                    NodeName = executor.Value.NodeName,
                    CurrentPartName = executor.Value.CurrentPartName,
                    StartedAt = executor.Value.StartedAt,
                    CurrentPart = executor.Value.CurrentPart,
                    TotalParts = executor.Value.TotalParts,
                    CurrentPartPercent = executor.Value.CurrentPartPercent,
                    Additional = executor.Value.AdditionalInfos.Where(x => x.Value.Expired == false)
                        .Select(x => new object[]
                        {
                            x.Key, x.Value.Value
                        }).ToArray()
                };
            }

            await _hubContext.Clients.All.SendAsync("UpdateExecutors", minified);
            await Task.Delay(500); // creates a 500 ms delay between messages to the client
        }
        finally
        {
            UpdateSemaphore.Release();
        }
    }

    /// <summary>
    /// Updates the file status
    /// </summary>
    public void UpdateFileStatus()
    {
        var status = ServiceLoader.Load<LibraryFileService>().GetStatus().Result;
        _hubContext.Clients.All.SendAsync("UpdateFileStatus", status);
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