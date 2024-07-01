namespace FileFlows.ServerShared.Models;

/// <summary>
/// Statistics for a Node
/// </summary>
public class NodeSystemStatistics
{
    /// <summary>
    /// Gets or sets the node UID
    /// </summary>
    public Guid Uid { get; set; }

    /// <summary>
    /// Gets or sets the size of the temporary directory
    /// </summary>
    public DirectorySize? TemporaryDirectorySize { get; set; }
    
    /// <summary>
    /// Gets or sets the size of the log directory
    /// </summary>
    public DirectorySize? LogDirectorySize { get; set; }

    /// <summary>
    /// Gets or sets when this was recorded
    /// </summary>
    public DateTime RecordedAt { get; set; }
}

/// <summary>
/// Records a directory path and size
/// </summary>
public class DirectorySize
{
    /// <summary>
    /// Gets or sets the path of the directory
    /// </summary>
    public string Path { get; set; } = null!;
    /// <summary>
    /// Gets or sets the size of the directory
    /// </summary>
    public long Size { get; set; }
}