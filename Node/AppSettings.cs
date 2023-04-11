using System.Text.Json;
using FileFlows.ServerShared;
using FileFlows.ServerShared.Helpers;
using FileFlows.ServerShared.Models;

namespace FileFlows.Node;

/// <summary>
/// The Application Settings for the Node
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Gets or sets a forced URL to the server
    /// </summary>
    public static string? ForcedServerUrl { get; set; }
    /// <summary>
    /// Gets or sets a forced temporary path
    /// </summary>
    public static string? ForcedTempPath { get; set; }
    /// <summary>
    /// Gets or sets a forced hostname to identify this node as
    /// </summary>
    public static string? ForcedHostName { get; set; }

    /// <summary>
    /// Gets or sets mappings passed in via enviromental values
    /// </summary>
    public static List<RegisterModelMapping> EnvironmentalMappings { get; set; }
    
    /// <summary>
    /// Gets or sets the runner count defined by environmental settings
    /// </summary>
    public static int? EnvironmentalRunnerCount { get; set; }

    /// <summary>
    /// Gets or sets if the node should be enabled when registered
    /// </summary>
    public static bool? EnvironmentalEnabled { get; set; }

    private string _ServerUrl = string.Empty;
    private string _TempPath = string.Empty;
    /// <summary>
    /// Gets or sets the URL to the server
    /// </summary>
    public string ServerUrl
    {
        get
        {
            if (string.IsNullOrEmpty(ForcedServerUrl) == false)
                return ForcedServerUrl;
            return _ServerUrl;
        }
        set
        {
            _ServerUrl = value ?? String.Empty;
        }
    }

    /// <summary>
    /// Gets or sets the temporary path
    /// </summary>
    public string TempPath
    {
        get
        {
            if (string.IsNullOrEmpty(ForcedTempPath) == false)
                return ForcedTempPath;
            return _TempPath;
        }
        set
        {
            _TempPath = value ?? String.Empty;
        }
    }

    /// <summary>
    /// Gets the hostname of this node
    /// </summary>
    public string HostName
    {
        get
        {
            if (string.IsNullOrEmpty(ForcedHostName) == false)
                return ForcedHostName;
            return Environment.MachineName;
        }
    }

    /// <summary>
    /// Gets or sets the number of runners this node can execute
    /// </summary>
    public int Runners { get; set; }
    
    /// <summary>
    /// Gets or sets if this node is enabled
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Saves the configuration
    /// </summary>
    public void Save()
    {
        Save(this);
    }

    /// <summary>
    /// Initializes the application settings
    /// </summary>
    public static void Init()
    {
        Instance = Load();
    }

    /// <summary>
    /// Gets or sets the instance of the AppSettings
    /// </summary>
    public static AppSettings Instance { get; set; } = new AppSettings();

    /// <summary>
    /// Saves the application settings
    /// <param name="settings">the application settings to save</param>
    /// </summary>
    public static void Save(AppSettings settings)
    {
        if (settings == null)
            return;
        Instance = settings;
        string json = System.Text.Json.JsonSerializer.Serialize(settings, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(DirectoryHelper.NodeConfigFile, json);
    }
    
    /// <summary>
    /// Loads the application settings
    /// </summary>
    /// <returns>the loaded application settings</returns>
    public static AppSettings Load()
    {
        string file = DirectoryHelper.NodeConfigFile;
        if (File.Exists(file) == false)
        {
            AppSettings settings = new();
            settings.Runners = 1;
            settings.TempPath = Globals.IsDocker ? "/temp" :  Path.Combine(DirectoryHelper.BaseDirectory, "Temp");
            settings.Save();
            return settings;
        }
        try
        {
            string json = File.ReadAllText(file);
            var settings = JsonSerializer.Deserialize<AppSettings>(json);
            return settings ?? new ();
        }
        catch (Exception) { }
        return new();
    }

    /// <summary>
    /// Checks if the node is configured
    /// </summary>
    /// <returns>true if configured, otherwise false</returns>
    public static bool IsConfigured()
    {
        return string.IsNullOrWhiteSpace(Load().ServerUrl) == false;
    }
}
