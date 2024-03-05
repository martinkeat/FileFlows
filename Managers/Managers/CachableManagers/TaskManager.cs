namespace FileFlows.Managers;

/// <summary>
/// Service for communicating with FileFlows server for task
/// </summary>
public class TaskManager : CachedManager<FileFlowsTask>
{
    /// <summary>
    /// Tasks do not need to update the configuration
    /// as they do not effect configuration on a Flow Runner
    /// </summary>
    public override bool IncrementsConfiguration => false;
}