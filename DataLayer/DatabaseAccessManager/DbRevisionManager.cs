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
/// Manages data access operations for the RevisionedObject table
/// </summary>
public class DbRevisionManager
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
    /// Initializes a new instance of the DbRevision manager
    /// </summary>
    /// <param name="dbType">the type of database</param>
    /// <param name="dbConnector">the database connector</param>
    public DbRevisionManager(DatabaseType dbType, IDatabaseConnector dbConnector)
    {
        DbType = dbType;
        DbConnector = dbConnector;
    }


    /// <summary>
    /// Inserts a new revision
    /// </summary>
    /// <param name="dbRevision">the new revision</param>
    internal async Task Insert(RevisionedObject dbRevision)
    {
        using var db = await DbConnector.GetDb(write: true);
        await db.Db.InsertAsync(dbRevision);
    }


    /// <summary>
    /// Fetches all items
    /// </summary>
    /// <returns>the items</returns>
    internal async Task<List<RevisionedObject>> GetAll()
    {
        using var db = await DbConnector.GetDb();
        return (await db.Db.FetchAsync<RevisionedObject>()).ToList();
    }

}