using System.Net;

namespace FileFlows.Shared.Helpers;

/// <summary>
/// IP Address helper
/// </summary>
public class IPHelper
{
    /// <summary>
    /// Determines whether the second IP address is greater than the first one.
    /// </summary>
    /// <param name="ipStart">The first IP address to compare.</param>
    /// <param name="ipEnd">The second IP address to compare.</param>
    /// <returns>True if ipEnd is greater than ipStart, otherwise false.</returns>
    public static bool IsGreaterThan(IPAddress ipStart, IPAddress ipEnd)
    {
        byte[] startBytes = ipStart.GetAddressBytes();
        byte[] endBytes = ipEnd.GetAddressBytes();

        for (int i = 0; i < startBytes.Length; i++)
        {
            if (endBytes[i] > startBytes[i])
            {
                return true;
            }
            
            if (endBytes[i] < startBytes[i])
            {
                return false;
            }
        }

        return false; // If execution reaches here, it means ipStart is equal to ipEnd or ipEnd is less than ipStart
    }
    
    /// <summary>
    /// Checks if an IP address falls within a given range (inclusive of the start and end IP addresses).
    /// </summary>
    /// <param name="ipStart">The start IP address of the range.</param>
    /// <param name="ipEnd">The end IP address of the range.</param>
    /// <param name="ip">The IP address to check.</param>
    /// <returns>True if the IP address falls within the range [ipStart, ipEnd], otherwise false.</returns>
    public static bool InRange(IPAddress ipStart, IPAddress ipEnd, IPAddress ip)
    {
        byte[] startBytes = ipStart.GetAddressBytes();
        byte[] endBytes = ipEnd.GetAddressBytes();
        byte[] ipBytes = ip.GetAddressBytes();

        // Ensure IP addresses are of the same family
        if (ipStart.AddressFamily != ip.AddressFamily || ipEnd.AddressFamily != ip.AddressFamily)
            return false;

        // Check if ip is within the range [ipStart, ipEnd]
        for (int i = 0; i < startBytes.Length; i++)
        {
            if (ipBytes[i] < startBytes[i] || ipBytes[i] > endBytes[i])
            {
                return false;
            }
        }

        return true;
    }
}