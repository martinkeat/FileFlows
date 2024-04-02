using FileFlows.Server.Helpers;
using FileFlows.Server.Services;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FileFlows.Server.Authentication;

/// <summary>
/// FileFlows authentication attribute
/// </summary>
public class FileFlowsAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    /// <summary>
    /// Gets or sets the role
    /// </summary>
    public UserRole Role { get; set; }

    /// <summary>
    /// Constructs a new instance of the FileFlows authorize filter
    /// </summary>
    /// <param name="role">the role</param>
    public FileFlowsAuthorizeAttribute(UserRole role = UserRole.Basic)
    {
        Role = role;
    }
    
    /// <summary>
    /// Handles the on on authorization
    /// </summary>
    /// <param name="context">the context</param>
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (LicenseHelper.IsLicensed(LicenseFlags.UserSecurity) == false)
            return;
        
        var settings = ServiceLoader.Load<AppSettingsService>().Settings;
        if (settings.Security == SecurityMode.Off)
            return;
        
        var user = context.HttpContext.GetLoggedInUser().Result;
        if(user == null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        if ((user.Role & Role) != Role)
        {
            context.Result = new UnauthorizedResult();
            return;
        }
    }
}