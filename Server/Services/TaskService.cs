using FileFlows.Managers;
using FileFlows.Plugin;
using FileFlows.ServerShared.Models;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// Service for communicating with FileFlows server for task
/// </summary>
public class TaskService
{
    /// <summary>
    /// Gets all the tasks in the system
    /// </summary>
    /// <returns>all the tasks</returns>
    public Task<List<FileFlowsTask>> GetAllAsync()
        => new TaskManager().GetAll();

    /// <summary>
    /// Gets a task by its UID
    /// </summary>
    /// <param name="uid">the UID of the task</param>
    /// <returns></returns>
    public Task<FileFlowsTask?> GetByUidAsync(Guid uid)
        => new TaskManager().GetByUid(uid);

    /// <summary>
    /// Gets a task by its name
    /// </summary>
    /// <param name="name">the name of the task</param>
    /// <returns>the task if found</returns>
    public Task<FileFlowsTask?> GetByNameAsync(string name)
        => new TaskManager().GetByName(name);

    /// <summary>
    /// Updates a task
    /// </summary>
    /// <param name="task">the task to update</param>
    /// <param name="auditDetails">The audit details</param>
    /// <returns>the update result</returns>
    public Task<Result<FileFlowsTask>> Update(FileFlowsTask task, AuditDetails? auditDetails)
        => new TaskManager().Update(task, auditDetails);

    /// <summary>
    /// Deletes the given tasks
    /// </summary>
    /// <param name="uids">the UID of the tasks to delete</param>
    /// <param name="auditDetails">the audit details</param>
    /// <returns>a task to await</returns>
    public Task Delete(Guid[] uids, AuditDetails auditDetails)
        => new TaskManager().Delete(uids, auditDetails);
}