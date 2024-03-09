using System.Collections;
using System.Net;
using Avalonia;
using FileFlows.Server.Helpers;
using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;

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
#if DEBUG
        // args = new[] { "--gui" };    
#endif
        if (args.Any(x =>
                x.ToLower() == "--help" || x.ToLower() == "-?" || x.ToLower() == "/?" || x.ToLower() == "/help" ||
                x.ToLower() == "-help"))
        {
            Console.WriteLine("FileFlows v" + Globals.Version);
            Console.WriteLine("--gui: To show the full GUI");
            Console.WriteLine("--minimal-gui: To show the limited GUI application");
            Console.WriteLine(
                "--base-dir: Optional override to set where the base data files will be read/saved to");
            return;
        }


        if (Globals.IsLinux && args?.Any(x => x == "--systemd") == true)
        {
            if (args?.Any(x => x == "--uninstall") == true)
                SystemdService.Uninstall(false);
            else
                SystemdService.Install(DirectoryHelper.BaseDirectory, isNode: false);
            return;
        }
        
        
        Application app = Services.ServiceLoader.Provider.GetRequiredService<Application>();
        app.Run(args);
    }
    
}
