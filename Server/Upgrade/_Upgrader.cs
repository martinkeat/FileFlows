using FileFlows.Managers.InitializationManagers;
using FileFlows.Plugin;
using FileFlows.Server.Services;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Upgrade;

/// <summary>
/// Runs the upgrades
/// </summary>
public class Upgrader
{
    // update this with the latest db version
    private readonly Version LATEST_DB_VERSION = new Version(24, 03, 5);

    /// <summary>
    /// Gets an instance of the upgrade manager
    /// </summary>
    /// <param name="settings">the application settings</param>
    /// <returns>an instance of the upgrade manager</returns>
    private UpgradeManager GetUpgradeManager(AppSettings settings)
        => new (Logger.Instance, settings.DatabaseType, settings.DatabaseConnection);
    
    /// <summary>
    /// Checks if an upgrade is required
    /// </summary>
    /// <param name="settings">the application settings</param>
    /// <returns>true if an upgrade is required, otherwise false</returns>
    internal Result<(bool Required, Version Current)> UpgradeRequired(AppSettings settings)
    {
        var manager = GetUpgradeManager(settings);
        
        var versionResult = manager.GetCurrentVersion().Result;
        if (versionResult.Failed(out string error))
            return Result<(bool Required, Version Current)>.Fail(error);
        if (versionResult.Value == null)
            return (false, new Version()); // database has not been initialized

        var version = versionResult.Value;
        return (version < LATEST_DB_VERSION, version);
    }

    /// <summary>
    /// Backup the current database
    /// </summary>
    /// <param name="currentVersion">the current version in the database</param>
    /// <param name="settings">the application settings</param>
    internal void Backup(Version currentVersion, AppSettings settings)
    {
        // first backup the database
        if (settings.DatabaseType == DatabaseType.Sqlite)
        {
            try
            {
                Logger.Instance.ILog("Backing up database");
                string source = Path.Combine(DirectoryHelper.DatabaseDirectory, "FileFlows.sqlite");
                string dbBackup = source.Replace(".sqlite",
                    "-" + currentVersion.Major + "." + currentVersion.Minor + "." + currentVersion.Build +
                    ".sqlite.backup");
                File.Copy(source, dbBackup);
                Logger.Instance.ILog("Backed up database to: " + dbBackup);
            }
            catch (Exception)
            {
            }
        }
        else
        {
            try
            {
                Logger.Instance.ILog("Backing up database, please wait this may take a while");
                string dbBackup = Path.Combine(DirectoryHelper.DatabaseDirectory, "FileFlow-" +
                    currentVersion.Major + "." + currentVersion.Minor + "." + currentVersion.Build +
                    ".sqlite.backup");
                var manager = new MigrationManager(Logger.Instance,
                    new() { Type = settings.DatabaseType, ConnectionString = settings.DatabaseConnection },
                    new () { Type = DatabaseType.Sqlite, ConnectionString = SqliteHelper.GetConnectionString(dbBackup) }
                );
                var result = manager.Migrate(backingUp: true);
                if(result.Failed(out string error))
                    Logger.Instance.ILog("Failed to backup database: " + error);
                else
                    Logger.Instance.ILog("Backed up database to: " + dbBackup);
            }
            catch (Exception ex)
            {
                Logger.Instance.ELog("Failed creating database backup: " + ex.Message);
            }
        }
    }

    /// <summary>
    /// Run the updates
    /// </summary>
    /// <param name="currentVersion">the current version in the database</param>
    /// <param name="appSettingsService">the application settings service</param>
    internal Result<bool> Run(Version currentVersion, AppSettingsService appSettingsService)
    {
        Logger.Instance.ILog("Current version: " + currentVersion);
        // check if current version is even set, and only then do we run the upgrades
        // so on a clean install these do not run
        if (currentVersion < new Version(23, 0))
            return true; // way to old, or new install
        
        var manager = GetUpgradeManager(appSettingsService.Settings);
        if (currentVersion < new Version(24, 2))
        {
            Logger.Instance.ILog("Running 24.2 upgrade");
            new Upgrade_24_02(Logger.Instance, appSettingsService, manager).Run();
        }

        if (currentVersion < new Version(24, 3, 2))
        {
            Logger.Instance.ILog("Running 24.3.2 upgrade");
            new Upgrade_24_03_2(Logger.Instance, appSettingsService, manager).Run();
        }

        if (currentVersion < new Version(24, 3, 5))
        {
            Logger.Instance.ILog("Running 24.3.5 upgrade");
            new Upgrade_24_03_5(Logger.Instance, appSettingsService, manager).Run();
        }

        // save the settings
        Logger.Instance.ILog("Saving version to database");
        var result = manager.SaveCurrentVersion().Result;
        if (result.IsFailed)
            return result;
        
        Logger.Instance.ILog("Finished checking upgrade scripts");
        return true;
    }
}
