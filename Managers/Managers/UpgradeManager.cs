using FileFlows.DataLayer.Upgrades;
using FileFlows.Plugin;

namespace FileFlows.Managers;

/// <summary>
/// Manager used by upgrade scripts to perform operations on the database
/// </summary>
public class UpgradeManager
{
    /// <summary>
    /// Run upgrade from 24.03.2
    /// </summary>
    public void Run_Upgrade_24_03_2(ILogger logger, DatabaseType dbType, string connectionString)
    {
        new Upgrade_24_03_2().Run(logger, dbType, connectionString);
    }
}