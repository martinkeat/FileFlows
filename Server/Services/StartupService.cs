using FileFlows.Managers.InitializationManagers;
using FileFlows.Plugin;
using FileFlows.Server.DefaultTemplates;
using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// Service used for status up work
/// All startup code, db initialization, migrations, upgrades should be done here,
/// so the UI apps can be shown with a status of what is happening
/// </summary>
public class StartupService
{
    /// <summary>
    /// Callback for notifying a status update
    /// </summary>
    public Action<string> OnStatusUpdate { get; set; }


    private AppSettingsService appSettingsService;

    /// <summary>
    /// Run the startup commands
    /// </summary>
    public Result<bool> Run()
    {
        UpdateStatus("Starting...");
        try
        {
            appSettingsService = ServiceLoader.Load<AppSettingsService>();

            string error;

            CheckLicense();

            CleanDefaultTempDirectory();

            BackupSqlite();

            if (CanConnectToDatabase().Failed(out error))
            {
                UpdateStatus(error);
                return Result<bool>.Fail(error);
            }
            
            if (DatabaseExists()) // only upgrade if it does exist
            {
                if (Upgrade().Failed(out error))
                {
                    UpdateStatus(error);
                    return Result<bool>.Fail(error);
                }
            }
            else if (CreateDatabase().Failed(out error))
            {
                UpdateStatus(error);
                return Result<bool>.Fail(error);
            }

            if (PrepareDatabase().Failed(out error))
            {
                UpdateStatus(error);
                return Result<bool>.Fail(error);
            }

            if (SetVersion().Failed(out error))
            {
                UpdateStatus(error);
                return Result<bool>.Fail(error);
            }
        
            UpdateStatus("Updating Templates...");
            UpdateTemplates();

            if (Globals.IsDocker)
                LogDockerModsFile();

            return true;
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Startup failure: " + ex.Message + Environment.NewLine + ex.StackTrace);
            #if(DEBUG)
            UpdateStatus("Startup failure: " + ex.Message + Environment.NewLine + ex.StackTrace);
            #else
            UpdateStatus("Startup failure: " + ex.Message);
            #endif
            return Result<bool>.Fail(ex.Message);
        }
    }

    /// <summary>
    /// Looks for a DockerMods output file and if found, logs its contents
    /// </summary>
    private void LogDockerModsFile()
    {
        var output = Path.Combine(DirectoryHelper.DockerModsDirectory, "output.log");
        if (File.Exists(output) == false)
            return;
        var content = File.ReadAllText(output);
        Logger.Instance.ILog("DockerMods: \n" + content);
        File.Delete(output);
    }

    /// <summary>
    /// Tests a connection to a database
    /// </summary>
    private Result<bool> CanConnectToDatabase()
        => MigrationManager.CanConnect(appSettingsService.Settings.DatabaseType,
            appSettingsService.Settings.DatabaseConnection);

    /// <summary>
    /// Checks if the database exists
    /// </summary>
    /// <returns>true if it exists, otherwise false</returns>
    private Result<bool> DatabaseExists()
        => MigrationManager.DatabaseExists(appSettingsService.Settings.DatabaseType,
            appSettingsService.Settings.DatabaseConnection);

    /// <summary>
    /// Creates the database
    /// </summary>
    /// <returns>true if it exists, otherwise false</returns>
    private Result<bool> CreateDatabase()
        => MigrationManager.CreateDatabase(Logger.Instance, appSettingsService.Settings.DatabaseType,
            appSettingsService.Settings.DatabaseConnection);

    /// <summary>
    /// Backups the database file if using SQLite and not migrating
    /// </summary>
    private void BackupSqlite()
    {
        if (appSettingsService.Settings.DatabaseType != DatabaseType.Sqlite)
            return;
        if (appSettingsService.Settings.DatabaseMigrateType != null)
            return;
        try
        {
            string dbfile = Path.Combine(DirectoryHelper.DatabaseDirectory, "FileFlows.sqlite");
            if (File.Exists(dbfile))
                File.Copy(dbfile, dbfile + ".backup", true);
        }
        catch (Exception ex)
        {
            Logger.Instance.WLog("Failed to backup SQLite database file: " + ex.Message);
        }
    }

