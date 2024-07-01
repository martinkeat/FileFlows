using FileFlows.Shared.Models;

namespace FileFlows.ServerShared.Models;



/// <summary>
/// Known file info
/// </summary>
public class KnownFileInfo
{
    /// <summary>
    /// Gets or sets the filename
    /// </summary>
    public string Name { get; set; } = null!;
    /// <summary>
    /// Gets or sets the creation time
    /// </summary>
    public DateTime CreationTime { get; set; } 
    /// <summary>
    /// Gets or sets the last write time
    /// </summary>
    public DateTime LastWriteTime { get; set; }
    /// <summary>
    /// Gets or sets the status
    /// </summary>
    public FileStatus Status { get; set; }
}