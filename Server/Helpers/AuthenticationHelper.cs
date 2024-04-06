using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FileFlows.Server.Services;
using FileFlows.Shared.Models;
using Microsoft.IdentityModel.Tokens;

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
        if (settings.Security is SecurityMode.Local or SecurityMode.Off)
            return settings.Security;
        if (LicenseHelper.IsLicensed(LicenseFlags.Enterprise) == false)
            return SecurityMode.Off;
        return settings.Security;
    }
    
    /// <summary>
    /// Creates a JWT Token for an email address
    /// </summary>
    /// <param name="user">the user</param>
    /// <param name="ipAddress">the IP Address of the user</param>
    /// <returns>the JWT token</returns>
    public static string CreateJwtToken(User user, string ipAddress, int expiryMinutes)
    {
        if (expiryMinutes < 1)
            expiryMinutes = 24 * 60;
        string ip = ipAddress.Replace(":", "_");
        string code = Decrypter.Encrypt(user.Uid + ":" + ip + ":" + user.Name);
        var claims = new List<Claim>
        {
            new (ClaimTypes.Name, user.Name),
            new (ClaimTypes.Email, user.Email),
            new Claim("code", code)
        };
    
        var claimsIdentity = new ClaimsIdentity(claims);

        var key = ServiceLoader.Load<AppSettingsService>().Settings.EncryptionKey;
    
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = claimsIdentity,
            Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
            Issuer = "https://fileflows.com",
            Audience = "https://fileflows.com",
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.ASCII.GetBytes(key)), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}