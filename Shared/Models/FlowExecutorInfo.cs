using FileFlows.Plugin;

namespace FileFlows.Shared.Models;

/// <summary>
/// Information used during the flow execution 
/// </summary>
public class FlowExecutorInfo
{
    /// <summary>
    /// Gets or sets the UID of the flow
    /// </summary>
    public Guid Uid { get; set; }
    
    /// <summary>
    /// Gets or sets the directory where the configuration is saved
    /// </summary>
    public string ConfigDirectory { get; set; }
    
    /// <summary>
    /// Gets or sets the configuration
    /// </summary>
    public ConfigurationRevision Config { get; set; }
    
    /// <summary>
    /// Gets or sets the UID of the node execution this flow
    /// </summary>
    public Guid NodeUid { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the Node executing this flow
    /// </summary>
    public string NodeName { get; set; }

    /// <summary>
    /// Gets or sets the executing log for of the flow
    /// </summary>
    //public string Log { get; set; }

    /// <summary>
    /// Gets or sets the library file being executed 
    /// </summary>
    public LibraryFile LibraryFile { get; set; }

    /// <summary>
    /// Gets or sets the relative file being executed
    /// </summary>
    public string RelativeFile { get; set; }
    
    /// <summary>
    /// Gets or sets an object reference of the library
    /// that the library file belongs
    /// </summary>
    public ObjectReference Library { get; set; }
    
    /// <summary>
    /// Gets or sets the path of the library
    /// </summary>
    public string LibraryPath { get; set; }
    
    /// <summary>
    /// Gets or sets if a fingerprint should be taken of the final file
    /// </summary>
    public bool Fingerprint { get; set; }

    /// <summary>
    /// Gets or sets the size of the original file being processed
    /// </summary>
    public long InitialSize { get; set; }

    /// <summary>
    /// Gets or sets the file that is currently being worked on/executed
    /// </summary>
    public string WorkingFile { get; set; }
    
    /// <summary>
    /// Gets or sets if the working file is actually a directory
    /// </summary>
    public bool IsDirectory { get; set; }

    /// <summary>
    /// Gets or sets the total parts in the flow that is executing
    /// </summary>
    public int TotalParts { get; set; }
    
    /// <summary>
    /// Gets or sets the index of the flow part that is currently executing
    /// </summary>
    public int CurrentPart { get; set; }

    /// <summary>
    /// Gets or sets the name of the flow part that is currently executing
    /// </summary>
    public string CurrentPartName { get; set; }

    /// <summary>
    /// Gets or sets the current percent of the executing flow part
    /// </summary>
    public float CurrentPartPercent { get; set; }

    /// <summary>
    /// Gets or sets when the last update was reported to the server
    /// </summary>
    public DateTime LastUpdate { get; set; }
    
    /// <summary>
    /// Gets or sets when the flow execution started
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Gets the total processing time of the flow
    /// </summary>
    public TimeSpan ProcessingTime => StartedAt > new DateTime(2000, 1, 1) ? DateTime.Now.Subtract(StartedAt) : new TimeSpan();
}

