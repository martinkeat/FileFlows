using FileFlows.Managers;
using FileFlows.Managers.InitializationManagers;
using FileFlows.Plugin;
using FileFlows.Server.Helpers;
using FileFlows.Server.Utils;
using FileFlows.ServerShared.Models;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// This service handles preparing the database and automatic migrating
/// It does not handle accessing the database
/// </summary>
public class DatabaseService
{
    private AppSettingsService ServiceAppSetting;
    public DatabaseService(AppSettingsService appSettingService)
    {
        ServiceAppSetting = appSettingService;
    }

    /// <summary>
    /// Check if a database migration is required
    /// </summary>
    /// <returns>true if migration required, otherwise false</returns>
    public bool MigrateRequired()
        => GetMigrationDatabase() != null;

    /// <summary>
    /// Gets the migration details
    /// </summary>
    /// <returns>the migration details, or null if no migration required</returns>
    private (DatabaseInfo Source, DatabaseInfo Destination)? GetMigrationDatabase()
    {
        var settings = ServiceAppSetting.Settings;
        bool nonSqlite = string.IsNullOrEmpty(settings.DatabaseConnection) == false &&
                         settings.DatabaseConnection.Contains(".sqlite") == false;

        DatabaseInfo source = new() { Type = settings.DatabaseType, ConnectionString = settings.DatabaseConnection };
        
        if (nonSqlite && LicenseHelper.IsLicensed(LicenseFlags.ExternalDatabase) == false)
        {
            DatabaseInfo dest = new ()
                { Type = DatabaseType.Sqlite, ConnectionString = SqliteHelper.GetConnectionString() };
            return (source, dest);
        }
        
        if (settings.DatabaseMigrateType == null || string.IsNullOrEmpty(settings.DatabaseMigrateConnection))
            return null;
        
        {
            DatabaseInfo dest = new ()
                { Type = settings.DatabaseMigrateType.Value, ConnectionString = settings.DatabaseMigrateConnection };
            return (source, dest);
        }
    }

    /// <summary>
    /// Migrates a database
    /// </summary>
    /// <returns>the result of the migration</returns>
    public Result<bool> MigrateDatabase()
    {
        if (ServiceAppSetting.Settings.DatabaseConnection == ServiceAppSetting.Settings.DatabaseMigrateConnection
            || string.IsNullOrWhiteSpace(ServiceAppSetting.Settings.DatabaseMigrateConnection)
            || ServiceAppSetting.Settings.DatabaseMigrateType == null) // invalid migration details
        {
            // migration and current databases match, nothing to do
            ServiceAppSetting.Settings.DatabaseMigrateConnection = null;
            ServiceAppSetting.Settings.DatabaseMigrateType = null;
            ServiceAppSetting.Save();
            return true; 
        }
        
        var migrationDetails = GetMigrationDatabase();
        if (migrationDetails == null)
            return Result<bool>.Fail("Failed to get migration details");

        var manager = new MigrationManager(Logger.Instance, migrationDetails.Value.Source,
            migrationDetails.Value.Destination);

        if (ServiceAppSetting.Settings.RecreateDatabase == false &&
            manager.ExternalDatabaseExists())
        {
            Logger.Instance.ILog("Switching to existing database");
            ServiceAppSetting.Settings.DatabaseConnection = ServiceAppSetting.Settings.DatabaseMigrateConnection;
            ServiceAppSetting.Settings.DatabaseMigrateConnection = null;
            ServiceAppSetting.Settings.DatabaseMigrateType = null;
            ServiceAppSetting.Settings.RecreateDatabase = false;
            ServiceAppSetting.Save();
            return true;
        }
        
        Logger.Instance.ILog("Database migration starting");
        var migratedResult = manager.Migrate();
        if (migratedResult.IsFailed == false)
        {
            ServiceAppSetting.Settings.DatabaseConnection = ServiceAppSetting.Settings.DatabaseMigrateConnection;
            ServiceAppSetting.Settings.DatabaseType = ServiceAppSetting.Settings.DatabaseMigrateType.Value;
        }

        ServiceAppSetting.Settings.DatabaseMigrateConnection = null;
        ServiceAppSetting.Settings.DatabaseMigrateType = null;
        ServiceAppSetting.Settings.RecreateDatabase = false;
        ServiceAppSetting.Save();
        if (migratedResult.IsFailed)
            return Result<bool>.Fail("Database migration failed, reverting to previous database settings");

        return true;
    }
    
    /// <summary>
    /// Prepares the database
    /// </summary>
    /// <returns>the result</returns>
    public Result<bool> PrepareDatabase()
    {
        string error;

        Logger.Instance.ILog("About to initialize Database");
        // initialize the database
        if (InitialiseManagers().Failed(out error))
        {
            Logger.Instance.ELog(error);
            return false;
        }
        
        Logger.Instance.ILog("Database initialized");

        RestoreDefaults();

        // _ = new DatabaseLogger();

        return true;
    }

    /// <summary>
    /// Restores any default values into the database
    /// </summary>
    private void RestoreDefaults()
    {
        new NodeManager().EnsureInternalNodeExists().Wait();
        new VariableManager().RestoreDefault();
    }

    /// <summary>
    /// Initializes the database managers
    /// </summary>
    private Result<bool> InitialiseManagers()
    {
        var settings = ServiceAppSetting.Settings;
        return Initializer.Init(Logger.Instance, settings.DatabaseType, settings.DatabaseConnection, settings.EncryptionKey);
    }

    /// <summary>
    /// Tests a connection to a database
    /// </summary>
    /// <param name="type">the type of the database</param>
    /// <param name="connectionString">the connection string to the database</param>
    /// <returns>true if successfully connected, otherwise false</returns>
    public Result<bool> TestConnection(DatabaseType type, string connectionString)
        => MigrationManager.CanConnect(type, connectionString);

    /// <summary>
    /// Gets the open number of database connections
    /// </summary>
    /// <returns>the number of connections</returns>
    public int GetOpenConnections()
        => new SettingsManager().GetOpenConnections();


    /// <summary>
    /// Sets the version in the database
    /// </summary>
    /// <returns>true if successful</returns>
    public Task SetVersion()
        => new FileFlowsManager().SetVersion(Globals.Version);
}