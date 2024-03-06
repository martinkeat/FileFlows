using System.Text;
using System.Text.Json;
using FileFlows.DataLayer.DatabaseConnectors;
using FileFlows.DataLayer.Converters;
using FileFlows.DataLayer.Helpers;
using FileFlows.DataLayer.Models;
using FileFlows.Shared;
using FileFlows.Shared.Models;

namespace FileFlows.DataLayer;

/// <summary>
/// Manages data access operations for the DbStatistic table
/// </summary>
public class DbStatisticManager
{
    /// <summary>
    /// The database connector
    /// </summary>
    private readonly IDatabaseConnector DbConnector;
    /// <summary>
    /// The type of database
    /// </summary>
    private readonly DatabaseType DbType; 
    
    /// <summary>
    /// Initializes a new instance of the DbStatistic manager
    /// </summary>
    /// <param name="dbType">the type of database</param>
    /// <param name="dbConnector">the database connector</param>
    public DbStatisticManager(DatabaseType dbType, IDatabaseConnector dbConnector)
    {
        DbType = dbType;
        DbConnector = dbConnector;
    }


    /// <summary>
    /// Inserts a new DbStatistic
    /// </summary>
    /// <param name="dbStatistic">the new DbStatistic</param>
    internal async Task Insert(DbStatistic dbStatistic)
    {
        using var db = await DbConnector.GetDb(write: true);
        await db.Db.InsertAsync(dbStatistic);
    }


    /// <summary>
    /// Fetches all items
    /// </summary>
    /// <returns>the items</returns>
    internal async Task<List<DbStatistic>> GetAll()
    {
        using var db = await DbConnector.GetDb();
        return (await db.Db.FetchAsync<DbStatistic>()).ToList();
    }

}