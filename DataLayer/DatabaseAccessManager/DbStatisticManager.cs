using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;
using FileFlows.DataLayer.DatabaseConnectors;
using FileFlows.DataLayer.Converters;
using FileFlows.DataLayer.Helpers;
using FileFlows.DataLayer.Models;
using FileFlows.Plugin;
using FileFlows.Shared;
using FileFlows.Shared.Models;

namespace FileFlows.DataLayer;

/// <summary>
/// Manages data access operations for the DbStatistic table
/// </summary>
internal  class DbStatisticManager : BaseManager
{
    
    /// <summary>
    /// Initializes a new instance of the DbStatistic manager
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="dbType">the type of database</param>
    /// <param name="dbConnector">the database connector</param>
    public DbStatisticManager(ILogger logger, DatabaseType dbType, IDatabaseConnector dbConnector) : base(logger, dbType, dbConnector)
    {
    }


    /// <summary>
    /// Inserts a new DbStatistic
    /// </summary>
    /// <param name="dbStatistic">the new DbStatistic</param>
    public async Task Insert(DbStatistic dbStatistic)
    {
        using var db = await DbConnector.GetDb(write: true);
        await db.Db.InsertAsync(dbStatistic);
    }


    /// <summary>
    /// Fetches all items
    /// </summary>
    /// <returns>the items</returns>
    public async Task<List<DbStatistic>> GetAll()
    {
        using var db = await DbConnector.GetDb();
        return (await db.Db.FetchAsync<DbStatistic>()).ToList();
    }

    /// <summary>
    /// Gets statistics by name
    /// </summary>
    /// <returns>the matching statistics</returns>
    public async Task<List<DbStatistic>> GetStatisticsByName(string name)
    {
        using var db = await DbConnector.GetDb();
        return (await db.Db.FetchAsync<DbStatistic>("where " + Wrap(nameof(DbStatistic.Name)) + " = @0", name)).ToList();
    }

    /// <summary>
    /// Clears DbStatistics based on specified conditions.
    /// </summary>
    /// <param name="name">Optional. The name for which DbStatistics should be cleared.</param>
    /// <param name="before">Optional. The date before which DbStatistics should be cleared.</param>
    /// <param name="after">Optional. The date after which DbStatistics should be cleared.</param>
    public async Task Clear(string? name = null, DateTime? before = null, DateTime? after = null)
    {
        if (string.IsNullOrWhiteSpace(name) && before == null && after == null)
        {
            Logger.ILog("Deleting ALL DbStatistics");
            using var db = await DbConnector.GetDb();
            await db.Db.ExecuteAsync("delete from " + Wrap(nameof(DbStatistic)));
        }
        else
        {
            string whereClause = "";

            if (before != null)
                whereClause += " " + Wrap(nameof(DbStatistic.LogDate)) + " < " + DbConnector.FormatDateQuoted(before.Value) + " ";

            if (after != null)
                whereClause += (string.IsNullOrWhiteSpace(whereClause) ? " " : " AND ") + Wrap(nameof(DbStatistic.LogDate)) + " > " + DbConnector.FormatDateQuoted(after.Value) + " ";

            if (string.IsNullOrWhiteSpace(name) == false)
                whereClause += (string.IsNullOrWhiteSpace(whereClause) ? " " : " AND ") + Wrap(nameof(DbStatistic.Name)) + " = @0";

            Logger.ILog(
                $"Deleting DbStatistics{(!string.IsNullOrWhiteSpace(whereClause) ? $" with conditions: {whereClause}" : "")}");
            using var db = await DbConnector.GetDb();
            await db.Db.ExecuteAsync("delete from " + Wrap(nameof(DbStatistic)) + " where " + whereClause, name);
        }
    }
}