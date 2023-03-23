using System.Text;
using FileFlows.Plugin;
using FileFlows.Server;
using FileFlows.ServerShared;
using FileFlows.ServerShared.Models;
using FileFlows.ServerShared.Helpers;
using FileFlows.ServerShared.Services;
using FileFlows.ServerShared.Workers;
using FileFlows.Shared.Models;
using Jint.Native.Json;

namespace FileFlows.Node.Workers;


/// <summary>
/// A flow worker executes a flow and start a flow runner
/// </summary>
public class FlowWorker : Worker
{
    /// <summary>
    /// A unique identifier to identify the flow worker
    /// This flow worker can have multiple executing processes so this do UID does
    /// not match the UI of an executor in the UI
    /// </summary>
    public readonly Guid Uid = Guid.NewGuid();

    private static readonly string ConfigKey = Environment.GetEnvironmentVariable("FF_ENCRYPT")?.EmptyAsNull() ?? Guid.NewGuid().ToString();

    #if(DEBUG)
    private static readonly bool ConfigNoEncrypt = true;
    #else
    private static readonly bool ConfigNoEncrypt = Environment.GetEnvironmentVariable("FF_NO_ENCRYPT") == "1";
    #endif

    /// <summary>
    /// The current configuration
    /// </summary>
    internal static int CurrentConfigurationRevision { get; private set; } = -1;

    /// <summary>
    /// The instance of the flow worker 
    /// </summary>
    private static FlowWorker? Instance;

    private readonly Mutex mutex = new Mutex();
    private readonly List<Guid> ExecutingRunners = new ();

    private const int DEFAULT_INTERVAL = 10;

    
    /// <summary>
    /// If this flow worker is running on the server or an external processing node
    /// </summary>
    private readonly bool isServer;

    private bool FirstExecute;

    /// <summary>
    /// The host name of the processing node
    /// </summary>
    private string Hostname { get; set; }

    
    /// <summary>
    /// Constructs a flow worker instance
    /// </summary>
    /// <param name="hostname">the host name of the processing node</param>
    /// <param name="isServer">if this flow worker is running on the server or an external processing node</param>
    public FlowWorker(string hostname, bool isServer = false) : base(ScheduleType.Second, DEFAULT_INTERVAL)
    {
        FlowWorker.Instance = this;
        this.isServer = isServer;
        this.FirstExecute = true;
        this.Hostname = hostname;
    }

    /// <summary>
    /// A function to check if this flow worker is enabled
    /// </summary>
    public Func<bool>? IsEnabledCheck { get; set; }

    /// <summary>
    /// Gets if there are any active runners
    /// </summary>
    public static bool HasActiveRunners => Instance?.ExecutingRunners?.Any() == true;

    /// <summary>
    /// Gets the number of active runners
    /// </summary>
    public static int ActiveRunners => Instance?.ExecutingRunners?.Count ?? 0;

