using FileFlows.Server.Helpers;

namespace FileFlows.Server.Models;

class License
{
    public LicenseStatus Status { get; set; }
    public DateTime ExpirationDateUtc { get; set; }
    public LicenseFlags Flags { get; set; }

    public int ProcessingNodes { get; set; }

    internal static License DefaultLicense() => new License
    {
        Status = LicenseStatus.Unlicensed,
        ProcessingNodes = 2
    };
}


/// <summary>
/// License status
/// </summary>
enum LicenseStatus
{
    /// <summary>
    /// Unlicensed, no key
    /// </summary>
    Unlicensed = -1,
    /// <summary>
    /// Invalid license key
    /// </summary>
    Invalid = 0,
    /// <summary>
    /// Valid license
    /// </summary>
    Valid = 1,
    /// <summary>
    /// Expired license
    /// </summary>
    Expired = 2,
    /// <summary>
    /// Revoked license
    /// </summary>
    Revoked = 4
}