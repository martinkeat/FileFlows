using System.Dynamic;
using System.Text;
using System.Text.RegularExpressions;
using FileFlows.Plugin;
using FileFlows.ScriptExecution;
using FileFlows.Server.Helpers;
using FileFlows.Server.Services;
using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Logger = FileFlows.Shared.Logger;

namespace FileFlows.Server.Controllers;

/// <summary>
/// Script controller
/// </summary>
[Route("/api/script")]
public class ScriptController : Controller
{

    /// <summary>
    /// Gets all scripts in the system
    /// </summary>
    /// <returns>a list of all scripts</returns>
    [HttpGet]
    public Task<IEnumerable<Script>> GetAll()
        => new ScriptService().GetAll();

    /// <summary>
    /// Get script templates for the function editor
    /// </summary>
    /// <returns>a list of script templates</returns>
    [HttpGet("templates")]
    public Task<IEnumerable<Script>> GetTemplates() 
        => new ScriptService().GetAllByType(ScriptType.Template);
    
    /// <summary>
    /// Returns a list of scripts
    /// </summary>
    /// <param name="type">the type of scripts to return</param>
    /// <returns>a list of scripts</returns>
    [HttpGet("all-by-type/{type}")]
    public Task<IEnumerable<Script>> GetAllByType([FromRoute] ScriptType type) 
        => new ScriptService().GetAllByType(type, loadCode: true);

    /// <summary>
    /// Returns a basic list of scripts
    /// </summary>
    /// <param name="type">the type of scripts to return</param>
    /// <returns>a basic list of scripts</returns>
    [HttpGet("list/{type}")]
    public Task<IEnumerable<Script>> List([FromRoute] ScriptType type)
        => new ScriptService().GetAllByType(type);


    /// <summary>
    /// Get a script
    /// </summary>
    /// <param name="name">The name of the script</param>
    /// <param name="type">The type of script</param>
    /// <returns>the script instance</returns>
    [HttpGet("{name}")]
    public Task<Script> Get([FromRoute] string name, ScriptType type = ScriptType.Flow)
        => new ScriptService().Get(name, type);


    /// <summary>
    /// Gets the code for a script
    /// </summary>
    /// <param name="name">The name of the script</param>
    /// <param name="type">The type of script</param>
    /// <returns>the code for a script</returns>
    [HttpGet("{name}/code")]
    public Task<string> GetCode(string name, [FromQuery] ScriptType type = ScriptType.Flow)
        => new ScriptService().GetCode(name, type);


    /// <summary>
    /// Validates a script has valid code
    /// </summary>
    /// <param name="args">the arguments to validate</param>
    [HttpPost("validate")]
    public void ValidateScript([FromBody] ValidateScriptModel args)
        => new ScriptService().ValidateScript(args.Code, args.IsFunction, args.Variables);

    /// <summary>
    /// Saves a script
    /// </summary>
    /// <param name="script">The script to save</param>
    /// <returns>the saved script instance</returns>
    [HttpPost]
    public Script Save([FromBody] Script script)
        => new ScriptService().Save(script);


    /// <summary>
    /// Delete scripts from the system
    /// </summary>
    /// <param name="model">A reference model containing UIDs to delete</param>
    /// <param name="type">The type of scripts being deleted</param>
    /// <returns>an awaited task</returns>
    [HttpDelete]
    public void Delete([FromBody] ReferenceModel<string> model, [FromQuery] ScriptType type = ScriptType.Flow)
    {
        var service = new ScriptService();
        foreach (string m in model.Uids)
        {
            service.Delete(m, type);
        }
    }

    /// <summary>
    /// Exports a script
    /// </summary>
    /// <param name="name">The name of the script</param>
    /// <returns>A download response of the script</returns>
    [HttpGet("export/{name}")]
    public async Task<IActionResult> Export([FromRoute] string name)
    {
        var script = await GetCode(name);
        if (script == null)
            return NotFound();
        byte[] data = System.Text.UTF8Encoding.UTF8.GetBytes(script);
        return File(data, "application/octet-stream", name + ".js");
    }

    /// <summary>
    /// Imports a script
    /// </summary>
    /// <param name="name">The name</param>
    /// <param name="code">The code</param>
    [HttpPost("import")]
    public Script Import([FromQuery(Name = "filename")] string name, [FromBody] string code)
    {
        var service = new ScriptService();
        // will throw if any errors
        name = name.Replace(".js", "").Replace(".JS", "");
        name = service.GetNewUniqueName(name);
        return service.Save(new () { Name = name, Code = code, Repository = false});
    }

    /// <summary>
    /// Duplicates a script
    /// </summary>
    /// <param name="name">The name of the script to duplicate</param>
    /// <param name="type">the script type</param>
    /// <returns>The duplicated script</returns>
    [HttpGet("duplicate/{name}")]
    public async Task<Script> Duplicate([FromRoute] string name, [FromQuery] ScriptType type = ScriptType.Flow)
    {
        var script = await Get(name, type);
        if (script == null)
            return null;
        bool isRepositoryScript = script.Repository;

        var service = new ScriptService();
        script.Name = service.GetNewUniqueName(name);

        if (isRepositoryScript)
        {
            var rgxComments = new Regex(@"\/\*(\*)?(.*?)\*\/", RegexOptions.Singleline);
            string replacement = $"/**\n * @basedOn {(name)}\n";
            var commentMatch = rgxComments.Match(script.Code);
            if (commentMatch.Success)
            {
                var descMatch = Regex.Match(commentMatch.Value, "(?<=(@description ))[^@]+");
                if (descMatch.Success)
                {
                    string desc = descMatch.Value.Trim();
                    if (desc.EndsWith("*"))
                        desc = desc[..^1].Trim();
                    replacement += " * @description " + desc + "\n";
                }
            }
            replacement += " */";
            script.Code = rgxComments.Replace(script.Code, replacement);
        }
        else if (script.Type != ScriptType.Flow)
            script.Code = script.Code.Replace("@name ", "@basedOn ");
        else
            script.Code = Regex.Replace(script.Code, "@name(.*?)$", "@name " + script.Name, RegexOptions.Multiline);
        
        script.Repository = false;
        script.Uid = script.Name;
        script.Type = type;
        return service.Save(script);
    }
    
    /// <summary>
    /// Model used to validate a script
    /// </summary>
    public class ValidateScriptModel
    {
        /// <summary>
        /// Gets or sets the code to validate
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets if this is a function being validated
        /// </summary>
        public bool IsFunction { get; set; }

        /// <summary>
        /// Gets or sets optional variables to use when validating a script
        /// </summary>
        public Dictionary<string, object> Variables { get; set; }
    }
}