    /// <summary>
    /// Sends a message update
    /// </summary>
    /// <param name="message">the message</param>
    void UpdateStatus(string message)
    {
        Logger.Instance.ILog(message);
        OnStatusUpdate?.Invoke(message);
    }
    

    /// <summary>
    /// Checks the license key
    /// </summary>
    void CheckLicense()
    {
        LicenseHelper.Update().Wait();
    }
    
    /// <summary>
    /// Clean the default temp directory on startup
    /// </summary>
    private void CleanDefaultTempDirectory()
    {
        UpdateStatus("Cleaning temporary directory");
        
        string tempDir = Application.Docker
            ? Path.Combine(DirectoryHelper.DataDirectory, "temp") // legacy reasons docker uses lowercase temp
            : Path.Combine(DirectoryHelper.BaseDirectory, "Temp");
        DirectoryHelper.CleanDirectory(tempDir);
    }

    /// <summary>
    /// Runs an upgrade
    /// </summary>
    /// <returns>the upgrade result</returns>
    Result<bool> Upgrade()
    {
        string error;
        var upgrader = new Upgrade.Upgrader();
        var upgradeRequired = upgrader.UpgradeRequired(appSettingsService.Settings);
        if (upgradeRequired.Failed(out error))
            return Result<bool>.Fail(error);

        bool needsUpgrade = upgradeRequired.Value.Required; 

        if (needsUpgrade == false)
            return true;
        
        
        UpdateStatus("Backing up old database...");
        upgrader.Backup(upgradeRequired.Value.Current, appSettingsService.Settings);
        
        UpdateStatus("Upgrading Please Wait...");
        var upgradeResult = upgrader.Run(upgradeRequired.Value.Current, appSettingsService);
        if(upgradeResult.Failed(out error))
            return Result<bool>.Fail(error);
        
        
        return true;
    }

    /// <summary>
    /// Updates the templates
    /// </summary>
    private void UpdateTemplates()
    {
        var libraryTemplates = TemplateLoader.GetLibraryTemplates();
        if (libraryTemplates?.Any() == true)
        {
            Logger.Instance.ILog("Extracting Library Templates");
            foreach (var template in libraryTemplates)
            {
                Logger.Instance.ILog("Embedded Library Template: " + GetShortName(template));
                TemplateLoader.ExtractTo(template, DirectoryHelper.TemplateDirectoryLibrary);
            }
        }
        else
        {
            Logger.Instance.WLog("No embedded library templates found");
        }

        // delete the old flow template files
        var oldFlowTemplates = Directory.GetFiles(DirectoryHelper.TemplateDirectoryFlow, "Templates_*.json");
        foreach(var file in oldFlowTemplates)
             File.Delete(file);
        
        var flowTemplates = TemplateLoader.GetFlowTemplates();
        if (flowTemplates?.Any() == true)
        {
            Logger.Instance.ILog("Extracting Flow Templates");
            foreach (var template in flowTemplates)
            {
                Logger.Instance.ILog("Embedded Flow Template: " + GetShortName(template));
                TemplateLoader.ExtractTo(template, DirectoryHelper.TemplateDirectoryFlow);
            }
        }
        else
        {
            Logger.Instance.WLog("No embedded flow templates found");
        }

        string GetShortName(string path)
        {
            int index = path.IndexOf("DefaultTemplates.");
            return path[(index + "DefaultTemplates.".Length)..];
        }
    }

    /// <summary>
    /// Prepares the database
    /// </summary>
    /// <returns>the result</returns>
    Result<bool>PrepareDatabase()
    {
        UpdateStatus("Initializing database...");

        string error;
        
        var service = ServiceLoader.Load<DatabaseService>();

        if (service.MigrateRequired())
        {
            UpdateStatus("Migrating database, please wait this may take a while.");
            if (service.MigrateDatabase().Failed(out error))
                return Result<bool>.Fail(error);
        }
        
        if (service.PrepareDatabase().Failed(out error))
            return Result<bool>.Fail(error);

        return true;
    }


    /// <summary>
    /// Sets the version in the database
    /// </summary>
    /// <returns>true if successful</returns>
    private Result<bool> SetVersion()
    {
        try
        {
            var service = ServiceLoader.Load<DatabaseService>();
            service.SetVersion().Wait();
            return true;
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex.Message);
        }
    }

}