using FileFlows.Managers;
using FileFlows.Managers.InitializationManagers;
using FileFlows.Plugin;
using FileFlows.Server.Services;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Upgrade;

public class Upgrader
{
    // update this with the latest db version
    private readonly Version LATEST_DB_VERSION = new Version(24, 03, 2);

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
            // REFACTOR: re-look into this

            //
            // // backup a MySQL db using the migrator
            // try
            // {
            //     Logger.Instance.ILog("Backing up database, please wait this may take a while");
            //     string dbBackup = DatabaseBackupManager.CreateBackup(AppSettings.Instance.DatabaseConnection,
            //         DirectoryHelper.DatabaseDirectory, currentVersion);
            //     Logger.Instance.ILog("Backed up database to: " + dbBackup);
            // }
            // catch (Exception ex)
            // {
            //     Logger.Instance.ELog("Failed creating database backup: " + ex.Message);
            // }
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
        if (currentVersion < new Version(24, 3, 0))  
            new Upgrade_24_02().Run(appSettingsService);


        // save the settings
        Logger.Instance.ILog("Saving version to database");
        var manager = GetUpgradeManager(appSettingsService.Settings);
        var result = manager.SaveCurrentVersion().Result;
        if (result.IsFailed)
            return result;
        
        Logger.Instance.ILog("Finished checking upgrade scripts");
        return true;
    }
}
