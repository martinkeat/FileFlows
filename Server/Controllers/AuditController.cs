using System.Text.RegularExpressions;
using FileFlows.Server.Authentication;
using FileFlows.Server.Helpers;
using FileFlows.Server.Services;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace FileFlows.Server.Controllers;

/// <summary>
/// Controller for the audit log
/// </summary>
[Route("/api/audit")]
[FileFlowsAuthorize(UserRole.Admin)]
public class AuditController : Controller
{
    /// <summary>
    /// Get all access control entries in the system
    /// </summary>
    /// <returns>A list of all access control entries</returns>
    [HttpPost]
    public Task<List<AuditEntry>> Search([FromBody] AuditSearchFilter filter)
    {
        if(LicenseHelper.IsLicensed(LicenseFlags.Auditing) == false)
            return Task.FromResult(new List<AuditEntry>());
        return ServiceLoader.Load<AuditService>().Search(filter);
    }

    /// <summary>
    /// Gets the audit history for a specific object
    /// </summary>
    /// <param name="type">the type of object</param>
    /// <param name="uid">the UID of the object</param>
    /// <returns>the audit history of the object</returns>
    [HttpGet("{type}/{uid}")]
    public Task<List<AuditEntry>> ObjectHistory([FromRoute] string type, [FromRoute] Guid uid)
    {
        if(LicenseHelper.IsLicensed(LicenseFlags.Auditing) == false || string.IsNullOrEmpty(type) || 
           Regex.IsMatch(type, @"^[a-zA-Z0-9\.]+$") == false)
            return Task.FromResult(new List<AuditEntry>());
        
        return ServiceLoader.Load<AuditService>().ObjectHistory(type, uid);
        
    }
}