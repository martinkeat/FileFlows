using System.Text.Json.Serialization;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// Service for read/writing the application settings
/// </summary>
public class AppSettingsService
{
    public AppSettingsService()
    {
        Settings = Load();
        if (Settings.DatabaseType == DatabaseType.Sqlite && Settings.DatabaseConnection?.Contains("Server") == true)
        {
            // this should only happen if they upgraded from 24.03.1 which did not store the database type in the database
            // and only MySQL was supported as an external database up until 24.03.2
            Settings.DatabaseType = DatabaseType.MySql;
        }

        if (string.IsNullOrWhiteSpace(Settings.DatabaseConnection))
            Settings.DatabaseConnection = SqliteHelper.GetConnectionString();
        Save();
 
        Globals.CustomFileFlowsDotComUrl = Settings.FileFlowsDotComUrl;
    }

    /// <summary>
    /// The AppSettings instance
    /// </summary>
    internal AppSettings Settings { get; private set; }
    
    /// <summary>
    /// Saves the app settings
    /// </summary>
    public void Save()
    {
        if (Settings.ServerPort != null && (Settings.ServerPort < 1 || Settings.ServerPort > 65535))
            Settings.ServerPort = null;
        
        var serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition  = JsonIgnoreCondition.WhenWritingDefault | JsonIgnoreCondition.WhenWritingNull
        };
        
        string json = JsonSerializer.Serialize(Settings, serializerOptions);
        File.WriteAllText(DirectoryHelper.ServerConfigFile, json);
    }
    
    /// <summary>
    /// Loads the app settings
    /// </summary>
    /// <returns>the app settings</returns>
    private AppSettings Load()
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
            
            return settings;
        }

        AppSettings? result = null;
        try
        {
            string json = File.ReadAllText(file);
            var options = new JsonSerializerOptions
            {
                AllowTrailingCommas = true
            };
            var settings = JsonSerializer.Deserialize<AppSettings>(json, options);
            result = settings ?? new();
        }
        catch (Exception ex)
        {
            Logger.Instance.WLog("Failed reading application settings file: " + ex.Message);
        }

        result ??= new();


        if (string.IsNullOrWhiteSpace(result.EncryptionKey))
        {
            result.EncryptionKey = Guid.NewGuid().ToString();
        }

        return result;
    }
}