    /// <summary>
    /// Executes the flow worker and start a flow runner if required
    /// </summary>
    protected override void Execute()
    {
        if (UpdaterWorker.UpdatePending)
            return;

        if (IsEnabledCheck?.Invoke() == false)
            return;
        var nodeService = NodeService.Load();
        ProcessingNode node;
        try
        {
            node = isServer ? nodeService.GetServerNodeAsync().Result : nodeService.GetByAddressAsync(this.Hostname).Result;
        }
        catch(Exception ex)
        {
            Logger.Instance?.ELog("Failed to register node: " + ex.Message);
            return;
        }

        if (FirstExecute)
        {
            FirstExecute = false;
            // tell the server to kill any flow executors from this node, in case this node was restarted
            nodeService.ClearWorkersAsync(node.Uid);
        }

        if (node == null)
        {
            Logger.Instance?.DLog($"Node not found");
            return;
        }

        if (UpdateConfiguration().Result == false)
            return;

        var settingsService = SettingsService.Load();
        var ffStatus = settingsService.GetFileFlowsStatus().Result;

        string nodeName = node?.Name == "FileFlowsServer" ? "Internal Processing Node" : (node?.Name ?? "Unknown");

        if (node?.Enabled != true)
        {
            Logger.Instance?.DLog($"Node '{nodeName}' is not enabled");
            return;
        }

        if(string.IsNullOrEmpty(node?.Schedule) == false && TimeHelper.InSchedule(node.Schedule) == false)
        {
            Logger.Instance?.DLog($"Node '{nodeName}' is out of schedule");
            Interval = 300; // slow interval down to 5minus
            return;
        }

        if (Interval == 300)
            Interval = DEFAULT_INTERVAL;

        if (node?.FlowRunners <= ExecutingRunners.Count)
        {
            Logger.Instance?.DLog($"At limit of running executors on '{nodeName}': " + node.FlowRunners);
            return; // already maximum executors running
        }


        string tempPath = node?.TempPath ?? string.Empty;
        if (string.IsNullOrEmpty(tempPath))
        {
            Logger.Instance?.ELog($"Temp Path not set on node '{nodeName}', cannot process");
            return;
        }
        
        if(Directory.Exists(tempPath) == false)
        {
            try
            {
                Directory.CreateDirectory(tempPath);
            }
            catch (Exception)
            {
                Logger.Instance?.ELog($"Temp Path does not exist on on node '{nodeName}', and failed to create it: {tempPath}");
                return;
            }
        }

        if (ffStatus?.Licensed == true && string.IsNullOrWhiteSpace(node.PreExecuteScript) == false)
        {
            if (PreExecuteScriptTest(node) == false)
                return;
        }
        
        var libFileService = LibraryFileService.Load();
        var libFileResult = libFileService.GetNext(node?.Name ?? string.Empty, node?.Uid ?? Guid.Empty,node?.Version ?? string.Empty, Uid).Result;
        if (libFileResult?.Status != NextLibraryFileStatus.Success)
        {
            Logger.Instance.ILog("No file found to process, status from server: " + (libFileResult?.Status.ToString() ?? "UNKNOWN"));
            return;
        }
        if (libFileResult?.File == null)
            return; // nothing to process
        var libFile = libFileResult.File;

        bool windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        Guid processUid = Guid.NewGuid();
        AddExecutingRunner(processUid);
        Task.Run(() =>
        {
            try
            {
#pragma warning disable CS8601 // Possible null reference assignment.
                var parameters = new string[]
                {
                    "--uid",
                    processUid.ToString(),
                    "--libfile",
                    libFile.Uid.ToString(),
                    "--tempPath",
                    tempPath,
                    "--cfgPath",
                    GetConfigurationDirectory(),
                    "--cfgKey",
                    ConfigNoEncrypt ? "NO_ENCRYPT" : ConfigKey,
                    "--baseUrl",
                    Service.ServiceBaseUrl,
                    Globals.IsDocker ? "--docker" : null,
                    isServer ? null : "--hostname",
                    isServer ? null : Hostname,
                    isServer ? "--server" : "--notserver"
                }.Where(x => x != null).ToArray();
#pragma warning restore CS8601 // Possible null reference assignment.

                try
                {
#if (DEBUG)
                    FileFlows.FlowRunner.Program.Main(parameters);
#else   
                    using Process process = new Process();
                
                    process.StartInfo = new ProcessStartInfo();
                    process.StartInfo.FileName = GetDotnetLocation();
                    process.StartInfo.WorkingDirectory = DirectoryHelper.FlowRunnerDirectory;
                    process.StartInfo.ArgumentList.Add("FileFlows.FlowRunner.dll");
                    foreach (var str in parameters)
                        process.StartInfo.ArgumentList.Add(str);

                    Logger.Instance?.ILog("Executing: " + process.StartInfo.FileName + " " + String.Join(" ", process.StartInfo.ArgumentList.Select(x => "\"" + x + "\"")));
                    Logger.Instance?.ILog("Working Directory: " + process.StartInfo.WorkingDirectory);

                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    StringBuilder completeLog = new StringBuilder();
                    if (string.IsNullOrEmpty(output) == false)
                    {
                        completeLog.AppendLine(
                            "==============================================================================" + Environment.NewLine +
                            "===                      PROCESSING NODE OUTPUT START                      ===" + Environment.NewLine +
                            "==============================================================================" + Environment.NewLine +
                            output + Environment.NewLine +
                            "==============================================================================" + Environment.NewLine +
                            "===                       PROCESSING NODE OUTPUT END                       ===" + Environment.NewLine +
                            "==============================================================================");
                    }
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    if (string.IsNullOrEmpty(error) == false)
                    {
                        completeLog.AppendLine(
                            "==============================================================================" + Environment.NewLine +
                            "===                   PROCESSING NODE ERROR OUTPUT START                   ===" + Environment.NewLine +
                            "==============================================================================" + Environment.NewLine +
                            error  + Environment.NewLine +
                            "==============================================================================" + Environment.NewLine +
                            "===                    PROCESSING NODE ERROR OUTPUT END                    ===" + Environment.NewLine +
                            "==============================================================================");
                    }

                    SaveLog(libFile, completeLog.ToString());

                    if (process.ExitCode != 0)
                    {
                        Logger.Instance?.ELog("Error executing runner: Invalid exit code: " + process.ExitCode);
                        libFile.Status = FileStatus.ProcessingFailed;
                        libFileService.Update(libFile);
                    }
                #endif
                }
                catch (Exception ex)
                {
                    Logger.Instance?.ELog("Error executing runner: " + ex.Message + Environment.NewLine + ex.StackTrace);
                    libFile.Status = FileStatus.ProcessingFailed;
                    libFileService.Update(libFile);
                }
            }
            finally
            {
                RemoveExecutingRunner(processUid);

                try
                {
                    string dir = Path.Combine(tempPath, "Runner-" + processUid.ToString());
                    if(Directory.Exists(dir))
                        Directory.Delete(dir, true);
                }
                catch (Exception ex)
                {
                    Logger.Instance?.WLog("Failed to clean up runner directory: " + ex.Message);
                }

                Trigger();
            }
        });
    }

