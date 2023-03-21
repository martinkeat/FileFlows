using Microsoft.AspNetCore.Mvc;
using FileFlows.Server.Helpers;
using FileFlows.Server.Services;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Controllers;

/// <summary>
/// Variable Controller
/// </summary>
[Route("/api/variable")]
public class VariableController : Controller
{   
    /// <summary>
    /// Get all variables configured in the system
    /// </summary>
    /// <returns>A list of all configured variables</returns>
    [HttpGet]
    public IEnumerable<Variable> GetAll() 
        => new VariableService().GetAll().OrderBy(x => x.Name.ToLowerInvariant());

    /// <summary>
    /// Get variable
    /// </summary>
    /// <param name="uid">The UID of the variable to get</param>
    /// <returns>The variable instance</returns>
    [HttpGet("{uid}")]
    public Variable Get(Guid uid)
        => new VariableService().GetByUid(uid);

    /// <summary>
    /// Get a variable by its name, case insensitive
    /// </summary>
    /// <param name="name">The name of the variable</param>
    /// <returns>The variable instance if found</returns>
    [HttpGet("name/{name}")]
    public Variable? GetByName(string name)
        => new VariableService().GetByName(name);

    /// <summary>
    /// Saves a variable
    /// </summary>
    /// <param name="variable">The variable to save</param>
    /// <returns>The saved instance</returns>
    [HttpPost]
    public Variable Save([FromBody] Variable variable)
    {
        new VariableService().Update(variable);
        return variable;
    }

    /// <summary>
    /// Delete variables from the system
    /// </summary>
    /// <param name="model">A reference model containing UIDs to delete</param>
    /// <returns>an awaited task</returns>
    [HttpDelete]
    public Task Delete([FromBody] ReferenceModel<Guid> model)
        => new VariableService().Delete(model.Uids);
}