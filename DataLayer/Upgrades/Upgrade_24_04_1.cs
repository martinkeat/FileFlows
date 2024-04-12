using FileFlows.DataLayer.DatabaseConnectors;
using FileFlows.Plugin;
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

        DeleteOldLibraryFiles(logger, db, connector);
        AddAuditTable(logger, db, connector);

        return true;
    }


    /// <summary>
    /// Deletes rogue library files
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="db">the db connection</param>
    /// <param name="connector">the connector</param>
    private void DeleteOldLibraryFiles(ILogger logger, DatabaseConnection db, IDatabaseConnector connector)
    {
        try
        {
            string sql;
            if (connector.Type == DatabaseType.Postgres)
            {
                sql = $@"DELETE FROM {connector.WrapFieldName("LibraryFile")}
                WHERE {connector.WrapFieldName("LibraryUid")} NOT IN (
                    SELECT CAST({connector.WrapFieldName("Uid")} AS UUID)
                    FROM {connector.WrapFieldName("DbObject")}
                    WHERE {connector.WrapFieldName("Type")} LIKE '%.Library'
                )";
            }
            else
            {
                sql = $@"delete 
from {connector.WrapFieldName("LibraryFile")} 
where {connector.WrapFieldName("LibraryUid")} not in (
    select {connector.WrapFieldName("Uid")}
    from {connector.WrapFieldName("DbObject")} 
    where {connector.WrapFieldName("Type")} like '%.Library'
)";
            }

            db.Db.Execute(sql);
        }
        catch (Exception)
        {
        }
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
        else if(connector.Type == DatabaseType.Postgres)
            AddAuditTable_Postgres(logger, db, connector);
        else if(connector.Type == DatabaseType.MySql)
            AddAuditTable_MySql(logger, db, connector);
        else if (connector.Type == DatabaseType.SqlServer)
            AddAuditTable_SqlServer(logger, db, connector);
        else
            throw new Exception("Invalid database type: " + connector.Type); // shouldnt happen
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
    RevisionUid     VARCHAR(36)        NOT NULL,
    Parameters      TEXT               NOT NULL,
    Changes         TEXT               NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_AuditLog_OperatorUid ON AuditLog (OperatorUid);
CREATE INDEX IF NOT EXISTS idx_AuditLog_LogDate ON AuditLog (LogDate);
";
        db.Db.Execute(sql);
    }


    /// <summary>
    /// Adds the audit table for MySql
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="db">the db connection</param>
    /// <param name="connector">the connector</param>
    private void AddAuditTable_MySql(ILogger logger, DatabaseConnection db, IDatabaseConnector connector)
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
    RevisionUid     VARCHAR(36)        NOT NULL,
    Parameters      TEXT               NOT NULL,
    Changes         TEXT               NOT NULL
);

ALTER TABLE AuditLog ADD INDEX (OperatorUid);
ALTER TABLE AuditLog ADD INDEX (LogDate);
";
        db.Db.Execute(sql);
    }
    
    /// <summary>
    /// Adds the audit table for Postgres
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="db">the db connection</param>
    /// <param name="connector">the connector</param>
    private void AddAuditTable_Postgres(ILogger logger, DatabaseConnection db, IDatabaseConnector connector)
    {
        string sql = @"

CREATE TABLE ""AuditLog""
(
    ""OperatorUid""     VARCHAR(36)        NOT NULL,
    ""OperatorName""    VARCHAR(255)       NOT NULL,
    ""OperatorType""    INT                NOT NULL,
    ""IPAddress""       VARCHAR(50)        NOT NULL,
    ""LogDate""         TIMESTAMP          DEFAULT CURRENT_TIMESTAMP,    
    ""Action""          INT                NOT NULL,
    ""ObjectType""      VARCHAR(255)       NOT NULL,
    ""ObjectUid""       VARCHAR(36)        NOT NULL,
    ""RevisionUid""     VARCHAR(36)        NOT NULL,
    ""Parameters""      TEXT               NOT NULL,
    ""Changes""         TEXT               NOT NULL
);
CREATE INDEX ON ""AuditLog"" (""OperatorUid"");
CREATE INDEX ON ""AuditLog"" (""LogDate"");
";
        db.Db.Execute(sql);
    }
    
    /// <summary>
    /// Adds the audit table for SqlServer
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="db">the db connection</param>
    /// <param name="connector">the connector</param>
    private void AddAuditTable_SqlServer(ILogger logger, DatabaseConnection db, IDatabaseConnector connector)
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
    RevisionUid     VARCHAR(36)        NOT NULL,
    Parameters      NVARCHAR(MAX)      NOT NULL,
    Changes         NVARCHAR(MAX)      NOT NULL
);

CREATE INDEX ix_AuditLog_OperatorUid ON AuditLog (OperatorUid);
CREATE INDEX ix_AuditLog_LogDate ON AuditLog (LogDate);
";
        db.Db.Execute(sql);
    }

}