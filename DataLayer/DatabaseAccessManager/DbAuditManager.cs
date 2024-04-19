using System.Text.Json;
using FileFlows.DataLayer.DatabaseConnectors;
using FileFlows.Plugin;
using FileFlows.Shared.Models;
using NPoco.Expressions;

namespace FileFlows.DataLayer;

/// <summary>
/// Manages data access operations for the audit table
/// </summary>
internal class DbAuditManager : BaseManager
{
    /// <summary>
    /// Initializes a new instance of the DbAudit manager
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="dbType">the type of database</param>
    /// <param name="dbConnector">the database connector</param>
    public DbAuditManager(ILogger logger, DatabaseType dbType, IDatabaseConnector dbConnector) : base(logger, dbType, dbConnector)
    {
    }

    /// <summary>
    /// Gets or sets if audits should be performed
    /// </summary>
    public static bool PerformAudits { get; set; }

    /// <summary>
    /// Inserts a new audit entry
    /// </summary>
    /// <param name="entry">the new audit entry</param>
    public async Task Insert(AuditEntry entry)
    {
        if (PerformAudits == false)
            return;
        if (entry.LogDate < new DateTime(2020, 1, 1))
            entry.LogDate = DateTime.UtcNow;
        if (string.IsNullOrWhiteSpace(entry.OperatorName) || string.IsNullOrWhiteSpace(entry.IPAddress) ||
            string.IsNullOrWhiteSpace(entry.ObjectType))
            return;
        
        string sql = "insert into " + Wrap("AuditLog") + " ( " +
                     Wrap(nameof(entry.OperatorUid)) + ", " +
                     Wrap(nameof(entry.OperatorName)) + ", " +
                     Wrap(nameof(entry.OperatorType)) + ", " +
                     Wrap(nameof(entry.IPAddress)) + ", " +
                     Wrap(nameof(entry.Action)) + ", " +
                     Wrap(nameof(entry.ObjectType)) + ", " +
                     Wrap(nameof(entry.ObjectUid)) + ", " +
                     Wrap(nameof(entry.RevisionUid)) + ", " +
                     Wrap(nameof(entry.Parameters)) + ", " +
                     Wrap(nameof(entry.Changes)) + ", " +
                     Wrap(nameof(entry.LogDate)) + ") " +
                     " values (@0, @1, @2, @3, @4, @5, @6, @7, @8, @9, " +
                     DbConnector.FormatDateQuoted(entry.LogDate) + ")";
        try
        {
            using var db = await DbConnector.GetDb();
            await db.Db.ExecuteAsync(sql,
                entry.OperatorUid.ToString(),
                entry.OperatorName,
                (int)entry.OperatorType,
                entry.IPAddress,
                (int)entry.Action,
                entry.ObjectType,
                entry.ObjectUid.ToString(),
                entry.RevisionUid?.ToString() ?? string.Empty,
                entry.Parameters?.Any() != true ? string.Empty : JsonSerializer.Serialize(entry.Parameters),
                entry.Changes?.Any() != true ? string.Empty : JsonSerializer.Serialize(entry.Changes)
            );
        }
        catch (Exception ex)
        {
            Logger.ELog("Failed to insert audit: " + ex.Message);
        }
    }


    /// <summary>
    /// Performs a search of the audit log
    /// </summary>
    /// <param name="filter">the search filter</param>
    /// <returns>the result</returns>
    public async Task<List<AuditEntry>> Search(AuditSearchFilter filter)
    {
        if (PerformAudits == false)
            return new();
        string sql = "select * from " + Wrap("AuditLog");
        sql += $" order by {Wrap(nameof(AuditEntry.LogDate))} desc ";
        int limit = 1000;
        sql += DbType switch
        {
            DatabaseType.MySql => $" LIMIT 0, {limit}",
            DatabaseType.Postgres => $" OFFSET 0 LIMIT {limit}",
            DatabaseType.Sqlite => $" LIMIT {limit} OFFSET 0",
            DatabaseType.SqlServer => $" OFFSET {0} ROWS FETCH NEXT {limit} ROWS ONLY",
            _ => string.Empty
        };
        using var db = await DbConnector.GetDb();
        return await db.Db.FetchAsync<AuditEntry>(sql);
    }

    /// <summary>
    /// Gets the audit history for a specific object
    /// </summary>
    /// <param name="type">the type of object</param>
    /// <param name="uid">the UID of the object</param>
    /// <returns>the audit history of the object</returns>
    public async Task<List<AuditEntry>> ObjectHistory(string type, Guid uid)
    {
        if (PerformAudits == false)
            return new();
        
        string sql = "select * from " + Wrap("AuditLog");
        sql += $" where {Wrap(nameof(AuditEntry.ObjectType))} = @0 and {Wrap(nameof(AuditEntry.ObjectUid))} = '{uid}'"; 
        sql += $" order by {Wrap(nameof(AuditEntry.LogDate))} desc ";
        int limit = 100;
        sql += DbType switch
        {
            DatabaseType.MySql => $" LIMIT 0, {limit}",
            DatabaseType.Postgres => $" OFFSET 0 LIMIT {limit}",
            DatabaseType.Sqlite => $" LIMIT {limit} OFFSET 0",
            DatabaseType.SqlServer => $" OFFSET {0} ROWS FETCH NEXT {limit} ROWS ONLY",
            _ => string.Empty
        };
        using var db = await DbConnector.GetDb();
        return await db.Db.FetchAsync<AuditEntry>(sql, type);
    }
}