using FileFlows.Shared.Models;

namespace FileFlows.ServerShared.Models;


/// <summary>
/// Result from a get Next Library call 
/// </summary>
public class NextLibraryFileResult
{
    /// <summary>
    /// Gets or sets the library file to process
    /// </summary>
    public LibraryFile? File { get; set; }

    /// <summary>
    /// Gets or sets the status of the call
    /// </summary>
    public NextLibraryFileStatus Status { get; set; }
}

/// <summary>
/// Status for the next library file call
/// </summary>
public enum NextLibraryFileStatus
{
    /// <summary>
    /// No files to process
    /// </summary>
    NoFile = 0,
    /// <summary>
    /// Successfully found file to process
    /// </summary>
    Success = 1,
    /// <summary>
    /// Server update pending
    /// </summary>
    UpdatePending = 2,
    /// <summary>
    /// The system is paused
    /// </summary>
    SystemPaused = 3,
    /// <summary>
    /// Version specified is not a valid version number
    /// </summary>
    InvalidVersion = 4,
    /// <summary>
    /// The version supplied is not a supported version
    /// </summary>
    VersionMismatch = 5,
    /// <summary>
    /// The node is not enabled
    /// </summary>
    NodeNotEnabled = 6,
    /// <summary>
    /// License limit reached
    /// </summary>
    LicenseLimitReached = 7
}