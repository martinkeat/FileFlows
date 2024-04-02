using FileFlows.DataLayer.DatabaseConnectors;
using FileFlows.Plugin;
using FileFlows.Shared.Models;

namespace FileFlows.DataLayer;

/// <summary>
/// Manages data access operations for the RevisionedObject table
/// </summary>
internal class DbRevisionManager : BaseManager
{
    /// <summary>
    /// Initializes a new instance of the DbRevision manager
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="dbType">the type of database</param>
    /// <param name="dbConnector">the database connector</param>
    public DbRevisionManager(ILogger logger, DatabaseType dbType, IDatabaseConnector dbConnector) : base(logger, dbType, dbConnector)
    {
    }


    /// <summary>
    /// Inserts a new revision
    /// </summary>
    /// <param name="dbRevision">the new revision</param>
    internal Task Insert(RevisionedObject dbRevision)
        => InsertBulk(dbRevision);

    
    /// <summary>
    /// Bulk insert many revisions
    /// </summary>
    /// <param name="items">the revisions to insert</param>
    public async Task InsertBulk(params RevisionedObject[] items)
    {
        using var db = await DbConnector.GetDb(write: true);
        db.Db.BeginTransaction();
        foreach (var item in items)
        {
            if (item.Uid == Guid.Empty)
                item.Uid = Guid.NewGuid();
            
            string sql = "insert into " + Wrap(nameof(RevisionedObject)) + " ( " +
                         Wrap(nameof(item.Uid)) + ", " +
                         Wrap(nameof(item.RevisionUid)) + ", " +
                         Wrap(nameof(item.RevisionName)) + ", " +
                         Wrap(nameof(item.RevisionType)) + ", " +
                         Wrap(nameof(item.RevisionDate)) + ", " +
                         Wrap(nameof(item.RevisionCreated)) + ", " +
                         Wrap(nameof(item.RevisionData)) + ") " +
                         " values (" +
                         $"'{item.Uid}', " +
                         $"'{item.RevisionUid}', " +
                         "@0, " +
                         "@1, " +
                         DbConnector.FormatDateQuoted(item.RevisionDate) + ", " +
                         DbConnector.FormatDateQuoted(item.RevisionCreated) + ", " +
                         "@2)";
            await db.Db.ExecuteAsync(sql, item.RevisionName, item.RevisionType, item.RevisionData);
        }
        db.Db.CompleteTransaction();
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

    /// <summary>
    /// Gets all revisions for a given object
    /// </summary>
    /// <param name="uid">the UID to get all revisions for</param>
    /// <returns>the items</returns>
    internal async Task<List<RevisionedObject>> GetAll(Guid uid)
    {
        using var db = await DbConnector.GetDb();
        return (await db.Db.FetchAsync<RevisionedObject>($"where {Wrap(nameof(RevisionedObject.RevisionUid))} = '{uid}'" +
                                                         $" order by {Wrap(nameof(RevisionedObject.RevisionDate))} desc"))
            .ToList();
    }
    
    /// <summary>
    /// Get latest revisions for all objects
    /// </summary>
    /// <returns>A list of latest revisions for all objects</returns>
    internal async Task<List<RevisionedObject>> ListAll()
    {
        using var db = await DbConnector.GetDb();
        string sql = "SELECT " +
                     "DISTINCT " +
                     Wrap(nameof(RevisionedObject.RevisionUid)) + ", " +
                     Wrap(nameof(RevisionedObject.Uid)) + ", " +
                     Wrap(nameof(RevisionedObject.RevisionType)) + ", " +
                     Wrap(nameof(RevisionedObject.RevisionName)) + ", " +
                     Wrap(nameof(RevisionedObject.RevisionDate)) + " " +
                     "FROM " +
                     Wrap(nameof(RevisionedObject)) + " " +
                     "WHERE " +
                     "(" + Wrap(nameof(RevisionedObject.RevisionUid)) + ", " +
                     Wrap(nameof(RevisionedObject.RevisionDate)) + ") IN (" +
                     "SELECT " +
                     Wrap(nameof(RevisionedObject.RevisionUid)) + ", " +
                     "MAX(" + Wrap(nameof(RevisionedObject.RevisionDate)) + ") " +
                     "FROM " +
                     Wrap(nameof(RevisionedObject)) + " " +
                     "GROUP BY " +
                     Wrap(nameof(RevisionedObject.RevisionUid)) +
                     ") " +
                     "ORDER BY " +
                     Wrap(nameof(RevisionedObject.RevisionType)) + ", " +
                     Wrap(nameof(RevisionedObject.RevisionName));


        return await db.Db.FetchAsync<RevisionedObject>(sql);
    }
    
    /// <summary>
    /// Delete items from a database
    /// </summary>
    /// <param name="uids">the UIDs of the items to delete</param>
    public async Task Delete(params Guid[] uids)
    {
        if (uids?.Any() != true)
            return; // nothing to delete

        string strUids = string.Join(",", uids.Select(x => "'" + x + "'"));
        
        using var db = await DbConnector.GetDb(write: true);
        await db.Db.ExecuteAsync($"delete from {Wrap(nameof(RevisionedObject))}" +
                                 $" where {Wrap(nameof(RevisionedObject.Uid))} in ({strUids})");
    }

    /// <summary>
    /// Gets a specific revision
    /// </summary>
    /// <param name="uid">The UID of the revision object</param>
    /// <param name="dboUid">the UID of the DbObject</param>
    /// <returns>The specific revision</returns>
    public async Task<RevisionedObject?> Get(Guid uid, Guid dboUid)
    {
        string sql = "select * from " + Wrap(nameof(RevisionedObject)) +
                     $" where {Wrap(nameof(RevisionedObject.Uid))} = '{uid}' " +
                     $" and {Wrap(nameof(RevisionedObject.RevisionUid))} = '{dboUid}'";
        using var db = await DbConnector.GetDb();
        return await db.Db.FirstOrDefaultAsync<RevisionedObject>(sql);
    }
}