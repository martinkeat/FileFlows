using FileFlows.Server.Authentication;
using FileFlows.Server.Services;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace FileFlows.Server.Controllers;

/// <summary>
/// Controller for scheduled tasks
/// </summary>
[Route("/api/task")]
[FileFlowsAuthorize(UserRole.Tasks)]
public class TaskController : Controller
{
    /// <summary>
    /// Get all scheduled tasks configured in the system
    /// </summary>
    /// <returns>A list of all configured scheduled tasks</returns>
    [HttpGet]
    public async Task<IEnumerable<FileFlowsTask>> GetAll()
        => (await ServiceLoader.Load<TaskService>().GetAllAsync()).OrderBy(x => x.Name.ToLowerInvariant());

    /// <summary>
    /// Get scheduled task
    /// </summary>
    /// <param name="uid">The UID of the scheduled task to get</param>
    /// <returns>The scheduled task instance</returns>
    [HttpGet("{uid}")]
    public Task<FileFlowsTask?> Get(Guid uid) 
        => ServiceLoader.Load<TaskService>().GetByUidAsync(uid);

    /// <summary>
    /// Get a scheduled task by its name, case insensitive
    /// </summary>
    /// <param name="name">The name of the scheduled task</param>
    /// <returns>The scheduled task instance if found</returns>
    [HttpGet("name/{name}")]
    public Task<FileFlowsTask?> GetByName(string name)
        => ServiceLoader.Load<TaskService>().GetByNameAsync(name);

    /// <summary>
    /// Saves a scheduled task
    /// </summary>
    /// <param name="fileFlowsTask">The scheduled task to save</param>
    /// <returns>The saved instance</returns>
    [HttpPost]
    public async Task<IActionResult> Save([FromBody] FileFlowsTask fileFlowsTask)
    {
        var result = await ServiceLoader.Load<TaskService>().Update(fileFlowsTask);
        if (result.Failed(out string error))
            return BadRequest(error);
        return Ok(result.Value);
    }

    /// <summary>
    /// Delete scheduled tasks from the system
    /// </summary>
    /// <param name="model">A reference model containing UIDs to delete</param>
    /// <returns>an awaited task</returns>
    [HttpDelete]
    public Task Delete([FromBody] ReferenceModel<Guid> model)
        => ServiceLoader.Load<TaskService>().Delete(model.Uids);


    /// <summary>
    /// Runs a script now
    /// </summary>
    /// <param name="uid">the UID of the script</param>
    [HttpPost("run/{uid}")]
    public Task<FileFlowsTaskRun> Run([FromRoute] Guid uid)
        => Workers.FileFlowsTasksWorker.Instance.RunByUid(uid);
}
