using FileFlows.Managers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// Service for communicating with FileFlows server for revisions
/// </summary>
public class RevisionService 
{
    /// <summary>
    /// Gets all revisions for a given objet
    /// </summary>
    /// <param name="uid">the UID of the object to get revisions for</param>
    /// <returns>all the revisions for the object</returns>
    public Task<List<RevisionedObject>> GetAllAsync(Guid uid)
        => new RevisionManager().GetAll();

    /// <summary>
    /// Deletes the given revisions
    /// </summary>
    /// <param name="uids">the UID of the revisions to delete</param>
    /// <returns>a task to await</returns>
    public Task Delete(Guid[] uids)
        => new RevisionManager().Delete(uids);

    /// <summary>
    /// Get latest revisions for all objects
    /// </summary>
    /// <returns>A list of latest revisions for all objects</returns>
    public Task<List<RevisionedObject>> ListAllAsync()
        => new RevisionManager().ListAll();

    /// <summary>
    /// Gets a specific revision
    /// </summary>
    /// <param name="uid">The UID of the object</param>
    /// <param name="revisionUid">the UID of the revision</param>
    /// <returns>The specific revision</returns>
    public Task<RevisionedObject> Get(Guid uid, Guid revisionUid)
        => new RevisionManager().Get(uid, revisionUid);

    /// <summary>
    /// Restores a revision
    /// </summary>
    /// <param name="uid">The UID of the object</param>
    /// <param name="revisionUid">the UID of the revision</param>
    public Task Restore(Guid uid, Guid revisionUid)
        => new RevisionManager().Restore(uid, revisionUid);

    /// <summary>
    /// Saves the revision to the database
    /// </summary>
    /// <param name="ro">the revisioned object to save</param>
    public Task Insert(RevisionedObject ro)
        => new RevisionManager().Insert(ro);
}