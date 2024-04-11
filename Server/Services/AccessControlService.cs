using System.Net;
using FileFlows.Managers;
using FileFlows.Plugin;
using FileFlows.ServerShared.Models;
using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// Service for the access control lists
/// </summary>
public class AccessControlService
{
    /// <summary>
    /// Gets all the access control entries
    /// </summary>
    /// <param name="type">The access control type</param>
    /// <returns>all the entries</returns>
    public async Task<List<AccessControlEntry>> GetAllAsync(AccessControlType? type)
    {
        var all = (await new AccessControlManager().GetAll()).Where(x => type == null || x.Type == type).ToList();
        return all.Any() != true ? new List<AccessControlEntry>() : all.OrderBy(x => x.Order).ToList();
    }

    /// <summary>
    /// Gets a access control entry by its UID
    /// </summary>
    /// <param name="uid">the UID of the access control entry</param>
    /// <returns>the access control entry if found</returns>
    public Task<AccessControlEntry?> GetByUidAsync(Guid uid)
        => new AccessControlManager().GetByUid(uid);

    /// <summary>
    /// Updates a access control entry
    /// </summary>
    /// <param name="entry">the access control entry to update</param>
    /// <param name="auditDetails">The audit details</param>
    /// <returns>the update result</returns>
    public Task<Result<AccessControlEntry>> Update(AccessControlEntry entry, AuditDetails? auditDetails)
        => new AccessControlManager().Update(entry, auditDetails);

    /// <summary>
    /// Deletes the given access control entries
    /// </summary>
    /// <param name="uids">the UID of the access control entry to delete</param>
    /// <param name="auditDetails">the audit details</param>
    /// <returns>a task to await</returns>
    public Task Delete(Guid[] uids, AuditDetails auditDetails)
        => new AccessControlManager().Delete(uids, auditDetails);

    /// <summary>
    /// Moves access control entries
    /// </summary>
    /// <param name="uids">The UIDs to move</param>
    /// <param name="type">The type of entries being moved</param>
    /// <param name="up">If the items are being moved up or down</param>
    /// <param name="auditDetails">the audit details</param>
    public Task Move(Guid[] uids, AccessControlType type, bool up, AuditDetails auditDetails)
        => new AccessControlManager().Move(uids, type, up, auditDetails);

    /// <summary>
    /// Checks if the IP Address is allowed to access this
    /// </summary>
    /// <param name="type">the type of access</param>
    /// <param name="ipAddress">the IP Address</param>
    public async Task<bool> CanAccess(AccessControlType type, string ipAddress)
    {
        if (ipAddress is "127.0.0.1" or "::1")
            return true; // always allow localhost
        var list = await GetAllAsync(type);
        if (list?.Any() != true)
            return true; // no access control configured, allow all

        if (IPAddress.TryParse(ipAddress, out IPAddress? ip) == false || ip == null)
            return false;

        foreach (var entry in list)
        {
            if (IPAddress.TryParse(entry.Start, out IPAddress? ipStart) == false || ipStart == null)
                continue;

            if (string.IsNullOrWhiteSpace(entry.End) || IPAddress.TryParse(entry.End, out IPAddress? ipEnd) == false ||
                ipEnd == null ||
                ipEnd.AddressFamily != ipStart.AddressFamily || IPHelper.IsGreaterThan(ipStart, ipEnd) == false)
            {
                // check if ips are same
                if (Equals(ip, ipStart) == false)
                    continue;
                return entry.Allow;
            }

            if (IPHelper.InRange(ipStart, ipEnd, ip))
            {
                return entry.Allow;
            }
        }

        // theres a list, not found, so block
        return false;
    }
}