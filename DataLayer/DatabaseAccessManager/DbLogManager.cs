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
/// Manages data access operations for the DbLogMessage table
/// </summary>
internal  class DbLogMessageManager : BaseManager
{
    /// <summary>
    /// Initializes a new instance of the DbLogMessage manager
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="dbType">the type of database</param>
    /// <param name="dbConnector">the database connector</param>
    public DbLogMessageManager(ILogger logger, DatabaseType dbType, IDatabaseConnector dbConnector) : base(logger, dbType, dbConnector) {
    }

    /// <summary>
    /// Inserts a new DbLogMessage
    /// </summary>
    /// <param name="dbLogMessage">the new DbLogMessage</param>
    internal async Task Insert(DbLogMessage dbLogMessage)
    {
        using var db = await DbConnector.GetDb(write: true);
        await db.Db.InsertAsync(dbLogMessage);
    }


    /// <summary>
    /// Fetches all items
    /// </summary>
    /// <returns>the items</returns>
    internal async Task<List<DbLogMessage>> GetAll()
    {
        using var db = await DbConnector.GetDb();
        return (await db.Db.FetchAsync<DbLogMessage>()).ToList();
    }

}