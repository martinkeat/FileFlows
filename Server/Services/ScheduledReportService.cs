using FileFlows.Managers;
using FileFlows.Plugin;
using FileFlows.ServerShared.Models;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// Service for scheduled reports
/// </summary>
public class ScheduledReportService
{
    /// <summary>
    /// Gets a ScheduledReport by its UID
    /// </summary>
    /// <param name="uid">the UID of the Scheduled Report</param>
    /// <returns>the Scheduled Report if found, otherwise null</returns>
    public Task<ScheduledReport?> GetByUid(Guid uid)
        => new ScheduledReportManager().GetByUid(uid);
    

    /// <summary>
    /// Gets all Scheduled Reports in the system
    /// </summary>
    /// <returns>all Scheduled Reports in the system</returns>
    public Task<List<ScheduledReport>> GetAll()
        => new ScheduledReportManager().GetAll();

    /// <summary>
    /// Updates a scheduled report
    /// </summary>
    /// <param name="report">the scheduled report to update</param>
    /// <param name="auditDetails">The audit details</param>
    /// <returns>the update result</returns>
    public Task<Result<ScheduledReport>> Update(ScheduledReport report, AuditDetails? auditDetails)
        => new ScheduledReportManager().Update(report, auditDetails);

    /// <summary>
    /// Deletes the given scheduled reports
    /// </summary>
    /// <param name="uids">the UID of the scheduled reports to delete</param>
    /// <param name="auditDetails">the audit details</param>
    /// <returns>a task to await</returns>
    public Task Delete(Guid[] uids, AuditDetails auditDetails)
        => new ScheduledReportManager().Delete(uids, auditDetails);
}