using System.Text;
using System.Text.Json;
using FileFlows.DataLayer.DatabaseConnectors;
using FileFlows.DataLayer.Helpers;
using FileFlows.Plugin;
using FileFlows.ServerShared;
using FileFlows.ServerShared.Helpers;
using FileFlows.ServerShared.Models.StatisticModels;
using FileFlows.Shared.Models;

namespace FileFlows.DataLayer.Upgrades;

/// <summary>
/// Upgrades for 24.02
/// </summary>
public class Upgrade_24_02
{
    /// <summary>
    /// Run the upgrade
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="dbType">the database type</param>
    /// <param name="connectionString">the database connection string</param>
    /// <returns>the upgrade result</returns>
    public async Task<Result<bool>> Run(ILogger logger, DatabaseType dbType, string connectionString)
    {
        var connector = DatabaseConnectorLoader.LoadConnector(logger, dbType, connectionString);
        if (await connector.ColumnExists("LibraryFile", "FailureReason") == false)
        {
            logger.ILog("Adding LibraryFile.FailureReason column");
            await connector.CreateColumn("LibraryFile", "FailureReason", "VARCHAR(512)", "''");
        }

        if (await connector.ColumnExists("LibraryFile", "ProcessOnNodeUid") == false)
        {
            logger.ILog("Adding LibraryFile.ProcessOnNodeUid column");
            await connector.CreateColumn("LibraryFile", "ProcessOnNodeUid", "varchar(36)", "''");
        }

        return true;
    }
}