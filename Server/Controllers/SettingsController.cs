using FileFlows.Plugin;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.InteropServices;
using FileFlows.Shared.Models;
using FileFlows.Server.Workers;
using FileFlows.Server.Helpers;
using FileFlows.Server.Database.Managers;
using FileFlows.Server.Services;


namespace FileFlows.Server.Controllers;
/// <summary>
/// Settings Controller
/// </summary>
[Route("/api/settings")]
public class SettingsController : Controller
{
    private static Settings Instance;
    private static SemaphoreSlim semaphore = new SemaphoreSlim(1);

    /// <summary>
    /// Gets the system status of FileFlows
    /// </summary>
    /// <returns>the system status of FileFlows</returns>
    [HttpGet("fileflows-status")]
    public FileFlowsStatus GetFileFlowsStatus()
    {
        FileFlowsStatus status = new();
        
        var license = LicenseHelper.GetLicense();
        if (license?.Status == LicenseStatus.Valid)
        {
            status.Licensed = true;
            string dbConnStr = AppSettings.Instance.DatabaseConnection;
            status.ExternalDatabase = (string.IsNullOrWhiteSpace(dbConnStr) || dbConnStr.ToLower().Contains("sqlite")) == false;
            status.LicenseDashboards = (license.Flags & LicenseFlags.Dashboards) == LicenseFlags.Dashboards;
            status.LicenseRevisions = (license.Flags & LicenseFlags.Revisions) == LicenseFlags.Revisions;
            status.LicenseTasks = (license.Flags & LicenseFlags.Tasks) == LicenseFlags.Tasks;
            status.LicenseAutoUpdates = (license.Flags & LicenseFlags.AutoUpdates) == LicenseFlags.AutoUpdates;
            status.LicenseWebhooks = (license.Flags & LicenseFlags.Webhooks) == LicenseFlags.Webhooks;
            status.LicenseProcessingOrder = (license.Flags & LicenseFlags.ProcessingOrder) == LicenseFlags.ProcessingOrder;
            status.LicenseEnterprise = (license.Flags & LicenseFlags.Enterprise) == LicenseFlags.Enterprise;
        }

        bool libs = new LibraryService().GetAll().Any();
        bool flows = new FlowService().GetAll().Any();

        if (flows)
            status.ConfigurationStatus |= ConfigurationStatus.Flows;
        if (libs)
            status.ConfigurationStatus |= ConfigurationStatus.Libraries;
        
        return status;
    }

    /// <summary>
    /// Checks latest version from fileflows.com
    /// </summary>
    /// <returns>The latest version number if greater than current</returns>
    [HttpGet("check-update-available")]
    public async Task<string> CheckLatestVersion()
    {
        var settings = await new SettingsController().Get();
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
        if ((license == null || license.Status == LicenseStatus.Unlicensed) && string.IsNullOrWhiteSpace(AppSettings.Instance.LicenseKey) == false)
            license.Status = LicenseStatus.Invalid;
        // clone it so we can remove some properties we dont want passed to the UI
        string json = JsonSerializer.Serialize(settings);
        var uiModel = JsonSerializer.Deserialize<SettingsUiModel>(json);
        SetLicenseFields(uiModel, license);
        uiModel.FileServerAllowedPathsString = uiModel.FileServerAllowedPaths?.Any() == true
            ? string.Join("\n", uiModel.FileServerAllowedPaths)
            : string.Empty;
        
        string dbConnStr = AppSettings.Instance.DatabaseMigrateConnection?.EmptyAsNull() ?? AppSettings.Instance.DatabaseConnection;
        if (string.IsNullOrWhiteSpace(dbConnStr) || dbConnStr.ToLower().Contains("sqlite"))
            uiModel.DbType = DatabaseType.Sqlite;
        else if (dbConnStr.Contains(";Uid="))
            new MySqlDbManager(string.Empty).PopulateSettings(uiModel, dbConnStr);
        // else
        //     new SqlServerDbManager(string.Empty).PopulateSettings(uiModel, dbConnStr);
        uiModel.RecreateDatabase = AppSettings.Instance.RecreateDatabase;
        
        return uiModel;
    }

    /// <summary>
    /// Get the system settings
    /// </summary>
    /// <returns>The system settings</returns>
    [HttpGet]
    public async Task<Settings> Get()
    {
        await semaphore.WaitAsync();
        try
        {
            if (Instance == null)
            {
                Instance = await DbHelper.Single<Settings>();
            }
            Instance.IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            Instance.IsDocker = Program.Docker;

            if (LicenseHelper.IsLicensed() == false)
            {
                Instance.LogFileRetention = 2;
            }
            
            return Instance;
        }
        finally
        {
            semaphore.Release();
        }
    }

