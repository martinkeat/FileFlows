using NPoco;

namespace FileFlows.Shared.Models;

/// <summary>
/// Library status
/// </summary>
public class LibraryStatus
{
    /// <summary>
    /// Gets or sets the name of the status
    /// </summary>
    [Ignore]
    public string Name { get; set; }
    
    /// <summary>
    /// Gets or sets the file status
    /// </summary>
    public FileStatus Status { get; set; }
    
    /// <summary>
    /// Gets or sets the number of library files in this status
    /// </summary>
    [Column("StatusCount")]
    public int Count { get; set; }
}