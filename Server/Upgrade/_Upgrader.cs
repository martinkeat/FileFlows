using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Upgrade;

public class Upgrader
{
    public void Run(Settings settings)
    {
        var currentVersion = string.IsNullOrWhiteSpace(settings.Version) ? new Version() : Version.Parse(settings.Version);
        Logger.Instance.ILog("Current version: " + currentVersion);
        // check if current version is even set, and only then do we run the upgrades
        // so on a clean install these do not run
        if (currentVersion > new Version(0, 4, 0))
        {
            if (new Version(settings.Version).ToString() != new Version(Globals.Version).ToString())
            {
                // first backup the database
                if (DbHelper.UseMemoryCache)
                {
                    try
                    {
                        Logger.Instance.ILog("Backing up database");
                        string source = SqliteDbManager.SqliteDbFile;
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
                    // backup a MySQL db using the migrator
                    try
                    {
                        Logger.Instance.ILog("Backing up database, please wait this may take a while");
                        string dbBackup = DatabaseBackupManager.CreateBackup(AppSettings.Instance.DatabaseConnection,
                            DirectoryHelper.DatabaseDirectory, currentVersion);
                        Logger.Instance.ILog("Backed up database to: " + dbBackup);
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.ELog("Failed creating database backup: " + ex.Message);
                    }
                }
            }
            if (currentVersion < new Version(24, 3, 0))  
                new Upgrade_24_02().Run(settings);
        }

        // save the settings
        if (settings.Version != Globals.Version.ToString())
        {
            Logger.Instance.ILog("Saving version to database");
            settings.Version = Globals.Version.ToString();
            // always increase the revision when the version changes
            settings.Revision += 1;
            DbHelper.Update(settings).Wait();
        }
        Logger.Instance.ILog("Finished checking upgrade scripts");
    }
}
