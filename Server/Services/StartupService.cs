using FileFlows.Plugin;
using FileFlows.Server.Helpers;
using Microsoft.AspNetCore.Components;

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

            if (Upgrade().Failed(out error))
            {
                UpdateStatus(error);
                return Result<bool>.Fail(error);
            }

            if (PrepareDatabase().Failed(out error))
            {
                UpdateStatus(error);
                return Result<bool>.Fail(error);
            }

            return true;
        }
        catch (Exception ex)
        {
            #if(DEBUG)
            UpdateStatus("Startup failure: " + ex.Message + Environment.NewLine + ex.StackTrace);
            #else
            UpdateStatus("Startup failure: " + ex.Message);
            #endif
            return Result<bool>.Fail(ex.Message);
        }
    }

    /// <summary>
    /// Sends a message update
    /// </summary>
    /// <param name="message">the message</param>
    void UpdateStatus(string message)
    {
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
        
        UpdateStatus("Upgrading Please Wait...");
        var upgradeResult = upgrader.Run(upgradeRequired.Value.Current, appSettingsService);
        if(upgradeResult.Failed(out error))
            return Result<bool>.Fail(error);
        
        return true;
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

}