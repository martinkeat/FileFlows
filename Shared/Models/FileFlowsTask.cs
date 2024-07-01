namespace FileFlows.Shared.Models;

/// <summary>
/// A task that runs at a configured schedule
/// </summary>
public class FileFlowsTask : FileFlowObject
{
    /// <summary>
    /// Gets or sets the script this task will execute
    /// </summary>
    public Guid Script { get; set; }
    
    /// <summary>
    /// Gets or sets if this task is enabled
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the type of task
    /// </summary>
    public TaskType Type { get; set; }

    /// <summary>
    /// Gets or sets the schedule this script runs at
    /// </summary>
    public string Schedule { get; set; }
    
    /// <summary>
    /// Gets or sets when the task was last run
    /// </summary>
    public DateTime LastRun { get; set; }

    /// <summary>
    /// Gets or sets the recent run history
    /// </summary>
    public Queue<FileFlowsTaskRun> RunHistory { get; set; } = new Queue<FileFlowsTaskRun>(10);
}



/// <summary>
/// Results of a run script
/// </summary>
public class FileFlowsTaskRun
{
    /// <summary>
    /// Gets or sets when the run was executed
    /// </summary>
    public DateTime RunAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets the execution log
    /// </summary>
    public string Log { get; init; }
    /// <summary>
    /// Gets the return value
    /// </summary>
    public object? ReturnValue { get; init; }

    /// <summary>
    /// Gets if the script ran successfully
    /// </summary>
    public bool Success { get; init; }
}