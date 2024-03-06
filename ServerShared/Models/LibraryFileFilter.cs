using FileFlows.Shared.Models;

namespace FileFlows.ServerShared.Models;

public class LibraryFileFilter
{
    /// <summary>
    /// Gets if we are requesting a file for processing
    /// </summary>
    public bool GettingFileForProcess => Rows == 1;
    
    /// <summary>
    /// the status of the data
    /// </summary>
    public FileStatus Status { get; set; }
    /// <summary>
    /// Gets or sets a list of libraries that are allowed, or null if any are allowed
    /// </summary>
    public List<Guid> AllowedLibraries { get; set; }
    /// <summary>
    /// Gets or sets the maximum size in MBs of the file to be returned
    /// </summary>
    public long? MaxSizeMBs { get; set; }
    /// <summary>
    /// Gets or sets UIDs of files to be ignored
    /// </summary>
    public List<Guid> ExclusionUids { get; set; }
    /// <summary>
    /// Gets or sets if only forced files should be returned
    /// </summary>
    public bool ForcedOnly { get; set; }
    /// <summary>
    /// Gets or sets the number to rows that will be fetched, not fetched now, but later on, used to determine
    /// if we are getting the 'NextFile' which takes Library runners into account
    /// </summary>
    public int Rows { get; set; }

    /// <summary>
    /// Gets or sets the amount to skip
    /// </summary>
    public int Skip { get; set; }

    /// <summary>
    /// Gets or sets a filter text the file name must contain
    /// </summary>
    public string Filter { get; set; }

    /// <summary>
    /// Gets or sets the system info
    /// </summary>
    public LibraryFilterSystemInfo SysInfo { get; set; }
}

public class LibraryFilterSystemInfo
{
    /// <summary>
    /// Gets or sets all the libraries in the system
    /// </summary>
    public Dictionary<Guid, Library> AllLibraries { get; set; }
    
    /// <summary>
    /// Gets or sets a list of current executors
    /// </summary>
    public List<FlowExecutorInfo> Executors { get; set; }
    
    /// <summary>
    /// Gets or sets if licensed for processing order
    /// </summary>
    public bool LicensedForProcessingOrder { get; set; }
    
} 