using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FileFlows.Server.Services;
using FileFlows.Shared.Models;
using Microsoft.IdentityModel.Tokens;

namespace FileFlows.Server.Helpers;

/// <summary>
/// Security helper
/// </summary>
public class SecurityHelper
{
    
    /// <summary>
    /// Creates a JWT Token for an email address
    /// </summary>
    /// <param name="user">the user</param>
    /// <param name="ipAddress">the IP Address of the user</param>
    /// <returns>the JWT token</returns>
    public static string CreateJwtToken(User user, string ipAddress)
    {
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
            Expires = DateTime.UtcNow.AddDays(7),
            Issuer = "https://fileflows.com",
            Audience = "https://fileflows.com",
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.ASCII.GetBytes(key)), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}