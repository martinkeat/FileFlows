using System.Runtime.InteropServices;

namespace FileFlows.ServerShared.Helpers;

/// <summary>
/// Helper to determine platform
/// </summary>
public class PlatformHelper
{
    /// Gets if this is running on an ARM CPU
    public static bool IsArm = RuntimeInformation.ProcessArchitecture == Architecture.Arm
                         || RuntimeInformation.ProcessArchitecture == Architecture.Arm64;

    /// <summary>
    /// Gets the operating system type for the current system
    /// </summary>
    /// <returns>the operating system type</returns>
    public static OperatingSystemType GetOperatingSystemType()
    {
        if (Globals.IsDocker)
            return OperatingSystemType.Docker;
        if (OperatingSystem.IsWindows())
            return OperatingSystemType.Windows;
        if (OperatingSystem.IsFreeBSD())
            return OperatingSystemType.FreeBsd;
        if (OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst())
            return OperatingSystemType.Mac;
        if (OperatingSystem.IsLinux())
            return OperatingSystemType.Linux;
        return OperatingSystemType.Unknown;
    }
}