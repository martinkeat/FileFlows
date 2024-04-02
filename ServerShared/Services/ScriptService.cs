using System.Text.Encodings.Web;
using FileFlows.Plugin;
using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.ServerShared.Services;

/// <summary>
/// Script Service interface
/// </summary>
public interface IScriptService
{
    /// <summary>
    /// Get all scripts
    /// </summary>
    /// <returns>a collection of scripts</returns>
    Task<IEnumerable<Script>> GetAll();
    
    
    /// <summary>
    /// Get a script
    /// </summary>
    /// <param name="name">The name of the script</param>
    /// <param name="type">the type of script to get</param>
    /// <returns>the script</returns>
    Task<Script> Get(string name, ScriptType type);
    
    /// <summary>
    /// Gets or sets a function used to load new instances of the service
    /// </summary>
    /// <param name="name">The name of the script</param>
    /// <param name="type">the type of script</param>
    /// <returns>the script code</returns>
    Task<string> GetCode(string name, ScriptType type);
}
