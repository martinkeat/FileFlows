using FileFlows.Server.Authentication;
using FileFlows.Server.Services;
using FileFlows.ServerShared.Models;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace FileFlows.Server.Controllers;

/// <summary>
/// Bsae controller
/// </summary>
public abstract class BaseController : Controller
{
    /// <summary>
    /// Gets the audit details
    /// </summary>
    /// <returns>the audit details</returns>
    protected async Task<AuditDetails?> GetAuditDetails()
    {
        var ip = HttpContext.Request.GetActualIP();
        var user = await HttpContext.GetLoggedInUser();
        if (user == null)
            return null;
        return new AuditDetails()
        {
            IPAddress = ip,
            UserName = user.Name,
            UserUid = user.Uid
        };
    }
}