using FileFlows.Plugin;
using FileFlows.Shared;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace FileFlows.FlowRunner;

/// <summary>
/// Interface used for the flow runner to communicate with the FileFlows server
/// </summary>
public interface IFlowRunnerCommunicator
{
    /// <summary>
    /// Logs a message to the FileFlows server
    /// </summary>
    /// <param name="runnerUid">the UID of the flow runner</param>
    /// <param name="message">the message to log</param>
    /// <returns>a completed task</returns>
    Task LogMessage(Guid runnerUid, string message);
}

/// <summary>
/// A communicator by the flow runner to communicate with the FileFlows server
/// </summary>
public class FlowRunnerCommunicator : IFlowRunnerCommunicator
{
    /// <summary>
    /// Gets or sets the URL to the signalr endpoint on the FileFlows server
    /// </summary>
    public static string SignalrUrl { get; set; }
    
    /// <summary>
    /// The signalr hub connection
    /// </summary>
    HubConnection connection;
    
    /// <summary>
    /// The UID of the executing library file
    /// </summary>
    private Guid LibraryFileUid;
    
    /// <summary>
    /// Delegate used when the flow is being canceled
    /// </summary>
    public delegate void Cancel();
    
    /// <summary>
    /// Event used when the flow is being canceled
    /// </summary>
    public event Cancel OnCancel;

    /// <summary>
    /// The run instance running this
    /// </summary>
    private readonly RunInstance runInstance;

    /// <summary>
    /// Creates an instance of the flow runner communicator
    /// </summary>
    /// <param name="runInstance">the run instance running this</param>
    /// <param name="libraryFileUid">the UID of the library file being executed</param>
    /// <exception cref="Exception">throws an exception if cannot connect to the server</exception>
    public FlowRunnerCommunicator(RunInstance runInstance, Guid libraryFileUid)
    {
        this.runInstance = runInstance;
        this.LibraryFileUid = libraryFileUid;
        runInstance.LogInfo("SignalrUrl: " + SignalrUrl);
        connection = new HubConnectionBuilder()
                            .WithUrl(new Uri(SignalrUrl))
                            .WithAutomaticReconnect()
                            .Build();
        connection.Closed += Connection_Closed;
        connection.On<Guid>("AbortFlow", (uid) =>
        {
            if (uid != LibraryFileUid)
                return;
            OnCancel?.Invoke();
        });
        connection.StartAsync().Wait();
        if (connection.State == HubConnectionState.Disconnected)
            throw new Exception("Failed to connect to signalr");
    }

    /// <summary>
    /// Closes the Signalr connection to the server
    /// </summary>
    public void Close()
    {
        try
        {
            connection?.DisposeAsync();
        }
        catch (Exception) { } // not sure if this can throw, but just in case
    }

    /// <summary>
    /// Called when the Signalr connection is closed
    /// </summary>
    /// <param name="obj">the connection object</param>
    /// <returns>a completed task</returns>
    private Task Connection_Closed(Exception? arg)
    {
        runInstance.LogInfo("Connection_Closed");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when the Signalr connection is received
    /// </summary>
    /// <param name="obj">the connection object</param>
    private void Connection_Received(string obj)
    {
        runInstance.LogInfo("Connection_Received");
    }

    /// <summary>
    /// Logs a message to the FileFlows server
    /// </summary>
    /// <param name="runnerUid">the UID of the flow runner</param>
    /// <param name="message">the message to log</param>
    /// <returns>a completed task</returns>
    public async Task LogMessage(Guid runnerUid, string message)
    {
        try
        {
            await connection.InvokeAsync("LogMessage", runnerUid, LibraryFileUid, message);
        } 
        catch (Exception)
        {
            // silently fail here, we store the log in memory if one message fails its not a biggie
            // once the flow is complete we send the entire log to the server to update
        }
    }

    /// <summary>
    /// Sends a hello to the server saying this runner is still executing
    /// </summary>
    /// <param name="runnerUid">the UID of the flow runner</param>
    /// <param name="info">The flow execution info</param>
    /// <param name="args">the node parameters</param>
    public async Task<bool> Hello(Guid runnerUid, FlowExecutorInfo info, NodeParameters args)
    {
        try
        {
            bool helloResult = await connection.InvokeAsync<bool>("Hello", runnerUid, JsonSerializer.Serialize(new
            {
                info.Library,
                info.Uid,
                info.CurrentPart,
                info.InitialSize,
                info.IsDirectory,
                info.LastUpdate,
                info.LibraryFile,
                info.LibraryPath,
                info.NodeName,
                info.NodeUid,
                info.RelativeFile,
                info.StartedAt,
                info.TotalParts,
                info.WorkingFile,
                info.CurrentPartName,
                info.CurrentPartPercent
            }));
            if(helloResult == false)
                args?.Logger?.WLog("Received a false from the hello request to the server");
            return helloResult;
        }
        catch(Exception ex)
        {
            args?.Logger?.ELog("Failed to send hello to server: " + ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Loads an instance of the FlowRunnerCommunicator
    /// </summary>
    /// <param name="runInstance">The run instance running this</param>
    /// <param name="libraryFileUid">the UID of the library file being processed</param>
    /// <returns>an instance of the FlowRunnerCommunicator</returns>
    public static FlowRunnerCommunicator Load(RunInstance runInstance, Guid libraryFileUid)
    {
        return new FlowRunnerCommunicator(runInstance, libraryFileUid);

    }
}