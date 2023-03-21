using FileFlows.Server.Helpers;
using FileFlows.Server.Services;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using NPoco;

namespace FileFlows.Server.Controllers;

/// <summary>
/// Revisioned Object controller
/// </summary>
[Route("/api/revision")]
public class RevisionController:Controller
{
    /// <summary>
    /// Get all revisions for an object
    /// </summary>
    /// <param name="uid">The UID of the object</param>
    /// <returns>A list of revisions for an object</returns>
    [HttpGet("{uid}")]
    public async Task<IEnumerable<RevisionedObject>> GetAll([FromRoute] Guid uid)
    {
        if (LicenseHelper.IsLicensed() == false)
            return new RevisionedObject[] { };
        
        var manager = DbHelper.GetDbManager();
        using var db = await manager.GetDb();
        var data = await db.Db.FetchAsync<RevisionedObject>(
            "select Uid, RevisionName, RevisionDate, RevisionType from RevisionedObject where RevisionUid = @0 order by RevisionDate desc", uid);
        return data.ToArray();
    }
    
    /// <summary>
    /// Get latest revisions for all objects
    /// </summary>
    /// <returns>A list of latest revisions for all objects</returns>
    [HttpGet("list")]
    public async Task<IEnumerable<RevisionedObject>> ListAll()
    {
        if (LicenseHelper.IsLicensed() == false)
            return new RevisionedObject[] { };
        
        var manager = DbHelper.GetDbManager();
        using var db = await manager.GetDb();
        var data = await db.Db.FetchAsync<RevisionedObject>(
            "select distinct RevisionUid, Uid, RevisionType, RevisionName, RevisionDate from RevisionedObject group by RevisionUid order by RevisionType, RevisionName");
        return data.ToArray();
    }

    /// <summary>
    /// Gets a specific revision
    /// </summary>
    /// <param name="uid">The UID of the object</param>
    /// <param name="revisionUid">the UID of the revision</param>
    /// <returns>The specific revision</returns>
    [HttpGet("{uid}/revision/{revisionUid}")]
    public async Task<RevisionedObject> GetRevision([FromRoute] Guid uid, [FromRoute] Guid revisionUid)
    {
        if (LicenseHelper.IsLicensed() == false)
            return null;
        var manager = DbHelper.GetDbManager();
        using var db = await manager.GetDb();
        var data = await db.Db.SingleOrDefaultAsync<RevisionedObject>("select * from RevisionedObject where RevisionUid = @0 and Uid = @1", uid, revisionUid);
        return data;
    }

    /// <summary>
    /// Restores a revision
    /// </summary>
    /// <param name="uid">The UID of the object</param>
    /// <param name="revisionUid">the UID of the revision</param>
    [HttpPut("{uid}/restore/{revisionUid}")]
    public async Task Restore([FromRoute] Guid uid, [FromRoute] Guid revisionUid)
    {
        if (LicenseHelper.IsLicensed() == false)
            return;
        
        var revision = await GetRevision(uid, revisionUid);
        if (revision == null)
            throw new Exception("Revision not found");
        // here we run into problems when using sqlite caching...
        // mysql is easy, just update the db, sqlite we have to update db and the cached instance

        var dbo = new DbObject();
        dbo.Data = revision.RevisionData;
        dbo.Name = revision.RevisionName;
        dbo.DateCreated = revision.RevisionCreated;
        dbo.DateModified = revision.RevisionDate;
        dbo.Uid = revision.RevisionUid;
        dbo.Type = revision.RevisionType;
        var manager = DbHelper.GetDbManager();
        await manager.AddOrUpdateDbo(dbo);

        // sqlite.. have to update any in memory objects...
        if (dbo.Type == typeof(Library).FullName)
            new LibraryService().Refresh();
        else if (dbo.Type == typeof(Flow).FullName)
            new FlowService().Refresh();
        else if (dbo.Type == typeof(Dashboard).FullName)
            new DashboardService().Refresh();
    }
    
    
    /// <summary>
    /// Creates an object revision
    /// </summary>
    /// <param name="dbo">the source to revision</param>
    /// <returns>the revisioned object reference</returns>
    internal static RevisionedObject From(DbObject dbo)
    {
        var ro = new RevisionedObject();
        ro.Uid = Guid.NewGuid();
        ro.RevisionDate = dbo.DateModified;
        ro.RevisionCreated = dbo.DateCreated;
        ro.RevisionData = dbo.Data;
        ro.RevisionType = dbo.Type;
        ro.RevisionUid = dbo.Uid;
        ro.RevisionName = dbo.Name;
        return ro;
    }

    /// <summary>
    /// Saves an DbObject revision
    /// </summary>
    /// <param name="dbo">the DbObject to save a revision of</param>
    internal static async Task SaveRevision(DbObject dbo)
    {
        if (LicenseHelper.IsLicensed() == false)
            return;
        
        var ro = From(dbo);
        await Save(ro);
    }
    
    /// <summary>
    /// Saves the revision to the database
    /// </summary>
    /// <param name="ro">the revisioned object to save</param>
    private static async Task Save(RevisionedObject ro)
    {
        // this is a premium feature
        if (LicenseHelper.IsLicensed() == false)
            return;
        
        if (ro == null)
            return;
        if (ro.Uid == Guid.Empty)
            ro.Uid = Guid.NewGuid();
        var manager = DbHelper.GetDbManager();
        using var db = await manager.GetDb();
        await db.Db.InsertAsync(ro);
    }
}