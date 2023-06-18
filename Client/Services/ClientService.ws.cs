using FileFlows.Client.Components;
using FileFlows.Plugin;
using Microsoft.AspNetCore.SignalR.Client;

namespace FileFlows.Client.Services;

/// <summary>
/// Service for connecting to the SignalR server and handling incoming messages and commands.
/// </summary>
public partial class ClientService
{
    /// <summary>
    /// The SignalR hub connection.
    /// </summary>
    private HubConnection _hubConnection;

    /// <summary>
    /// Indicates whether the client is connected to the SignalR server.
    /// </summary>
    private bool _isConnected;

    /// <summary>
    /// Event raised when the client is connected to the SignalR server.
    /// </summary>
    public event Action Connected;

    /// <summary>
    /// Event raised when the client is disconnected from the SignalR server.
    /// </summary>
    public event Action Disconnected;

    /// <summary>
    /// Starts the client service asynchronously.
    /// </summary>
    public async Task StartAsync()
    {
        await ConnectAsync();
    }

    /// <summary>
    /// Connects to the SignalR server.
    /// </summary>
    private async Task ConnectAsync()
    {
        while (true) // Retry indefinitely
        {
            try
            {
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(ServerUri)
                    .Build();

                _hubConnection.Closed += async (exception) =>
                {
                    _isConnected = false;
                    Disconnected?.Invoke();
                    await Task.Delay(TimeSpan.FromSeconds(5)); // Delay before reconnecting
                    await ConnectAsync();
                };

                _hubConnection.On<ToastData>("Toast", HandleToast);

                await _hubConnection.StartAsync();

                _isConnected = true;
                Connected?.Invoke();

                return; // Connected successfully, exit the method
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to the SignalR server: {ex.Message}");
                await Task.Delay(TimeSpan.FromSeconds(5)); // Delay before reconnecting
            }
        }
    }

    /// <summary>
    /// Handles the toast data received from the SignalR server.
    /// </summary>
    /// <param name="data">The toast data.</param>
    private void HandleToast(ToastData data)
    {
        switch (data.Type)
        {
            case LogType.Info:
                Toast.ShowInfo(data.Message);
                break;
            case LogType.Debug:
                Toast.ShowSuccess(data.Message);
                break;
            case LogType.Warning:
                Toast.ShowWarning(data.Message);
                break;
            case LogType.Error:
                Toast.ShowError(data.Message);
                break;
        }
    }
    
    /// <summary>
    /// Represents the toast data received from the SignalR server.
    /// </summary>
    private class ToastData
    {
        /// <summary>
        /// Gets or sets the type of the toast.
        /// </summary>
        public LogType Type { get; set; }

        /// <summary>
        /// Gets or sets the toast message.
        /// </summary>
        public string Message { get; set; }
    }
}
