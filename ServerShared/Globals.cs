using System.Runtime.InteropServices;

namespace FileFlows.ServerShared;

/// <summary>
/// Globals variables
/// </summary>
public class Globals
{
    /// <summary>
    /// Gets the version of FileFlows
    /// </summary>
    public static Version Version = new Version("1.1.0.2247");

    /// <summary>
    /// The minimum supported node version
    /// </summary>
    public static readonly Version MinimumNodeVersion = Version;
    
    public static bool IsDevelopment { get; set; }

    /// <summary>
    /// Gets if this is running on Windows
    /// </summary>
    public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    
    /// <summary>
    /// Gets if this is running on linux
    /// </summary>
    public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux); 
    
    /// <summary>
    /// Gets if this is running on Mac
    /// </summary>
    public static bool IsMac => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    /// <summary>
    /// Gets if this is running on an ARM CPU
    /// </summary>
    public static bool IsArm =>
        System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == Architecture.Arm
        || System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == Architecture.Arm64;

    /// <summary>
    /// Gets or sets if this node is running inside a docker container
    /// </summary>
    public static bool IsDocker { get; set; }

    /// <summary>
    /// Gets or sets if this node is running as a systemd service
    /// </summary>
    public static bool IsSystemd { get; set; }
    
    /// <summary>
    /// Gets or sets if unit testing
    /// </summary>
    #if(DEBUG)
    public static bool IsUnitTesting { get; set; }
    #else
    public static bool IsUnitTesting => false;
    #endif



    /// <summary>
    /// The name of the internal processing node
    /// </summary>
    public const string InternalNodeName = "FileFlowsServer";
    
    /// <summary>
    /// The UID of the internal processing node
    /// </summary>
    public static readonly Guid InternalNodeUid = new Guid("bf47da28-051e-452e-ad21-c6a3f477fea9");


    public const string FlowFailName = "Fail Flow";
    private const string FailFlowUidStr = "fabbe59c-9d4d-4b6d-b1ef-4ed6585ac7cc";
    public static readonly Guid FailFlowUid = new Guid(FailFlowUidStr);
    public const string FailFlowDescription = "A system flow that will execute when another flow reports a failure, -1 from a node.";

    public const string FlowFailureInputUid = "FileFlows.BasicNodes.FlowFailure";
}