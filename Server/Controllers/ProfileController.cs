using FileFlows.Server.Authentication;
using FileFlows.Server.Helpers;
using FileFlows.Server.Services;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace FileFlows.Server.Controllers;

/// <summary>
/// Profile Controller
/// </summary>
[Route("/api/profile")]
[FileFlowsAuthorize]
public class ProfileController : Controller
{
    /// <summary>
    /// Gets the profile
    /// </summary>
    /// <returns>the profile</returns>
    public async Task<IActionResult> Get()
    {
        var profile = new Profile();
        profile.Security = AuthenticationHelper.GetSecurityMode(); 
        if (profile.Security  == SecurityMode.Off)
        {
            profile.Role = UserRole.Admin;
        }
        else
        {
            var user = await HttpContext.GetLoggedInUser();
            if (user == null)
                return Unauthorized();
            profile.Role = user.Role;
            profile.Name = user.Name;
            profile.Uid = user.Uid;
        }

        profile.ServerOS = PlatformHelper.GetOperatingSystemType();
        
        bool libs = await ServiceLoader.Load<LibraryService>().HasAny();
        bool flows = await ServiceLoader.Load<FlowService>().HasAny();
        bool users = await ServiceLoader.Load<UserService>().HasAny();

        var settings = await ServiceLoader.Load<SettingsService>().Get();

        if (settings.InitialConfigDone)
            profile.ConfigurationStatus |= ConfigurationStatus.InitialConfig;
        if (settings.EulaAccepted)
            profile.ConfigurationStatus |= ConfigurationStatus.EulaAccepted;
        if (flows)
            profile.ConfigurationStatus |= ConfigurationStatus.Flows;
        if (libs)
            profile.ConfigurationStatus |= ConfigurationStatus.Libraries;
        if (users)
            profile.ConfigurationStatus |= ConfigurationStatus.Users;
        profile.IsWebView = Application.UsingWebView;
        var license = LicenseHelper.GetLicense();
        if (license?.Status == LicenseStatus.Valid)
        {
            profile.License = license.Flags;
            profile.UsersEnabled = (license.Flags & LicenseFlags.UserSecurity) == LicenseFlags.UserSecurity &&
                                   ServiceLoader.Load<AppSettingsService>().Settings.Security != SecurityMode.Off && 
                                   await ServiceLoader.Load<UserService>().HasAny();
        }

        return Ok(profile);
    }
}