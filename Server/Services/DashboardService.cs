using FileFlows.Managers;
using FileFlows.Plugin;
using FileFlows.ServerShared.Models;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// Dashboard Service
/// </summary>
public class DashboardService
{
    /// <summary>
    /// Gets all dashboards in the system
    /// </summary>
    /// <returns>all the dashboards</returns>
    public Task<List<Dashboard>> GetAll()
        => new DashboardManager().GetAll();

    /// <summary>
    /// Gets a dashboard by its UID
    /// </summary>
    /// <param name="uid">the UID of the dashboard</param>
    /// <returns>all the dashboards</returns>
    public Task<Dashboard?> GetByUid(Guid uid)
        => new DashboardManager().GetByUid(uid);

    /// <summary>
    /// Deletes items matching the UIDs
    /// </summary>
    /// <param name="uids">the UIDs of the items to delete</param>
    /// <param name="auditDetails">the audit details</param>
    public Task Delete(Guid[] uids, AuditDetails auditDetails)
        => new DashboardManager().Delete(uids, auditDetails);

    /// <summary>
    /// Updates an item
    /// </summary>
    /// <param name="item">the item being updated</param>
    /// <param name="auditDetails">The audit details</param>
    /// /// <returns>the result of the update, if successful the updated item</returns>
    public Task<Result<Dashboard>> Update(Dashboard item, AuditDetails? auditDetails)
        => new DashboardManager().Update(item, auditDetails, dontIncrementConfigRevision: true);
}