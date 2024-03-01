using System.Text.Json.Serialization;

namespace FileFlows.Server;

/// <summary>
/// Application settings that are saved to the appsettings.json file 
/// </summary>
internal class AppSettings
{
    /// <summary>
    /// Gets or sets the database connection to use
    /// </summary>
    public string DatabaseConnection { get; set; }

    /// <summary>
    /// Gets or sets if the database should recreate if it already exists
    /// </summary>
    public bool RecreateDatabase{ get; set; }

    /// <summary>
    /// Gets or sets the encryption key to use
    /// </summary>
    public string EncryptionKey { get; set; }

    /// <summary>
    /// Gets or sets the license email
    /// </summary>
    public string LicenseEmail { get; set; }

    /// <summary>
    /// Gets or sets the licensed key
    /// </summary>
    public string LicenseKey { get; set; }

    /// <summary>
    /// Gets or sets the license code
    /// </summary>
    public string LicenseCode { get; set; }
    
    /// <summary>
    /// Gets or sets the server port to use 
    /// </summary>
    public int? ServerPort { get; set; }

    /// <summary>
    /// Gets or sets if the app should start minimized
    /// </summary>
    public bool StartMinimized { get; set; }

    /// <summary>
    /// Gets or sets an optional FileFlows.com URL
    /// </summary>
    public string FileFlowsDotComUrl { get; set; }

    /// <summary>
    /// Gets or sets the connection string of where to migrate the data to
    /// This will be checked at startup and if found, the data will be migrated then this
    /// setting will be reset
    /// </summary>
    public string DatabaseMigrateConnection { get; set; }


    private static AppSettings? _Instance;

    /// <summary>
    /// The AppSettings instance
    /// </summary>
    public static AppSettings Instance
    {
        get
        {
            _Instance ??= Load();
            return _Instance;
        }
    }

    /// <summary>
    /// Saves the app settings
    /// </summary>
    public void Save()
    {
        if (ServerPort != null && (ServerPort < 1 || ServerPort > 65535))
            ServerPort = null;
        
        var serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition  = JsonIgnoreCondition.WhenWritingDefault | JsonIgnoreCondition.WhenWritingNull
        };
        
        string json = JsonSerializer.Serialize(this, serializerOptions);
        File.WriteAllText(DirectoryHelper.ServerConfigFile, json);
    }
    
    /// <summary>
    /// Loads the app settings
    /// </summary>
    /// <returns>the app settings</returns>
    public static AppSettings Load()
    {
        string file = DirectoryHelper.ServerConfigFile;
        if (File.Exists(file) == false)
        {
            AppSettings settings = new();
            if (File.Exists(DirectoryHelper.EncryptionKeyFile))
            {
                settings.EncryptionKey = File.ReadAllText(DirectoryHelper.EncryptionKeyFile);
                File.Delete(DirectoryHelper.EncryptionKeyFile);
            }

            if (string.IsNullOrWhiteSpace(settings.EncryptionKey))
                settings.EncryptionKey = Guid.NewGuid().ToString();
            
            settings.Save();
            
            return settings;
        }

        AppSettings? result = null;
        try
        {
            string json = File.ReadAllText(file);
            var settings = JsonSerializer.Deserialize<AppSettings>(json);
            result = settings ?? new ();
        }
        catch (Exception) { }

        result ??= new();


        if (string.IsNullOrWhiteSpace(result.EncryptionKey))
        {
            result.EncryptionKey = Guid.NewGuid().ToString();
            result.Save();
        }

        return result;
    }
}