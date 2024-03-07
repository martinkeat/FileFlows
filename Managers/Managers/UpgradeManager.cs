using FileFlows.Managers.Upgrades;

namespace FileFlows.Managers;

/// <summary>
/// Manager used by upgrade scripts to perform operations on the database
/// </summary>
public class UpgradeManager
{
    /// <summary>
    /// Run upgrade from 24.03.2
    /// </summary>
    public void Run_Upgrade_24_03_2(Settings settings)
    {
        new Upgrade_24_03_2().Run(settings);
    }
}