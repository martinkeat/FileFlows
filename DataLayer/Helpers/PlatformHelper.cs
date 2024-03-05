using System.Runtime.InteropServices;

namespace FileFlows.DataLayer.Helpers;

public class PlatformHelper
{
    /// Gets if this is running on an ARM CPU
    public static bool IsArm = RuntimeInformation.ProcessArchitecture == Architecture.Arm
                         || RuntimeInformation.ProcessArchitecture == Architecture.Arm64;
}