    private bool PreExecuteScriptTest(ProcessingNode node)
    {
        var scriptService  = ScriptService.Load();
        string scriptDir = Path.Combine(GetConfigurationDirectory(), "Scripts");
        string sharedDir = Path.Combine(scriptDir, "Shared");
        string jsFile = Path.Combine(scriptDir, "System", node.PreExecuteScript + ".js");
        if (File.Exists(jsFile) == false)
        {
            jsFile = Path.Combine(GetConfigurationDirectory(), "Scripts", "Flow", node.PreExecuteScript + ".js");
            if (File.Exists(jsFile) == false)
            {
                jsFile = Path.Combine(sharedDir, node.PreExecuteScript + ".js");
                if (File.Exists(jsFile) == false)
                {
                    Logger.Instance.ELog("Failed to locate pre-execute script: " + node.PreExecuteScript);
                    return false;
                }
            }
        }

        Logger.Instance.ILog("Loading Pre-Execute Script: " + jsFile);
        string code = File.ReadAllText(jsFile);
        if (string.IsNullOrWhiteSpace(code))
        {
            Logger.Instance.ELog("Failed to load pre-execute script code");
            return false;
        }

        var variableService = new VariableService();
        var variables = variableService.GetAllAsync().Result?.ToDictionary(x => x.Name, x => (object)x.Value) ?? new ();
        if (variables.ContainsKey("FileFlows.Url"))
            variables["FileFlows.Url"] = Service.ServiceBaseUrl;
        else
            variables.Add("FileFlows.Url", Service.ServiceBaseUrl);
        var result = ScriptExecutor.Execute(code, variables, sharedDirectory: sharedDir);
        if (result.Success == false)
        {
            Logger.Instance.ELog("Pre-execute script failed: " + result.ReturnValue + "\n" + result.Log);
            return false;
        }
        Logger.Instance.ILog("Pre-Execute script returned: " + result.ReturnValue == null ? "" : System.Text.Json.JsonSerializer.Serialize(result.ReturnValue));
        

        if (result.ReturnValue?.ToString()?.ToLowerInvariant() == "exit")
        {
            Logger.Instance.ILog("Exiting");
            return false;
        }

        if (result.ReturnValue?.ToString()?.ToLowerInvariant() == "restart")
        {
            if (Globals.IsDocker == false)
            {
                Logger.Instance.WLog("Requested restart but not running inside docker");
                return false;
            }

            if (HasActiveRunners)
            {
                Logger.Instance.WLog("Requested restart, but there are executing runners");
                return false;
            }
            
            Logger.Instance.ILog("Restarting...");
            Thread.Sleep(5_000);
            Environment.Exit(0);
            return false;
        }
        
        if (result.ReturnValue as bool? == false)
        {
            Logger.Instance.ELog("Output from pre-execute script failed: " + result.ReturnValue + "\n" + result.Log);
            return false;
        }
        Logger.Instance.ILog("Pre-execute script passed: \n"+ result.Log.Replace("\\n", "\n"));
        return true;
    }

