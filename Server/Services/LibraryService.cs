using FileFlows.Managers;
using FileFlows.Plugin;
using FileFlows.ServerShared.Models;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// Service for communicating with FileFlows server for libraries
/// </summary>
public class LibraryService : ILibraryService
{
    /// <inheritdoc />
    public Task<Library?> GetByUidAsync(Guid uid)
        => new LibraryManager().GetByUid(uid);

    /// <inheritdoc />
    public Task<List<Library>> GetAllAsync()
        => new LibraryManager().GetAll();

    /// <summary>
    /// Gets if there are any libraries in the system
    /// </summary>
    /// <returns>true if there are some, otherwise false</returns>
    public Task<bool> HasAny()
        => new LibraryManager().HasAny();

    /// <summary>
    /// Updates an library
    /// </summary>
    /// <param name="library">the library being updated</param>
    /// <param name="auditDetails">The audit details</param>
    /// <returns>the result of the update, if successful the updated item</returns>
    public Task<Result<Library>> Update(Library library, AuditDetails? auditDetails)
        => new LibraryManager().Update(library, auditDetails);

    /// <summary>
    /// Deletes items matching the UIDs
    /// </summary>
    /// <param name="uids">the UIDs of the items to delete</param>
    /// <param name="auditDetails">the audit details</param>
    public Task Delete(Guid[] uids, AuditDetails auditDetails)
        => new LibraryManager().Delete(uids, auditDetails);

    /// <summary>
    /// Updates the last scanned of a library to now
    /// </summary>
    /// <param name="uid">the UID of the library</param>
    public Task UpdateLastScanned(Guid uid)
        => new LibraryManager().UpdateLastScanned(uid);

    /// <summary>
    /// Updates all libraries with the new flow name if they used this flow
    /// </summary>
    /// <param name="uid">the UID of the flow</param>
    /// <param name="name">the new name of the flow</param>
    /// <returns>a task to await</returns>
    public Task UpdateFlowName(Guid uid, string name)
        => new LibraryManager().UpdateFlowName(uid, name);
}