    private void SetLicenseFields(SettingsUiModel settings, License license)
    {
        settings.LicenseKey = AppSettings.Instance.LicenseKey;
        settings.LicenseEmail  = AppSettings.Instance.LicenseEmail;
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
            FileServerAllowedPaths = model.FileServerAllowedPathsString?.Split(new [] { "\r\n", "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries)
        });
        // validate license it
        AppSettings.Instance.LicenseKey = model.LicenseKey?.Trim();
        AppSettings.Instance.LicenseEmail = model.LicenseEmail?.Trim();
        await LicenseHelper.Update();

        var newConnectionString = GetConnectionString(model, model.DbType);
        if (IsConnectionSame(AppSettings.Instance.DatabaseConnection, newConnectionString) == false)
        {
            // need to migrate the database
            AppSettings.Instance.DatabaseMigrateConnection = newConnectionString?.EmptyAsNull() ?? DbManager.GetDefaultConnectionString();
        }

        AppSettings.Instance.RecreateDatabase = model.RecreateDatabase; 
        // save AppSettings with updated license and db migration if set
        AppSettings.Instance.Save();
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
        var settings = await Get() ?? model;
        model.Name = settings.Name;
        model.Uid = settings.Uid;
        model.Version = Globals.Version.ToString();
        model.DateCreated = settings.DateCreated;
        model.IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        model.IsDocker = Program.Docker;
        model.Revision = Math.Max(model.Revision, Instance.Revision + 1);
        Instance = model;
        await DbHelper.Update(model);
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

    private string GetConnectionString(SettingsUiModel settings, DatabaseType dbType)
    {
        // if (dbType == DatabaseType.SqlServer)
        //     return new SqlServerDbManager(string.Empty).GetConnectionString(settings.DbServer, settings.DbName, settings.DbUser,
        //         settings.DbPassword);
        if (dbType == DatabaseType.MySql)
            return new MySqlDbManager(string.Empty).GetConnectionString(settings.DbServer, settings.DbName, settings.DbPort, settings.DbUser,
                settings.DbPassword);
        return string.Empty;
    }
    

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

        // if (model.Type == DatabaseType.SqlServer)
        //     return new SqlServerDbManager(string.Empty).Test(model.Server, model.Name, model.User, model.Password)
        //         ?.EmptyAsNull() ?? "OK";
        if (model.Type == DatabaseType.MySql)
            return new MySqlDbManager(string.Empty).Test(model.Server, model.Name, model.Port, model.User, model.Password)
                ?.EmptyAsNull() ?? "OK";
        
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
    public int GetCurrentConfigRevision() => Instance.Revision;
    
    /// <summary>
    /// Loads the current configuration
    /// </summary>
    /// <returns>the current configuration</returns>
    [HttpGet("current-config")]
    public async Task<ConfigurationRevision> GetCurrentConfig()
    {
        var cfg = new ConfigurationRevision();
        cfg.Revision = Instance.Revision;
        var scriptController = new ScriptController();
        cfg.FlowScripts = (await scriptController.GetAllByType(ScriptType.Flow)).ToList();
        cfg.SystemScripts = (await scriptController.GetAllByType(ScriptType.System)).ToList();
        cfg.SharedScripts = (await scriptController.GetAllByType(ScriptType.Shared)).ToList();
        cfg.Variables = new VariableService().GetAll().ToDictionary(x => x.Name, x => x.Value);
        cfg.Flows = new FlowService().GetAll();
        cfg.Libraries = new LibraryService().GetAll();
        cfg.Enterprise = LicenseHelper.IsLicensed(LicenseFlags.Enterprise);
        cfg.AllowRemote = Instance.FileServerDisabled == false;
        cfg.PluginSettings = new PluginService().GetAllPluginSettings().Result;
        cfg.MaxNodes = LicenseHelper.IsLicensed() ? 250 : 30;
        cfg.KeepFailedFlowTempFiles = Instance.KeepFailedFlowTempFiles;
        cfg.Enterprise = LicenseHelper.IsLicensed(LicenseFlags.Enterprise);
        var pluginInfos = (await new PluginController().GetAll())
            .Where(x => x.Enabled)
            .ToDictionary(x => x.PackageName + ".ffplugin", x => x);
        var plugins = new Dictionary<string, byte[]>();
        var pluginNames = new List<string>();
        List<string> flowElementsInUse = cfg.Flows.SelectMany(x => x.Parts.Select(x => x.FlowElementUid)).ToList();
        
        Logger.Instance.DLog("Plugin, Flow Elements in Use: \n" + string.Join("\n", flowElementsInUse));

        foreach (var file in new DirectoryInfo(DirectoryHelper.PluginsDirectory).GetFiles("*.ffplugin"))
        {
            Logger.Instance.DLog($"Plugin found '{file.Name}'");
            if (pluginInfos.ContainsKey(file.Name) == false)
            {
                Logger.Instance.DLog($"Plugin '{file.Name}' not enabled skipping for configuration.");
                continue; // not enabled, skipped
            }

            var pluginInfo = pluginInfos[file.Name];
            
            var inUse = pluginInfo.Elements.Any(x => flowElementsInUse.Contains(x.Uid));
            if (inUse == false)
            {
                Logger.Instance.DLog($"Plugin '{pluginInfo.Name}' not in use by any flow, skipping");
                Logger.Instance.DLog("Plugin not using flow parts: " + string.Join(", ", pluginInfo.Elements.Select(x => x.Uid)));
                continue; // plugin not used, skipping
            }

            Logger.Instance.DLog($"Plugin '{pluginInfo.Name}' is used in configuration.");
            plugins.Add(file.Name, await System.IO.File.ReadAllBytesAsync(file.FullName));
            pluginNames.Add(pluginInfo.Name);
        }

        cfg.Plugins = plugins;
        cfg.PluginNames = pluginNames;
        Logger.Instance.DLog($"Plugin list that is used in configuration:", string.Join(", ", plugins.Select(x => x.Key)));
        
        return cfg;
    }
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