    private void StringBuilderLog(StringBuilder builder, LogType type, params object[] args)
    {
        string typeString = type switch
        {
            LogType.Debug => "[DBUG] ",
            LogType.Info => "[INFO] ",
            LogType.Warning => "[WARN] ",
            LogType.Error => "[ERRR] ",
            _ => "",
        };
        string message = typeString + string.Join(", ", args.Select(x =>
            x == null ? "null" :
            x.GetType().IsPrimitive ? x.ToString() :
            x is string ? x.ToString() :
            System.Text.Json.JsonSerializer.Serialize(x)));
        builder.AppendLine(message);
    }

    /// <summary>
    /// Adds an executing runner to the list of currently running flow runners
    /// </summary>
    /// <param name="uid">The uid of the flow runner to add</param>
    private void AddExecutingRunner(Guid uid)
    {
        mutex.WaitOne();
        try
        {
            ExecutingRunners.Add(uid);
        }
        finally
        {
            mutex.ReleaseMutex();   
        }
    }

    
    /// <summary>
    /// Removes a flow runner from the list of currently executing flow runners
    /// </summary>
    /// <param name="uid">The uid of the flow runner to remove</param>
    private void RemoveExecutingRunner(Guid uid)
    {
        Logger.Instance?.ILog($"Removing executing runner[{ExecutingRunners.Count}]: {uid}");
        mutex.WaitOne();
        try
        {
            if (ExecutingRunners.Contains(uid))
                ExecutingRunners.Remove(uid);
            else
            {
                Logger.Instance?.ILog("Executing runner not in list: " + uid +" => " + string.Join(",", ExecutingRunners.Select(x => x.ToString())));
            }
            Logger.Instance?.ILog("Runner count: " + ExecutingRunners.Count);
        }
        catch(Exception ex)
        {
            Logger.Instance?.ELog("Failed to remove executing runner: " + ex.Message + Environment.NewLine + ex.StackTrace);    
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }

    /// <summary>
    /// Saves the flow runner log to the server
    /// </summary>
    /// <param name="libFile">The Library File that was processed</param>
    /// <param name="log">The full flow runner log</param>
    private void SaveLog(LibraryFile libFile, string log)
    { 
        var service = new LibraryFileService();
        bool saved = service.SaveFullLog(libFile.Uid, log).Result;
        if (!saved)
        {
            // save to main output
            log = string.Join('\n', log.Split('\n').Select(x => "       " + x).ToArray());
            Logger.Instance?.DLog(Environment.NewLine + log);
        }
    }


    /// <summary>
    /// The location of dotnet
    /// </summary>
    private static string Dotnet = "";
    
    /// <summary>
    /// Gets the location of dotnet to use to start the flow runner
    /// </summary>
    /// <returns>the location of dotnet to use to start the flow runner</returns>
    private string GetDotnetLocation()
    {
        if(string.IsNullOrEmpty(Dotnet))
        {
            if (Globals.IsWindows == false && File.Exists("/dotnet/dotnet"))
                Dotnet = "/dotnet/dotnet"; // location of docker
            else if (Globals.IsWindows == false && File.Exists("/root/.dotnet/dotnet"))
                Dotnet = "/root/.dotnet/dotnet"; // location of legacy docker
            else
                Dotnet = "dotnet";// assume in PATH
        }
        return Dotnet;
    }

    private string GetConfigurationDirectory(int? configVersion = null) =>
        Path.Combine(DirectoryHelper.ConfigDirectory, (configVersion ?? CurrentConfigurationRevision).ToString());

    private bool GetCurrentConfigEncrypted()
    {
        string cfgFile = Path.Combine(GetConfigurationDirectory(), "config.json");
        if (File.Exists(cfgFile) == false)
            return false;
        string content = File.ReadAllText(cfgFile);
        if (string.IsNullOrWhiteSpace(content))
            return false;
        return content.Contains("Revision") == false;
    }
    
    /// <summary>
    /// Ensures the local configuration is current with the server
    /// </summary>
    /// <returns>an awaited task</returns>
    private async Task<bool> UpdateConfiguration()
    {
        var service = new SettingsService();
        int revision = await service.GetCurrentConfigurationRevision();
        if (revision == -1)
        {
            Logger.Instance.ELog("Failed to get current configuration revision from server");
            return false;
        }


        if (revision == CurrentConfigurationRevision)
            return true;

        var config = await service.GetCurrentConfiguration();
        if (config == null)
        {
            Logger.Instance.ELog("Failed downloading latest configuration from server");
            return false;
        }

        string dir = GetConfigurationDirectory(revision);
        try
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
            
            Directory.CreateDirectory(dir);
            Directory.CreateDirectory(Path.Combine(dir, "Scripts"));
            Directory.CreateDirectory(Path.Combine(dir, "Scripts", "Shared"));
            Directory.CreateDirectory(Path.Combine(dir, "Scripts", "Flow"));
            Directory.CreateDirectory(Path.Combine(dir, "Scripts", "System"));
            Directory.CreateDirectory(Path.Combine(dir, "Plugins"));
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog($"Failed recreating configuration directory '{dir}': {ex.Message}");
            return false;
        }

        foreach (var script in config.FlowScripts)
            await System.IO.File.WriteAllTextAsync(Path.Combine(dir, "Scripts", "Flow", script.Name + ".js"), script.Code);
        foreach (var script in config.SystemScripts)
            await System.IO.File.WriteAllTextAsync(Path.Combine(dir, "Scripts", "System", script.Name + ".js"), script.Code);
        foreach (var script in config.SharedScripts)
            await System.IO.File.WriteAllTextAsync(Path.Combine(dir, "Scripts", "Shared", script.Name + ".js"), script.Code);
        

        bool windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        bool macOs = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        bool is64bit = IntPtr.Size == 8;
        foreach (var plugin in config.Plugins ?? new Dictionary<string, byte[]>())
        {
            var zip = Path.Combine(dir, plugin.Key + ".zip");
            await File.WriteAllBytesAsync(zip, plugin.Value);
            string destDir = Path.Combine(dir, "Plugins", plugin.Key);
            Directory.CreateDirectory(destDir);
            System.IO.Compression.ZipFile.ExtractToDirectory(zip, destDir);
            File.Delete(zip);
            
            // check if there are runtime specific files that need to be moved
            foreach (string rdir in windows ? new[] { "win", "win-" + (is64bit ? "x64" : "x86") } : macOs ? new[] { "osx-x64" } : new string[] { "linux-x64", "linux" })
            {
                var runtimeDir = new DirectoryInfo(Path.Combine(destDir, "runtimes", rdir));
                Logger.Instance?.ILog("Searching for runtime directory: " + runtimeDir.FullName);
                if (runtimeDir.Exists)
                {
                    foreach (var rfile in runtimeDir.GetFiles("*.*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            if (Regex.IsMatch(rfile.Name, @"\.(dll|so)$") == false)
                                continue;

                            Logger.Instance?.ILog("Trying to move file: \"" + rfile.FullName + "\" to \"" + destDir + "\"");
                            rfile.MoveTo(Path.Combine(destDir, rfile.Name));
                            Logger.Instance?.ILog("Moved file: \"" + rfile.FullName + "\" to \"" + destDir + "\"");
                        }
                        catch (Exception ex)
                        {
                            Logger.Instance?.ILog("Failed to move file: " + ex.Message);
                        }
                    }
                }
            }
        }

        string json = System.Text.Json.JsonSerializer.Serialize(new
        {
            config.Revision,
            config.MaxNodes,
            config.Variables,
            config.Libraries,
            config.PluginSettings,
            config.Flows,
            config.FlowScripts,
            config.SharedScripts,
            config.SystemScripts
        });

        string cfgFile = Path.Combine(dir, "config.json");
        if (ConfigNoEncrypt)
        {
            Logger.Instance?.DLog("Configuration set to no encryption, saving as plain text");
            await File.WriteAllTextAsync(cfgFile, json);
        }
        else
        {
            Logger.Instance?.DLog("Saving encrypted configuration");
            Utils.ConfigEncrypter.Save(json, ConfigKey, cfgFile);
        }

        CurrentConfigurationRevision = revision;

        return true;

    }

}
