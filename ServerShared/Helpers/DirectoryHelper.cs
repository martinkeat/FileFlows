using System.Text.RegularExpressions;

namespace FileFlows.ServerShared.Helpers;

/// <summary>
/// A helper class to manage the directories used by FileFlows Server and Node
/// </summary>
public class DirectoryHelper
{
    /// <summary>
    /// Initializes the Directory Helper
    /// </summary>
    public static void Init()
    {
        InitLoggingDirectory();
        InitDataDirectory();
        InitPluginsDirectory();
        InitTemplatesDirectory();

        FlowRunnerDirectory = Path.Combine(BaseDirectory, "FlowRunner");
    }

    private static void InitPluginsDirectory()
    {
        #if(DEBUG)
        return;
        #else
        string dir = PluginsDirectory;
        if (Directory.Exists(dir) == false)
            Directory.CreateDirectory(dir);

        string oldDir = Path.Combine(BaseDirectory, Globals.IsNode ? "Node" : "Server", "Plugins");
        if (Directory.Exists(oldDir) == false)
            return;
        MoveDirectoryContent(oldDir, dir);
        #endif
    }

    private static void InitTemplatesDirectory()
    {
#if(DEBUG && false)
        return;
#else
        foreach (var dir in new[] { TemplateDirectory, TemplateDirectoryFlow, TemplateDirectoryLibrary })
        {
            if (Directory.Exists(dir) == false)
                Directory.CreateDirectory(dir);
        }
#endif
        
    }
    private static string _BaseDirectory = null!;

