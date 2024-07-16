using FileFlows.Server.Cli;
using FileFlows.Server.Services;
using FileFlows.Shared.Helpers;

namespace FileFlows.Server;

/// <summary>
/// Main entry point for server
/// </summary>
public class Program
{
    /// <summary>
    /// General cache used by the server
    /// </summary>
    internal static CacheStore GeneralCache = new ();

    [STAThread] // need for Photino.net on windows
    public static void Main(string[] args)
    {
        if (CommandLine.Process(args))
            return;
        
        Application app = ServiceLoader.Provider.GetRequiredService<Application>();
        ServerShared.Services.SharedServiceLoader.Loader = type => ServiceLoader.Provider.GetRequiredService(type);
        app.Run(args);
    }
    
}
