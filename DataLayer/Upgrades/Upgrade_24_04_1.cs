using FileFlows.DataLayer.DatabaseConnectors;
using FileFlows.DataLayer.Models;
using FileFlows.Plugin;
using FileFlows.Shared.Models;
using DatabaseType = FileFlows.Shared.Models.DatabaseType;
using ILogger = FileFlows.Plugin.ILogger;

namespace FileFlows.DataLayer.Upgrades;

/// <summary>
/// Upgrades for 24.04.1
/// </summary>
public class Upgrade_24_04_1
{
    /// <summary>
    /// Run the upgrade
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="dbType">the database type</param>
    /// <param name="connectionString">the database connection string</param>
    /// <returns>the upgrade result</returns>
    public Result<bool> Run(ILogger logger, DatabaseType dbType, string connectionString)
    {
        var connector = DatabaseConnectorLoader.LoadConnector(logger, dbType, connectionString);
        using var db = connector.GetDb(true).Result;

        AddAuditTable(logger, db, connector);

        return true;
    }


    /// <summary>
    /// Adds the audit table
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="db">the db connection</param>
    /// <param name="connector">the connector</param>
    private void AddAuditTable(ILogger logger, DatabaseConnection db, IDatabaseConnector connector)
    {
        if (connector.Type == DatabaseType.Sqlite)
            AddAuditTable_Sqlite(logger, db, connector);
    }


    /// <summary>
    /// Adds the audit table for SQLite
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="db">the db connection</param>
    /// <param name="connector">the connector</param>
    private void AddAuditTable_Sqlite(ILogger logger, DatabaseConnection db, IDatabaseConnector connector)
    {
        string sql = @"
CREATE TABLE AuditLog
(
    OperatorUid     VARCHAR(36)        NOT NULL,
    OperatorName    VARCHAR(255)       NOT NULL,
    OperatorType    INT                NOT NULL,
    IPAddress       VARCHAR(50)        NOT NULL,
    LogDate         datetime,
    Action          INT                NOT NULL,
    ObjectType      VARCHAR(255)       NOT NULL,
    ObjectUid       VARCHAR(36)        NOT NULL,
    Parameters      TEXT               NOT NULL,
    RevisionUid     VARCHAR(36)        NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_AuditLog_OperatorUid ON AuditLog (OperatorUid);
CREATE INDEX IF NOT EXISTS idx_AuditLog_LogDate ON AuditLog (LogDate);
";
        db.Db.Execute(sql);
    }

}