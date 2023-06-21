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
    /// Update executes
    /// </summary>
    /// <param name="executors">the executors</param>
    public void UpdateExecutors(Dictionary<Guid, FlowExecutorInfo> executors)
    {
        _hubContext.Clients.All.SendAsync("UpdateExecutors", executors);
    }

    /// <summary>
    /// Updates the file status
    /// </summary>
    public void UpdateFileStatus()
    {
        var status = new LibraryFileService().GetStatus().ToList();
        _hubContext.Clients.All.SendAsync("UpdateFileStatus", status);
    }
}