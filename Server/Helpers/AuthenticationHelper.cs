using FileFlows.Server.Services;

namespace FileFlows.Server.Helpers;

/// <summary>
/// Authentication helper
/// </summary>
public class AuthenticationHelper
{
    /// <summary>
    /// Gets the user security mode in in use
    /// </summary>
    /// <returns>the user security mode</returns>
    public static SecurityMode GetSecurityMode()
    {
        if (LicenseHelper.IsLicensed(LicenseFlags.UserSecurity) == false)
            return SecurityMode.Off;
        
        var settings = ServiceLoader.Load<AppSettingsService>().Settings;
        return settings.Security;
    }
}