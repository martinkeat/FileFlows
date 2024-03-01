using System.Collections;
using System.Net;
using Avalonia;
using FileFlows.Server.Database;
using FileFlows.Server.Database.Managers;
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
    /// Gets if this is running inside a docker container
    /// </summary>
    public static bool Docker => ServerShared.Globals.IsDocker;
    private static Mutex appMutex = null;
    const string appName = "FileFlowsServer";
    /// <summary>
    /// General cache used by the server
    /// </summary>
    internal static CacheStore GeneralCache = new ();
    
    /// <summary>
    /// Gets or sets an optional entry point that launched this
    /// </summary>
    public static string? EntryPoint { get; private set; }

    public static void Main(string[] args)
    {
#if DEBUG
        args = new[] { "--gui" };    
#endif
        try
        {
            if (args.Any(x =>
                    x.ToLower() == "--help" || x.ToLower() == "-?" || x.ToLower() == "/?" || x.ToLower() == "/help" ||
                    x.ToLower() == "-help"))
            {
                Console.WriteLine("FileFlows v" + Globals.Version);
                Console.WriteLine("--gui: To show the full GUI");
                Console.WriteLine("--minimal-gui: To show the limited GUI application");
                Console.WriteLine("--base-dir: Optional override to set where the base data files will be read/saved to");
                return;
            }
            
            
            if (Globals.IsLinux && args?.Any(x => x == "--systemd") == true)
            {
                if(args?.Any(x => x == "--uninstall") == true)
                    SystemdService.Uninstall(false);
                else
                    SystemdService.Install(DirectoryHelper.BaseDirectory, isNode: false);
                return;
            }
            
            Globals.IsDocker = args?.Any(x => x == "--docker") == true;
            Globals.IsSystemd = args?.Any(x => x == "--systemd-service") == true;
            var baseDir = args.SkipWhile((arg, index) => arg != "--base-dir" || index == args.Length - 1).Skip(1).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(baseDir) == false)
                DirectoryHelper.BaseDirectory = baseDir;
            
            bool minimalGui = Globals.IsDocker == false && args?.Any(x => x.ToLowerInvariant() == "--minimal-gui") == true; 
            bool fullGui = Globals.IsDocker == false && args?.Any(x => x.ToLowerInvariant() == "--gui") == true;
            bool noGui = fullGui == false && minimalGui == false;
            Program.EntryPoint = args.SkipWhile((arg, index) => arg != "--entry-point" || index == args.Length - 1).Skip(1).FirstOrDefault();
            
            if(string.IsNullOrWhiteSpace(EntryPoint) == false && OperatingSystem.IsMacOS())
                File.WriteAllText(Path.Combine(DirectoryHelper.BaseDirectory, "version"), Globals.Version.Split('.').Last());

            if (noGui == false && Globals.IsWindows)
            {
                // hide the console on window
                Utils.WindowsConsoleManager.Hide();
            }
            
            if (Docker == false)
            {
                appMutex = new Mutex(true, appName, out bool createdNew);
                if (createdNew == false)
                {
                    // app is already running;
                    if (noGui)
                    {
                        Console.WriteLine("An instance of FileFlows is already running");
                    }
                    else
                    {
                        try
                        {
                            var appBuilder = Gui.Avalon.App.BuildAvaloniaApp(true);
                            appBuilder.StartWithClassicDesktopLifetime(args);
                        }
                        catch (Exception) { }
                    }
            
                    return;
                }
            }
            DirectoryHelper.Init(Docker, false);
            
            
            if(File.Exists(Path.Combine(DirectoryHelper.BaseDirectory, "server-upgrade.bat")))
                File.Delete(Path.Combine(DirectoryHelper.BaseDirectory, "server-upgrade.bat"));
            if(File.Exists(Path.Combine(DirectoryHelper.BaseDirectory, "server-upgrade.sh")))
                File.Delete(Path.Combine(DirectoryHelper.BaseDirectory, "server-upgrade.sh"));

            ServicePointManager.DefaultConnectionLimit = 50;

            InitializeLoggers();

            // must be done after directory helper otherwise will fail 
            Globals.CustomFileFlowsDotComUrl = AppSettings.Instance.FileFlowsDotComUrl;
            
            WriteLogHeader(args);

            CleanDefaultTempDirectory();
            
            HttpHelper.Client = HttpHelper.GetDefaultHttpHelper(string.Empty);
            
            CheckLicense();

            if (PrepareDatabase() == false)
                return;
            
            if (Docker || noGui)
            {
                Console.WriteLine("Starting FileFlows Server...");
                WebServer.Start(args);
            }
            else if(fullGui)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(50);
                    try
                    {
                        Logger.Instance.ILog("Starting FileFlows Server...");
                        WebServer.Start(args);
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.ELog("Failed starting server: " + ex.Message + Environment.NewLine + ex.StackTrace);
                    }
                });
                
                var webview = new Gui.Photino.WebView();
                webview.Open();;
            }
             else if(minimalGui) {
            
                DateTime dt = DateTime.Now;
                try
                {
                    var appBuilder = Gui.Avalon.App.BuildAvaloniaApp();
                    appBuilder.StartWithClassicDesktopLifetime(args);
                }
                catch (Exception ex)
                {
                    if(DateTime.Now.Subtract(dt) < new TimeSpan(0,0,2))
                        Console.WriteLine("Failed to launch GUI: " + ex.Message + Environment.NewLine + ex.StackTrace);
                    else
                        Console.WriteLine("Error: " + ex.Message + Environment.NewLine + ex.StackTrace);
                }
            }

            _ = WebServer.Stop();
            Console.WriteLine("Exiting FileFlows Server...");
        }
        catch (Exception ex)
        {
            try
            {
                Logger.Instance.ELog("Error: " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
            catch (Exception) { }
            Console.WriteLine("Error: " + ex.Message + Environment.NewLine + ex.StackTrace);
        }
    }

    private static void WriteLogHeader(string[] args)
    {
        Logger.Instance.ILog(new string('=', 50));
        Thread.Sleep(1); // so log message can be written
        Logger.Instance.ILog("Starting FileFlows " + Globals.Version);
        Thread.Sleep(1); // so log message can be written
        if(Docker)
            Logger.Instance.ILog("Running inside docker container");
        if(string.IsNullOrWhiteSpace(Program.EntryPoint) == false)
            Logger.Instance.DLog("Entry Point: " + EntryPoint);
        if(string.IsNullOrWhiteSpace(Globals.CustomFileFlowsDotComUrl) == false)
            Logger.Instance.ILog("Custom FileFlows.com: " + Globals.CustomFileFlowsDotComUrl);
        Thread.Sleep(1); // so log message can be written
        Logger.Instance.DLog("Arguments: " + (args?.Any() == true ? string.Join(" ", args) : "No arguments"));
        Thread.Sleep(1); // so log message can be written
        foreach (DictionaryEntry var in Environment.GetEnvironmentVariables())
        {
            Logger.Instance.DLog($"ENV.{var.Key} = {var.Value}");
            Thread.Sleep(1); // so log message can be written
        }
        Thread.Sleep(1); // so log message can be written
        Logger.Instance.ILog(new string('=', 50));
        Thread.Sleep(1); // so log message can be written
    }

    private static void CheckLicense()
    {
        LicenseHelper.Update().Wait();
    }

    private static void InitializeLoggers()
    {
        new ServerShared.FileLogger(DirectoryHelper.LoggingDirectory, "FileFlows");
        new ConsoleLogger();
    }

    private static bool PrepareDatabase()
    {
        if (string.IsNullOrEmpty(AppSettings.Instance.DatabaseConnection) == false &&
            AppSettings.Instance.DatabaseConnection.Contains(".sqlite") == false)
        {
            // check if licensed for external db, if not force migrate to sqlite
            if (LicenseHelper.IsLicensed(LicenseFlags.ExternalDatabase) == false)
            {
                #if(DEBUG)
                // twice for debugging so we can step into it and see why
                if (LicenseHelper.IsLicensed(LicenseFlags.ExternalDatabase) == false)
                {
                }
                #endif

                FlowDbConnection.Initialize(true);
                Logger.Instance.WLog("No longer licensed for external database, migrating to SQLite database.");
                AppSettings.Instance.DatabaseMigrateConnection = SqliteDbManager.GetDefaultConnectionString();
            }
            else
            {
                FlowDbConnection.Initialize(false);
            }
        }
        else
            FlowDbConnection.Initialize(true); 
        
        if (string.IsNullOrEmpty(AppSettings.Instance.DatabaseMigrateConnection) == false)
        {
            if (AppSettings.Instance.DatabaseConnection == AppSettings.Instance.DatabaseMigrateConnection)
            {
                AppSettings.Instance.DatabaseMigrateConnection = null;
                AppSettings.Instance.Save();
            }
            else if (AppSettings.Instance.RecreateDatabase == false &&
                     DbMigrater.ExternalDatabaseExists(AppSettings.Instance.DatabaseMigrateConnection))
            {
                Logger.Instance.ILog("Switching to existing database");
                AppSettings.Instance.DatabaseConnection = AppSettings.Instance.DatabaseMigrateConnection;
                AppSettings.Instance.DatabaseMigrateConnection = null;
                AppSettings.Instance.RecreateDatabase = false;
                AppSettings.Instance.Save();
            }
            else
            {
                Logger.Instance.ILog("Database migration starting");
                bool migrated = DbMigrater.Migrate(AppSettings.Instance.DatabaseConnection,
                    AppSettings.Instance.DatabaseMigrateConnection);
                if (migrated)
                    AppSettings.Instance.DatabaseConnection = AppSettings.Instance.DatabaseMigrateConnection;
                else
                {
                    Logger.Instance.ELog("Database migration failed, reverting to previous database settings");
                    #if(DEBUG)
                    throw new Exception("Migration failed");
                    #endif
                }

                AppSettings.Instance.DatabaseMigrateConnection = null;
                AppSettings.Instance.RecreateDatabase = false;
                AppSettings.Instance.Save();
            }
        }
            
        Logger.Instance.ILog("About to initialize Database");
        // initialize the database
        if (DbHelper.Initialize().Result == false)
        {
            Logger.Instance.ELog("Failed initializing database");
            return false;
        }
        Logger.Instance.ILog("Database initialized");
            
        // run any upgrade code that may need to be run
        var settings = DbHelper.Single<Settings>().Result;
        new Upgrade.Upgrader().Run(settings);
        DbHelper.RestoreDefaults();

        new DatabaseLogger();
        
        return true;
    }

    /// <summary>
    /// Clean the default temp directory on startup
    /// </summary>
    private static void CleanDefaultTempDirectory()
    {
        string tempDir = Docker ? Path.Combine(DirectoryHelper.DataDirectory, "temp") : Path.Combine(DirectoryHelper.BaseDirectory, "Temp");
        DirectoryHelper.CleanDirectory(tempDir);
    }

}
