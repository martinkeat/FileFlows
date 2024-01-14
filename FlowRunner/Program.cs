using System.Globalization;
using FileFlows.Plugin;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;
using System.Net;
using Esprima.Ast;
using FileFlows.Plugin.Services;
using FileFlows.ServerShared;
using FileFlows.ServerShared.FileServices;

namespace FileFlows.FlowRunner;

/// <summary>
/// Flow Runner
/// </summary>
public class Program
{
    /// <summary>
    /// Gets the runner instance UID
    /// </summary>
    public static Guid Uid { get; private set; }

    public static FlowLogger Logger;

    /// <summary>
    /// Main entry point for the flow runner
    /// </summary>
    /// <param name="args">the command line arguments</param>
    public static void Main(string[] args)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

        int exitCode = Run(args);
        LogInfo("Exit Code: " + exitCode);
        Environment.ExitCode = exitCode;
    }
    
    
    /// <summary>
    /// Runs the runner
    /// </summary>
    /// <param name="args">the args for the runner</param>
    /// <returns>the exit code of the runner</returns>
    public static int Run(string[] args)
    {
        Logger = new (null);
        LogInfo("Flow Runner Version: " + Globals.Version);
        ServicePointManager.DefaultConnectionLimit = 50;
        try
        {
            args ??= new string[] { };
            bool server = args.Any(x => x.ToLower() == "--server");

            string uid = GetArgument(args, "--uid");
            if (string.IsNullOrEmpty(uid))
                throw new Exception("uid not set.");
            Uid = Guid.Parse(uid);

            string tempPath = GetArgument(args, "--tempPath");
            if (string.IsNullOrEmpty(tempPath) || Directory.Exists(tempPath) == false)
                throw new Exception("Temp path doesnt exist: " + tempPath);
            LogInfo("Temp Path: " + tempPath);
            
            string cfgPath = GetArgument(args, "--cfgPath");
            if (string.IsNullOrEmpty(cfgPath) || Directory.Exists(cfgPath) == false)
                throw new Exception("Configuration Path doesnt exist: " + cfgPath);

            string cfgFile = Path.Combine(cfgPath, "config.json");
            if(File.Exists(cfgFile) == false)
                throw new Exception("Configuration file doesnt exist: " + cfgFile);


            string cfgKey = GetArgument(args, "--cfgKey");
            if (string.IsNullOrEmpty(cfgKey))
                throw new Exception("Configuration Key not set");
            bool noEncrypt = cfgKey == "NO_ENCRYPT";
            string cfgJson;
            if (noEncrypt)
            {
                LogInfo("No Encryption for Node configuration");
                cfgJson = File.ReadAllText(cfgFile);
            }
            else
            {
                LogInfo("Using configuration encryption key: " + cfgKey);
                cfgJson = ConfigDecrypter.DecryptConfig(cfgFile, cfgKey);
            }

            var config = JsonSerializer.Deserialize<ConfigurationRevision>(cfgJson);

            string baseUrl = GetArgument(args, "--baseUrl");
            if (string.IsNullOrEmpty(baseUrl))
                throw new Exception("baseUrl not set");
            LogInfo("Base URL: " + baseUrl);
            Service.ServiceBaseUrl = baseUrl;

            string hostname = GetArgument(args, "--hostname");
            if(string.IsNullOrWhiteSpace(hostname))
                hostname = Environment.MachineName;

            Globals.IsDocker = args.Contains("--docker");
            LogInfo("Docker: " + Globals.IsDocker);

            string workingDir = Path.Combine(tempPath, "Runner-" + uid);
            LogInfo("Working Directory: " + workingDir);
            try
            {
                Directory.CreateDirectory(workingDir);
            }
            catch (Exception ex) when (Globals.IsDocker)
            {
                // this can throw if mapping inside a docker container is not valid, or the mapped location has become unavailable
                LogError("==========================================================================================");
                LogError("Failed to create working directory, this is likely caused by the mapped '/temp' directory is missing or has become unavailable from the host machine");
                LogError(ex.Message);
                LogError("==========================================================================================");
                return 1;
            }

            LogInfo("Created Directory: " + workingDir);

            var libfileUid = Guid.Parse(GetArgument(args, "--libfile"));
            HttpHelper.Client = HttpHelper.GetDefaultHttpHelper(Service.ServiceBaseUrl);
            var result = Execute(new()
            {
                IsServer = server,
                Config = config,
                ConfigDirectory = cfgPath,
                TempDirectory = tempPath,
                LibraryFileUid = libfileUid,
                WorkingDirectory = workingDir,
                Hostname = hostname
            });
            return result == null ? -2 : result == FileStatus.Processed ? 0 : -1;
        }
        catch (Exception ex)
        {
            LogInfo("Error: " + ex.Message + Environment.NewLine + ex.StackTrace);
            while(ex.InnerException != null)
            {
                LogInfo("Error: " + ex.Message + Environment.NewLine + ex.StackTrace);
                ex = ex.InnerException;
            }
            return 1;
        }
    }

    static string GetArgument(string[] args, string name)
    {
        int index = args.Select(x => x.ToLower()).ToList().IndexOf(name.ToLower());
        if (index < 0)
            return string.Empty;
        if (index >= args.Length - 1)
            return string.Empty;
        return args[index + 1];
    }


    /// <summary>
    /// Executes the runner
    /// </summary>
    /// <param name="args">the args</param>
    /// <returns>the library file status, or null if library file was not loaded</returns>
    /// <exception cref="Exception">error was thrown</exception>
    static FileStatus? Execute(ExecuteArgs args)
    {
        ProcessingNode node;
        var nodeService = NodeService.Load();
        try
        {
            string address = args.IsServer ? "INTERNAL_NODE" : args.Hostname;
            LogInfo("Address: "+ address);
            node = nodeService.GetByAddressAsync(address).Result;
            if (node == null)
                throw new Exception("Failed to load node!!!!");
            LogInfo("Node SignalrUrl: " + node.SignalrUrl);
        }
        catch (Exception ex)
        {
            LogInfo("Failed to register node: " + ex.Message + Environment.NewLine + ex.StackTrace);
            throw;
        }

        if ((node.Address == "FileFlowsServer" || node.SignalrUrl == "flow") && string.IsNullOrEmpty(Service.ServiceBaseUrl) == false)
            FlowRunnerCommunicator.SignalrUrl = Service.ServiceBaseUrl.EndsWith("/")
                ? Service.ServiceBaseUrl + "flow"
                : Service.ServiceBaseUrl + "/flow";
        else
            FlowRunnerCommunicator.SignalrUrl = node.SignalrUrl;

        var libFileService = LibraryFileService.Load();
        var libFile = libFileService.Get(args.LibraryFileUid).Result;
        if (libFile == null)
        {
            LogInfo("Library file not found, must have been deleted from the library files.  Nothing to process");
            return null; // nothing to process
        }

        // string workingFile = node.Map(libFile.Name);
        string workingFile = libFile.Name;

        var libfileService = LibraryFileService.Load();
        var lib = args.Config.Libraries.FirstOrDefault(x => x.Uid == libFile.Library.Uid);
        if (lib == null)
        {
            LogInfo("Library was not found, deleting library file");
            libfileService.Delete(libFile.Uid).Wait();
            return null;
        }

        FileSystemInfo file = lib.Folders ? new DirectoryInfo(workingFile) : new FileInfo(workingFile);
        bool fileExists = file.Exists; // set to variable so we can set this to false in debugging easily
        bool remoteFile = false;
        IFileService _fileService;
        if (fileExists)
        {
            _fileService = args.IsServer ? new LocalFileService() : new MappedFileService(node);
        }
        else if (args.IsServer || libfileService.ExistsOnServer(libFile.Uid).Result == false)
        {
            // doesnt exist
            LogInfo("Library file does not exist, deleting from library files: " + file.FullName);
            libfileService.Delete(libFile.Uid).Wait();
            return libFile.Status;
        }
        else
        {
            _fileService = new MappedFileService(node);
            bool exists = lib.Folders
                ? _fileService.DirectoryExists(workingFile)
                : _fileService.FileExists(workingFile);
            
            if (exists == false)
            {
                // need to try a remote
                if (args.Config.AllowRemote == false)
                {
                    LogError("Library file exists but is not accessible from node: " + file.FullName);
                    libFile.Status = FileStatus.MappingIssue;
                    libFile.ExecutedNodes = new List<ExecutedNode>();
                    libfileService.Update(libFile).Wait();
                    return libFile.Status;
                }

                if (lib.Folders)
                {
                    LogError("Library folder exists, but remote file server is not available for folders: " + file.FullName);
                    libFile.Status = FileStatus.MappingIssue;
                    libFile.ExecutedNodes = new List<ExecutedNode>();
                    libfileService.Update(libFile).Wait();
                    return libFile.Status;
                }
            
                remoteFile = true;
                _fileService = new RemoteFileService(Uid, Service.ServiceBaseUrl, args.WorkingDirectory, Logger,
                    libFile.Name.Contains('/') ? '/' : '\\');
            }
        }

        FileService.Instance = _fileService;

        var flow = args.Config.Flows.FirstOrDefault(x => x.Uid == (lib.Flow?.Uid ?? Guid.Empty));
        if (flow == null || flow.Uid == Guid.Empty)
        {
            LogInfo("Flow not found, cannot process file: " + file.FullName);
            libFile.Status = FileStatus.FlowNotFound;
            libfileService.Update(libFile).Wait();
            return libFile.Status;
        }

        libFile.Status = FileStatus.Processing;
        
        // update the library file to reference the updated flow (if changed)
        if (libFile.Flow?.Name != flow.Name || libFile.Flow?.Uid != flow.Uid)
        {
            libFile.Flow = new ObjectReference
            {
                Uid = flow.Uid,
                Name = flow.Name,
                Type = typeof(Flow)?.FullName ?? String.Empty
            };
            libfileService.Update(libFile).Wait();
        }

        libFile.ProcessingStarted = DateTime.Now;
        libfileService.Update(libFile).Wait();

        var info = new FlowExecutorInfo
        {
            Uid = Program.Uid,
            Config = args.Config,
            ConfigDirectory = args.ConfigDirectory,
            LibraryFile = libFile,
            //Log = String.Empty,
            NodeUid = node.Uid,
            NodeName = node.Name,
            RelativeFile = libFile.RelativePath,
            Library = libFile.Library,
            IsRemote = remoteFile,
            TotalParts = flow.Parts.Count,
            CurrentPart = 0,
            CurrentPartPercent = 0,
            CurrentPartName = string.Empty,
            StartedAt = DateTime.Now,
            WorkingFile = workingFile,
            IsDirectory = lib.Folders,
            LibraryPath = lib.Path, 
            Fingerprint = lib.UseFingerprinting,
            InitialSize = lib.Folders ? GetDirectorySize(workingFile) : FileService.Instance.FileSize(workingFile).ValueOrDefault
        };
        LogInfo("Start Working File: " + info.WorkingFile);
        info.LibraryFile.OriginalSize = info.InitialSize;
        LogInfo("Initial Size: " + info.InitialSize);
        LogInfo("File Service:"  + _fileService.GetType().Name);
        

        var runner = new Runner(info, flow, node, args.WorkingDirectory);
        runner.Run(Logger);
        return libFile.Status;
    }

    private static long GetDirectorySize(string path)
    {
        try
        {
            DirectoryInfo dir = new DirectoryInfo(path);
            return dir.EnumerateFiles("*.*", SearchOption.AllDirectories).Sum(x => x.Length);
        }
        catch (Exception ex)
        {
            LogInfo("Failed retrieving directory size: " + ex.Message);
            return 0;
        }
    }


    internal static void LogInfo(string message)
    {
        if(Logger != null)
            Logger.ILog(message);
        else
            Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " [INFO] -> " + message);
    }
    internal static void LogWarning(string message)
    {
        if(Logger != null)
            Logger.WLog(message);
        else
            Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " [WARN] -> " + message);
    }

    internal static void LogError(string message)
    {
        if (Logger != null)
            Logger.ELog(message);
        else
            Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " [ERRR] -> " + message);
    }
}