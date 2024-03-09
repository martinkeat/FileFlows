using FileFlows.Plugin;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.InteropServices;
using FileFlows.Shared.Models;
using FileFlows.Server.Workers;
using FileFlows.Server.Helpers;
using FileFlows.Server.Services;
using FileFlows.ServerShared.Services;
using SettingsService = FileFlows.Server.Services.SettingsService;


namespace FileFlows.Server.Controllers;
/// <summary>
/// Settings Controller
/// </summary>
[Route("/api/settings")]
public class SettingsController : Controller
{
    /// <summary>
    /// The settings for the application
    /// </summary>
    private AppSettings Settings;
    /// <summary>
    /// The settings for the application
    /// </summary>
    private AppSettingsService SettingsService;
    
    /// <summary>
    /// Initializes a new instance of the controller
    /// </summary>
    /// <param name="appSettingsService">the application settings service</param>
    public SettingsController(AppSettingsService appSettingsService)
    {
        SettingsService = appSettingsService;
        Settings = appSettingsService.Settings;
    }

    /// <summary>
    /// Gets the system status of FileFlows
    /// </summary>
    /// <returns>the system status of FileFlows</returns>
    [HttpGet("fileflows-status")]
    public Task<FileFlowsStatus> GetFileFlowsStatus()
        => ServiceLoader.Load<SettingsService>().GetFileFlowsStatus();

    /// <summary>
    /// Checks latest version from fileflows.com
    /// </summary>
    /// <returns>The latest version number if greater than current</returns>
    [HttpGet("check-update-available")]
    public async Task<string> CheckLatestVersion()
    {
        var settings = await Get();
        if (settings.DisableTelemetry)
            return string.Empty; 
        try
        {
            var result = ServerUpdater.GetLatestOnlineVersion();
            if (result.updateAvailable == false)
                return string.Empty;
            return result.onlineVersion.ToString();
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Failed checking latest version: " + ex.Message + Environment.NewLine + ex.StackTrace);
            return String.Empty;
        }
    }

    /// <summary>
    /// Get the system settings
    /// </summary>
    /// <returns>The system settings</returns>
    [HttpGet("ui-settings")]
    public async Task<SettingsUiModel> GetUiModel()
    {
        var settings = await Get();
        var license = LicenseHelper.GetLicense();
        if ((license == null || license.Status == LicenseStatus.Unlicensed) && string.IsNullOrWhiteSpace(Settings.LicenseKey) == false)
            license.Status = LicenseStatus.Invalid;
        // clone it so we can remove some properties we dont want passed to the UI
        string json = JsonSerializer.Serialize(settings);
        var uiModel = JsonSerializer.Deserialize<SettingsUiModel>(json);
        SetLicenseFields(uiModel, license);
        uiModel.FileServerAllowedPathsString = uiModel.FileServerAllowedPaths?.Any() == true
            ? string.Join("\n", uiModel.FileServerAllowedPaths)
            : string.Empty;
        
        string dbConnStr = Settings.DatabaseMigrateConnection?.EmptyAsNull() ?? Settings.DatabaseConnection;
        if (string.IsNullOrWhiteSpace(dbConnStr) || dbConnStr.ToLower().Contains("sqlite"))
            uiModel.DbType = DatabaseType.Sqlite;
        // REFACTOR: re-look into this
        // else if (dbConnStr.Contains(";Uid="))
        //     new MySqlDbManager(string.Empty).PopulateSettings(uiModel, dbConnStr);
        uiModel.RecreateDatabase = Settings.RecreateDatabase;
        
        return uiModel;
    }

    /// <summary>
    /// Get the system settings
    /// </summary>
    /// <returns>The system settings</returns>
    [HttpGet]
    public Task<Settings> Get()
        => ServiceLoader.Load<SettingsService>().Get();

    private void SetLicenseFields(SettingsUiModel settings, License license)
    {
        settings.LicenseKey = Settings.LicenseKey;
        settings.LicenseEmail  = Settings.LicenseEmail;
        settings.LicenseFiles = license == null ? string.Empty :
            license.Files >= 1_000_000_000 ? "Unlimited" : license.Files.ToString();
        settings.LicenseFlags = license == null ? 0 : license.Flags;
        settings.LicenseProcessingNodes = LicenseHelper.GetLicensedProcessingNodes();
        settings.LicenseExpiryDate = license == null ? DateTime.MinValue : license.ExpirationDateUtc.ToLocalTime();
        settings.LicenseStatus = (license == null ? LicenseStatus.Unlicensed : license.Status).ToString();
    }

