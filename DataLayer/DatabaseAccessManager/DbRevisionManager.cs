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
/// Manages data access operations for the RevisionedObject table
/// </summary>
internal  class DbRevisionManager : BaseManager
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


    /// <summary>
    /// Gets all revisions for a given object
    /// </summary>
    /// <param name="uid">the UID to get all revisions for</param>
    /// <returns>the items</returns>
    internal async Task<List<RevisionedObject>> GetAll(Guid uid)
    {
        using var db = await DbConnector.GetDb();
        return (await db.Db.FetchAsync<RevisionedObject>($"where {Wrap(nameof(RevisionedObject.Uid))} = '{uid}'" +
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
        string sql = "select distinct " +
                     Wrap(nameof(RevisionedObject.RevisionUid)) + ", " +
                     Wrap(nameof(RevisionedObject.RevisionUid)) + ", " +
                     Wrap(nameof(RevisionedObject.Uid)) + ", " +
                     Wrap(nameof(RevisionedObject.RevisionType)) + ", " +
                     Wrap(nameof(RevisionedObject.RevisionName)) + ", " +
                     Wrap(nameof(RevisionedObject.RevisionDate)) + "  " +
                     " from " + Wrap(nameof(RevisionedObject)) + " " +
                     " group by " +
                     Wrap(nameof(RevisionedObject.RevisionUid)) +
                     " order by " +
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

        string strUids = string.Join(",", uids.Select(x => "'" + x.ToString() + "'"));
        
        using var db = await DbConnector.GetDb(write: true);
        await db.Db.ExecuteAsync($"delete from {Wrap(nameof(RevisionedObject))}" +
                                 $" where {Wrap(nameof(RevisionedObject.Uid))} in ({strUids})");
    }

    /// <summary>
    /// Gets a specific revision
    /// </summary>
    /// <param name="uid">The UID of the object</param>
    /// <param name="revisionUid">the UID of the revision</param>
    /// <returns>The specific revision</returns>
    public async Task<RevisionedObject> Get(Guid uid, Guid revisionUid)
    {
        using var db = await DbConnector.GetDb();
        return await db.Db.SingleAsync<RevisionedObject>(
            $"where {Wrap(nameof(RevisionedObject.RevisionUid))} = '{revisionUid}' and {Wrap(nameof(RevisionedObject.Uid))} = '{uid}'");
    }
}