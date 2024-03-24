using FileFlows.DataLayer.Upgrades;
using FileFlows.Plugin;
using FileFlows.ServerShared;

namespace FileFlows.Managers.InitializationManagers;

/// <summary>
/// Manager used by upgrade scripts to perform operations on the database
/// </summary>
public class UpgradeManager
{
    /// <summary>
    /// the logger to use
    /// </summary>
    private readonly ILogger Logger;
    /// <summary>
    /// the type of database
    /// </summary>
    private readonly DatabaseType DbType;
    /// <summary>
    /// the database connection string
    /// </summary>
    private readonly string ConnectionString;
    
    /// <summary>
    /// Initializes a new instance of the upgrade manager
    /// </summary>
    /// <param name="logger">the logger to use</param>
    /// <param name="dbType">the type of database</param>
    /// <param name="connectionString">the database connection string</param>
    public UpgradeManager(ILogger logger, DatabaseType dbType, string connectionString)
    {
        Logger = logger;
        DbType = dbType;
        ConnectionString = connectionString;
    }

    /// <summary>
    /// Checks if a database exists
    /// </summary>
    /// <returns>true if it exists</returns>
    public Result<bool> DatabaseExists()
        => MigrationManager.DatabaseExists(DbType, ConnectionString);

    /// <summary>
    /// Tests a connection to a database
    /// </summary>
    public Result<bool> CanConnectToServer()
        => MigrationManager.CanConnect(DbType, ConnectionString);
    
    /// <summary>
    /// Gets the current version from the database
    /// </summary>
    /// <returns>the current version</returns>
    public async Task<Result<Version?>> GetCurrentVersion()
    {
        try
        {
            string error;
            var dam = DatabaseAccessManager.FromType(Logger, DbType, ConnectionString);
            if (DatabaseAccessManager.CanConnect(DbType, ConnectionString).Failed(out error))
                return Result<Version?>.Fail(error);
            var settings = await dam.FileFlowsObjectManager.Single<Settings>();
            if (settings.Failed(out error) || settings.Value == null)
                return null;
            return Version.Parse(settings.Value.Version);
        }
        catch (Exception ex)
        {
            return Result<Version?>.Fail(ex.Message);
        }
    }

    /// <summary>
    /// Saves the current version to the database
    /// </summary>
    /// <returns>true if the update was successful</returns>
    public async Task<Result<bool>> SaveCurrentVersion()
    {
        try
        {
            var dam = DatabaseAccessManager.FromType(Logger, DbType, ConnectionString);
            var settings = await dam.FileFlowsObjectManager.Single<Settings>();
            if (settings.Failed(out string error))
                return Result<bool>.Fail(error);

            settings.Value.Version = Globals.Version;
            settings.Value.Revision += 1;
            await dam.FileFlowsObjectManager.Update(settings);
            return true;
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex.Message);
        }
    }
    
    /// <summary>
    /// Run upgrade from 24.02
    /// </summary>
    public Task Run_Upgrade_24_02(ILogger logger, DatabaseType dbType, string connectionString)
        => new Upgrade_24_02().Run(logger, dbType, connectionString);
    
    /// <summary>
    /// Run upgrade from 24.03.2
    /// </summary>
    public void Run_Upgrade_24_03_2(ILogger logger, DatabaseType dbType, string connectionString)
        => new Upgrade_24_03_2().Run(logger, dbType, connectionString);
    
    /// <summary>
    /// Run upgrade from 24.03.5
    /// </summary>
    public void Run_Upgrade_24_03_5(ILogger logger, DatabaseType dbType, string connectionString)
        => new Upgrade_24_03_5().Run(logger, dbType, connectionString);
}