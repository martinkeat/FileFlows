using FileFlows.DataLayer.DatabaseConnectors;
using FileFlows.DataLayer.Models;
using FileFlows.Plugin;
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
    public Task Insert(DbStatistic dbStatistic)
        => InsertBulk(dbStatistic);
    
    
    /// <summary>
    /// Bulk insert many statistics
    /// </summary>
    /// <param name="stats">the stats to insert</param>
    public async Task InsertBulk(params DbStatistic[] stats)
    {
        using var db = await DbConnector.GetDb(write: true);
        db.Db.BeginTransaction();
        foreach (var stat in stats)
        {
            string sql = "insert into " + Wrap(nameof(DbStatistic)) + " ( " +
                         Wrap(nameof(stat.Name)) + ", " +
                         Wrap(nameof(stat.Data)) + ") " +
                         " values (@0, @1)";
            await db.Db.ExecuteAsync(sql, stat.Name, stat.Data);
        }
        db.Db.CompleteTransaction();
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
    public async Task<DbStatistic> GetStatisticByName(string name)
    {
        using var db = await DbConnector.GetDb();
        return await db.Db.SingleOrDefaultAsync<DbStatistic>
            ("where " + Wrap(nameof(DbStatistic.Name)) + " = @0", name);
    }

    /// <summary>
    /// Clears DbStatistics based on specified conditions.
    /// </summary>
    /// <param name="name">Optional. The name for which DbStatistics should be cleared.</param>
    public async Task Clear(string? name = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            Logger.ILog("Deleting ALL DbStatistics");
            using var db = await DbConnector.GetDb();
            await db.Db.ExecuteAsync("delete from " + Wrap(nameof(DbStatistic)));
        }
        else
        {
            string whereClause = "";

            if (string.IsNullOrWhiteSpace(name) == false)
                whereClause += (string.IsNullOrWhiteSpace(whereClause) ? " " : " AND ") + Wrap(nameof(DbStatistic.Name)) + " = @0";

            Logger.ILog(
                $"Deleting DbStatistics{(!string.IsNullOrWhiteSpace(whereClause) ? $" with conditions: {whereClause}" : "")}");
            using var db = await DbConnector.GetDb();
            await db.Db.ExecuteAsync("delete from " + Wrap(nameof(DbStatistic)) + " where " + whereClause, name);
        }
    }
}