    /// <summary>
    /// Gets or sets the base directory of FileFlows
    /// eg %appdata%\FileFlows
    /// </summary>
    public static string BaseDirectory
    {
        get
        {
            if (string.IsNullOrEmpty(_BaseDirectory))
            {
                var dllDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrEmpty(dllDir))
                    throw new Exception("Failed to find DLL directory");
                _BaseDirectory = new DirectoryInfo(dllDir).Parent?.FullName ?? string.Empty;
            }
            return _BaseDirectory;
        }
        set =>  _BaseDirectory = value;
    }
    
    private static string ExecutingDirectory => 
        Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!;


    /// <summary>
    /// Inits the logging directory and moves any files if they need to be moved
    /// </summary>
    private static void InitLoggingDirectory()
    {
        string dir = Path.Combine(BaseDirectory, "Logs");
        LibraryFilesLoggingDirectory = Path.Combine(dir, "LibraryFiles");
        if (Directory.Exists(dir) == false)
            Directory.CreateDirectory(dir);
        if(Directory.Exists(LibraryFilesLoggingDirectory) == false)
            Directory.CreateDirectory(LibraryFilesLoggingDirectory);
        
        
        // look for logs from other directories
        string localLogs = Path.Combine(ExecutingDirectory, "Logs");
        if(localLogs != dir && Directory.Exists(localLogs))
            MoveDirectoryContent(localLogs, dir);
        
        // move library file log files if needed
        var di = new DirectoryInfo(dir);
        var files = di.GetFiles("*.log").Union(di.GetFiles("*.html"));
        foreach (var file in files)
        {
            if (Regex.IsMatch(file.Name, @"^[a-fA-F0-9\-]{36}\.(log|html)$"))
            {
                var destLogFile = Path.Combine(LibraryFilesLoggingDirectory, file.Name);
                if (file.FullName == destLogFile)
                    continue; // shouldn't happen
                file.MoveTo(destLogFile, true);
                Shared.Logger.Instance?.ILog("Moved library file log file: " + destLogFile);
            }
        }
        
        LoggingDirectory = dir;
    }

    /// <summary>
    /// Inits the data directory and moves any files if they need to be moved
    /// </summary>
    private static void InitDataDirectory()
    {
        string dir = Path.Combine(BaseDirectory, "Data");
        if (Directory.Exists(dir) == false)
            Directory.CreateDirectory(dir);
        DataDirectory = dir;
        
        // look for logs from other directories
        string localData = Path.Combine(Directory.GetCurrentDirectory(), "Data");
        if(localData != dir && Directory.Exists(localData))
            MoveDirectoryContent(localData, dir);

        const string encryptKey = "encryptionkey.txt";
        EncryptionKeyFile = Path.Combine(dir, encryptKey);
        if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), encryptKey)))
            File.Move(Path.Combine(Directory.GetCurrentDirectory(), encryptKey), EncryptionKeyFile);
        
        const string nodeConfig = "node.config";
        NodeConfigFile = Path.Combine(dir, nodeConfig);
        if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "fileflows.config")))
            File.Move(Path.Combine(Directory.GetCurrentDirectory(), "fileflows.config"), NodeConfigFile);
        
        ServerConfigFile = Path.Combine(dir, "server.config");

        DatabaseDirectory = Globals.IsDocker == false ? dir : Path.Combine(dir, "Data");
        if (Directory.Exists(DatabaseDirectory) == false)
            Directory.CreateDirectory(DatabaseDirectory);
        
        ConfigDirectory = Path.Combine(Globals.IsDocker == false ? dir : Path.Combine(dir, "Data"), "Config");
        if (Directory.Exists(ConfigDirectory) == false)
            Directory.CreateDirectory(ConfigDirectory);
    }

    /// <summary>
    /// Gets the logging directory
    /// </summary>
    public static string LoggingDirectory { get; private set; } = null!;
    
    /// <summary>
    /// Gets the directory where library file logs are stored 
    /// </summary>
    public static string LibraryFilesLoggingDirectory { get; private set; } = null!;

    /// <summary>
    /// Gets the data directory
    /// </summary>
    public static string DataDirectory { get; private set; } = null!;
    
    /// <summary>
    /// Gets the directory containing the cached configurations
    /// </summary>
    public static string ConfigDirectory { get; private set; } = null!;
    
    /// <summary>
    /// Gets the directory the database is saved in
    /// </summary>
    public static string DatabaseDirectory { get; private set; } = null!;

    /// <summary>
    /// Gets the flow runner directory
    /// </summary>
    public static string FlowRunnerDirectory { get; private set; } = null!;

    /// <summary>
    /// Gets the logging directory
    /// </summary>
    public static string PluginsDirectory
    {
        get
        {
            #if(DEBUG)
            return "Plugins";
            #else
            // docker we expose this in the data directory so we
            // reduce how many things we have to map out
            if (Globals.IsDocker) 
                return Path.Combine(DataDirectory, "Plugins");
            return Path.Combine(BaseDirectory, "Plugins");
            #endif
        }
    }
    /// <summary>
    /// Gets the templates directory
    /// </summary>
    public static string TemplateDirectory
    {
        get
        {
            // docker we expose this in the data directory so we
            // reduce how many things we have to map out
            if (Globals.IsDocker) 
                return Path.Combine(DataDirectory, "Templates");
            return Path.Combine(BaseDirectory, "Templates");
        }
    }
    /// <summary>
    /// Gets the directory for flow templates
    /// </summary>
    public static string TemplateDirectoryFlow => Path.Combine(TemplateDirectory, "Flow");
    /// <summary>
    /// Gets the directory for library templates
    /// </summary>
    public static string TemplateDirectoryLibrary => Path.Combine(TemplateDirectory, "Library");

    /// <summary>
    /// Gets the DockerMods directory
    /// </summary>
    public static string DockerModsDirectory => "/app/DockerMods"; // this directory will not be mapped out, so will be cleaned when new DockerImage is pulled
    
    // /// <summary>
    // /// Gets the scripts directory
    // /// </summary>
    // public static string ScriptsDirectory
    // {
    //     get
    //     {
    //         // docker we expose this in the data directory so we
    //         // reduce how many things we have to map out
    //         if (Globals.IsDocker) 
    //             return Path.Combine(DataDirectory, "Scripts");
    //         return Path.Combine(BaseDirectory, "Scripts");
    //     }
    // }
    
    // /// <summary>
    // /// Gets the scripts directory for flow scripts
    // /// </summary>
    // public static string ScriptsDirectoryFlow => Path.Combine(ScriptsDirectory, "Flow");
    //
    // /// <summary>
    // /// Gets the scripts directory for system scripts
    // /// </summary>
    // public static string ScriptsDirectorySystem => Path.Combine(ScriptsDirectory, "System");
    //
    // /// <summary>
    // /// Gets the scripts directory for scripts from the repository
    // /// </summary>
    // public static string ScriptsDirectoryShared => Path.Combine(ScriptsDirectory, "Shared");
    //
    // /// <summary>
    // /// Gets the scripts directory for webhook scripts
    // /// </summary>
    // public static string ScriptsDirectoryWebhook => Path.Combine(ScriptsDirectory, "Webhook");
    
    /// <summary>
    /// Gets the scripts directory for template scripts
    /// </summary>
    public static string ScriptsDirectoryFunction => Path.Combine(TemplateDirectory, "Function");
    
    /// <summary>
    /// Gets the location of the encryption key file
    /// </summary>
    public static string EncryptionKeyFile { get;private set; } = null!;

    /// <summary>
    /// Gets the location of the node configuration file
    /// </summary>
    public static string NodeConfigFile { get; private set; } = null!;


    /// <summary>
    /// Gets the location of the server configuration file
    /// </summary>
    public static string ServerConfigFile { get; private set; } = null!;
    
    private static void MoveDirectoryContent(string source, string destination)
    {
        if(Directory.Exists(destination) == false)
            Directory.CreateDirectory(destination);

        if(Directory.Exists(source) == false)
            return;

        var diSource = new DirectoryInfo(source);
        foreach(var dir in diSource.GetDirectories())
        {
            MoveDirectoryContent(dir.FullName, Path.Combine(destination, dir.Name));
        }

        foreach(var file in diSource.GetFiles())
        {
            try
            {
                file.MoveTo(Path.Combine(destination, file.Name));
            }
            catch(Exception) { }
        }

        try
        {
           diSource.Delete(true); 
        }
        catch(Exception) { }
    }


    /// <summary>
    /// Deletes all files and folders from a directory
    /// </summary>
    /// <param name="path">the path of the directory</param>
    public static void CleanDirectory(string path)
    {
        var dirInfo = new DirectoryInfo(path);
        if (dirInfo.Exists == false)
            return;
        try
        {
            var subDirs = dirInfo.GetDirectories();
            foreach (var sub in subDirs)
            {
                try
                {
                    sub.Delete(true);
                }
                catch
                {
                }
            }

            var files = dirInfo.GetFiles();
            foreach (var file in files)
            {
                try
                {
                    file.Delete();
                }
                catch
                {
                }
            }
        } 
        catch { }
    }
    
    /// <summary>
    /// Copies a directory and all its contents
    /// </summary>
    /// <param name="sourceDir">the source directory</param>
    /// <param name="destinationDir">the destination directory</param>
    /// <param name="recursive">if all subdirectories should be copied</param>
    /// <exception cref="DirectoryNotFoundException">throw in the source directory does not exist</exception>
    public static void CopyDirectory(string sourceDir, string destinationDir, bool recursive = true)
    {
        // Get information about the source directory
        var dir = new DirectoryInfo(sourceDir);

        // Check if the source directory exists
        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        // Cache directories before we start copying
        DirectoryInfo[] dirs = dir.GetDirectories();

        // Create the destination directory
        Directory.CreateDirectory(destinationDir);

        // Get the files in the source directory and copy to the destination directory
        foreach (FileInfo file in dir.GetFiles())
        {
            string targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath, true);
        }

        // If recursive and copying subdirectories, recursively call this method
        if (recursive)
        {
            foreach (DirectoryInfo subDir in dirs)
            {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir, true);
            }
        }
    }

    /// <summary>
    /// Gets the home directory, this will be used as the default location when adding flows/libraries etc
    /// </summary>
    /// <returns>the users home directory</returns>
    public static string GetUsersHomeDirectory()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)?.EmptyAsNull() ?? Environment.GetEnvironmentVariable("HOME");
        if (string.IsNullOrWhiteSpace(home) == false)
            return home;
        if(OperatingSystem.IsWindows() == false && Directory.Exists("/media"))
            return "/media";
        
        return OperatingSystem.IsWindows() ? "C:\\" : "/";
    }
}