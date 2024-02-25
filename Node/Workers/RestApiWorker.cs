using FileFlows.ServerShared.Workers;
using Microsoft.AspNetCore.Http.Json;

namespace FileFlows.Node.Workers;

/// <summary>
/// A worker that provides a basic REST api to the node
/// </summary>
public class RestApiWorker: Worker
{
    /// <summary>
    /// The port the API runs on
    /// </summary>
    internal static int Port = 19201;
    
    private DateTime startedAt = DateTime.UtcNow;

    /// <summary>
    /// Constructs the REST API worker
    /// </summary>
    public RestApiWorker() : base(ScheduleType.Startup, 0)
    {
    }

    private bool started = false;

    protected override void Execute()
    {
        if (started)
            return;

        int port = Port;
        if (port < 1 || port > 65535)
            port = 19201;

        started = true;
        var builder = WebApplication.CreateBuilder();
        
        // this changes the JSON from camelCase to PascalCase
        builder.Services.Configure<JsonOptions>(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = null;
        });
        
        var app = builder.Build();

        app.MapGet("/", () => "Hello World!");
        app.MapGet("/status", () => new 
        {
            StartedAt = startedAt,
            UpTime = DateTime.UtcNow.Subtract(startedAt).TotalSeconds, 
            FlowWorker.ActiveRunners
        });

        app.Run($"http://[::]:{port}");
    }
}