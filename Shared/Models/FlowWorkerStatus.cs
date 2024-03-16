namespace FileFlows.Shared.Models;

using FileFlows.Plugin;
using System;

/// <summary>
/// Flow worker status detailing information about a currently executing flow
/// </summary>
public class FlowWorkerStatus
{
    /// <summary>
    /// Gets or sets the a unique id for this flow executor.
    /// This is used so the flow can be cancelled and will be uniquely
    /// generated when the flow starts
    /// </summary>
    public Guid Uid { get; set; }
    
    /// <summary>
    /// Gets or sets the current file being processed
    /// </summary>
    public string CurrentFile { get; set; }
    
    /// <summary>
    /// Gets or sets the relative file of the executing file
    /// </summary>
    public string RelativeFile { get; set; }

    /// <summary>
    /// Gets or sets an object reference of the library
    /// where the currently executing file belongs
    /// </summary>
    public ObjectReference Library { get; set; }

    /// <summary>
    /// Gets or sets the current flow part
    /// </summary>
    public Guid CurrentUid { get; set; }
    
    /// <summary>
    /// Gets or sets the current workign file
    /// </summary>
    public string WorkingFile { get; set; }

    /// <summary>
    /// Gets the current process status
    /// </summary>
    public ProcessStatus Status => string.IsNullOrEmpty(CurrentFile) ? ProcessStatus.Waiting : ProcessStatus.Processing;
    
    /// <summary>
    /// Gets or sets the total parts of the flow
    /// </summary>
    public int TotalParts { get; set; }
    
    /// <summary>
    /// Gets or sets the index of current flow part
    /// </summary>
    public int CurrentPart { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the current flow part
    /// </summary>
    public string CurrentPartName { get; set; }

    /// <summary>
    /// Gets or sets the percentage of the currently executing flow part
    /// </summary>
    public float CurrentPartPercent { get; set; }

    
    /// <summary>
    /// Gets or sets when this flow was started
    /// </summary>
    public DateTime StartedAt { get; set; }

    
    /// <summary>
    /// Gets the current total processing time of the flow
    /// </summary>
    public TimeSpan ProcessingTime => StartedAt > new DateTime(2000, 1, 1) ? DateTime.UtcNow.Subtract(StartedAt) : new TimeSpan();
}

/// <summary>
/// The processing status
/// </summary>
public enum ProcessStatus
{
    /// <summary>
    /// Waiting to be processed
    /// </summary>
    Waiting,
    /// <summary>
    /// Processing a file/folder
    /// </summary>
    Processing
}