    /// <summary>
    /// Save the system settings
    /// </summary>
    /// <param name="model">the system settings to save</param>
    /// <returns>The saved system settings</returns>
    [HttpPut("ui-settings")]
    public async Task SaveUiModel([FromBody] SettingsUiModel model)
    {
        if (model == null)
            return;

        await Save(new ()
        {
            PausedUntil = model.PausedUntil,
            Language = model.Language,
            LogFileRetention = model.LogFileRetention,
            LogDatabaseRetention = model.LogDatabaseRetention,
            LogEveryRequest = model.LogEveryRequest,
            AutoUpdate = model.AutoUpdate,
            DisableTelemetry = model.DisableTelemetry,
            AutoUpdateNodes = model.AutoUpdateNodes,
            AutoUpdatePlugins = model.AutoUpdatePlugins,
            LogQueueMessages = model.LogQueueMessages,
            KeepFailedFlowTempFiles = model.KeepFailedFlowTempFiles,
            ShowFileAddedNotifications = model.ShowFileAddedNotifications,
            HideProcessingStartedNotifications = model.HideProcessingStartedNotifications,
            HideProcessingFinishedNotifications = model.HideProcessingFinishedNotifications,
            ProcessFileCheckInterval = model.ProcessFileCheckInterval,
            FileServerDisabled = model.FileServerDisabled,
            FileServerOwnerGroup = model.FileServerOwnerGroup,
            FileServerFilePermissions = model.FileServerFilePermissions,
            FileServerAllowedPaths = model.FileServerAllowedPathsString?.Split(new [] { "\r\n", "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries)
        });
        // validate license it
        Settings.LicenseKey = model.LicenseKey?.Trim();
        Settings.LicenseEmail = model.LicenseEmail?.Trim();
        await LicenseHelper.Update();
        
        TranslaterHelper.InitTranslater(model.Language?.EmptyAsNull() ?? "en");
        
        // REFACTOR: re-look into this
        // var newConnectionString = GetConnectionString(model, model.DbType);
        // if (IsConnectionSame(Settings.DatabaseConnection, newConnectionString) == false)
        // {
        //     // need to migrate the database
        //     Settings.DatabaseMigrateConnection = newConnectionString?.EmptyAsNull() ?? DbManager.GetDefaultConnectionString();
        // }

        Settings.RecreateDatabase = model.RecreateDatabase; 
        // save AppSettings with updated license and db migration if set
        SettingsService.Save();
    }
    /// <summary>
    /// Save the system settings
    /// </summary>
    /// <param name="model">the system settings to save</param>
    /// <returns>The saved system settings</returns>
    internal async Task Save(Settings model)
    {
        if (model == null)
            return;
        await ServiceLoader.Load<SettingsService>().Save(model);
    }

    private bool IsConnectionSame(string original, string newConnection)
    {
        if (IsSqliteConnection(original) && IsSqliteConnection(newConnection))
            return true;
        return original == newConnection;
    }

    private bool IsSqliteConnection(string connString)
    {
        if (string.IsNullOrWhiteSpace(connString))
            return true;
        return connString.IndexOf("FileFlows.sqlite") > 0;
    }

    // private string GetConnectionString(SettingsUiModel settings, DatabaseType dbType)
    // {
    //     // if (dbType == DatabaseType.SqlServer)
    //     //     return new SqlServerDbManager(string.Empty).GetConnectionString(settings.DbServer, settings.DbName, settings.DbUser,
    //     //         settings.DbPassword);
    //     if (dbType == DatabaseType.MySql)
    //         return new MySqlDbManager(string.Empty).GetConnectionString(settings.DbServer, settings.DbName, settings.DbPort, settings.DbUser,
    //             settings.DbPassword);
    //     return string.Empty;
    // }
    

    /// <summary>
    /// Tests a database connection
    /// </summary>
    /// <param name="model">The database connection info</param>
    /// <returns>OK if successful, otherwise a failure message</returns>
    [HttpPost("test-db-connection")]
    public string TestDbConnection([FromBody] DbConnectionInfo model)
    {
        if (model == null)
            throw new ArgumentException(nameof(model));

        // REFACTOR: re-look into this
        // if (model.Type == DatabaseType.MySql)
        //     return new MySqlDbManager(string.Empty).Test(model.Server, model.Name, model.Port, model.User, model.Password)
        //         ?.EmptyAsNull() ?? "OK";
        
        return "Unsupported database type";
    }

    /// <summary>
    /// Triggers a check for an update
    /// </summary>
    [HttpPost("check-for-update-now")]
    public async Task CheckForUpdateNow()
    {
        if (LicenseHelper.IsLicensed(LicenseFlags.AutoUpdates) == false)
            return;

        if (ServerUpdater.Instance == null)
            return;

        _ = Task.Run(async () =>
        {
            await Task.Delay(1);
            return ServerUpdater.Instance.RunCheck();
        });
        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets the current configuration revision
    /// </summary>
    /// <returns>the current revision</returns>
    [HttpGet("current-config/revision")]
    public Task<int> GetCurrentConfigRevision()
        => ServiceLoader.Load<SettingsService>().GetCurrentConfigurationRevision();
    
    /// <summary>
    /// Loads the current configuration
    /// </summary>
    /// <returns>the current configuration</returns>
    [HttpGet("current-config")]
    public Task<ConfigurationRevision> GetCurrentConfig()
        => ServiceLoader.Load<SettingsService>().GetCurrentConfiguration();
}

/// <summary>
/// Database connection details
/// </summary>
public class DbConnectionInfo
{
    /// <summary>
    /// Gets or sets the server address
    /// </summary>
    public string Server { get; set; }
    /// <summary>
    /// Gets or sets the database name
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Gets or sets the port
    /// </summary>
    public int Port { get; set; }
    /// <summary>
    /// Gets or sets the connecting user
    /// </summary>
    public string User { get; set; }
    /// <summary>
    /// Gets or sets the password used
    /// </summary>
    public string Password { get; set; }
    /// <summary>
    /// Gets or sets the database type
    /// </summary>
    public DatabaseType Type { get; set; }
}
