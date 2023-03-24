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
    private const string UnsafeCharacters = "<>:\"/\\|?*";
    
    /// <summary>
    /// Gets all scripts in the system
    /// </summary>
    /// <returns>a list of all scripts</returns>
    [HttpGet]
    public async Task<IEnumerable<Script>> GetAll()
    {
        List<Script> scripts = new();
        var taskScriptsFlow = GetAll(ScriptType.Flow);
        var taskScriptsSystem = GetAll(ScriptType.System);
        var taskScriptsShared = GetAll(ScriptType.Shared);

        FileFlowsRepository repo = new FileFlowsRepository();
        try
        {
            repo = await new RepositoryService().GetRepository();
        }
        catch (Exception)
        {
            // silently fail
        }

        scripts.AddRange(taskScriptsFlow.Result);
        scripts.AddRange(taskScriptsSystem.Result);
        scripts.AddRange(taskScriptsShared.Result);

        var dictFlowScripts = repo.FlowScripts?.ToDictionary(x => x.Path, x => x.Revision) ?? new ();
        var dictSystemScripts = repo.SystemScripts?.ToDictionary(x => x.Path, x => x.Revision) ?? new ();
        var dictSharedScripts = repo.SharedScripts?.ToDictionary(x => x.Path, x => x.Revision) ?? new ();
        foreach (var script in scripts)
        {
            if (string.IsNullOrEmpty(script.Path))
                continue;
            var dict = script.Type switch
            {
                ScriptType.Shared => dictSharedScripts,
                ScriptType.System => dictSystemScripts,
                _ => dictFlowScripts
            };
            if (dict.ContainsKey(script.Path) == false)
                continue;
            script.LatestRevision = dict[script.Path];
        }
        
        scripts = scripts.DistinctBy(x => x.Name).ToList();
        var dictScripts = scripts.ToDictionary(x => x.Name.ToLower(), x => x);
        var flows = new FlowController().GetAll();
        string flowTypeName = typeof(Flow).FullName ?? string.Empty;
        foreach (var flow in flows ?? new Flow[] {})
        {
            if (flow?.Parts?.Any() != true)
                continue;
            foreach (var p in flow.Parts)
            {
                if (p.FlowElementUid.StartsWith("Script:") == false)
                    continue;
                string scriptName = p.FlowElementUid[7..].ToLower();
                if (dictScripts.ContainsKey(scriptName) == false)
                    continue;
                var script = dictScripts[scriptName];
                script.UsedBy ??= new();
                if (script.UsedBy.Any(x => x.Uid == flow.Uid))
                    continue;
                script.UsedBy.Add(new ()
                {
                    Name = flow.Name,
                    Type = flowTypeName,
                    Uid = flow.Uid
                });
            }
        }

        var tasks = new TaskController().GetAll();
        string taskTypeName = typeof(FileFlowsTask).FullName ?? string.Empty;
        foreach (var task in tasks)
        {
            if (dictScripts.ContainsKey(task.Script.ToLower()) == false)
                continue;
            var script = dictScripts[task.Script.ToLower()];
            script.UsedBy ??= new();
            script.UsedBy.Add(new ()
            {
                Name = task.Name,
                Type = taskTypeName,
                Uid = task.Uid
            });
        }

        return scripts.OrderBy(x => x.Name.ToLowerInvariant());
    }

    /// <summary>
    /// Get script templates for the function editor
    /// </summary>
    /// <returns>a list of script templates</returns>
    [HttpGet("templates")]
    public Task<IEnumerable<Script>> GetTemplates() => GetAll(ScriptType.Template);
    
    /// <summary>
    /// Returns a list of scripts
    /// </summary>
    /// <param name="type">the type of scripts to return</param>
    /// <returns>a list of scripts</returns>
    [HttpGet("all-by-type/{type}")]
    public Task<IEnumerable<Script>> GetAllByType([FromRoute] ScriptType type) => GetAll(type, loadCode: true);

    /// <summary>
    /// Returns a basic list of scripts
    /// </summary>
    /// <param name="type">the type of scripts to return</param>
    /// <returns>a basic list of scripts</returns>
    [HttpGet("list/{type}")]
    public Task<IEnumerable<Script>> List([FromRoute] ScriptType type) => GetAll(type, loadCode: false);

    async Task<IEnumerable<Script>> GetAll(ScriptType type, bool loadCode = true)
    {
        List<Script> scripts = new();
        string dir = type == ScriptType.Flow ? DirectoryHelper.ScriptsDirectoryFlow : 
            type == ScriptType.Shared ? DirectoryHelper.ScriptsDirectoryShared : 
            type == ScriptType.Template ? DirectoryHelper.ScriptsDirectoryFunction : 
            DirectoryHelper.ScriptsDirectorySystem;
        foreach (var file in new DirectoryInfo(dir).GetFiles("*.js", SearchOption.AllDirectories))
        {
            var script = await GetScript(type, file, loadCode);
            scripts.Add(script);
        }

        return scripts.OrderBy(x => x.Name);
    }

    private async Task<Script> GetScript(ScriptType type, FileInfo file, bool loadCode)
    {
        string name = file.Name.Replace(".js", "");
        var code = loadCode ? await System.IO.File.ReadAllTextAsync(file.FullName) : null;
        bool repository = false;
        int revision = 0;
        string path = string.Empty;
        if (loadCode)
        {
            repository = code.StartsWith("// path:");
            if (repository)
            {
                var match = Regex.Match(code, @"@revision ([\d]+)");
                if (match.Success)
                    revision = int.Parse(match.Groups[1].Value);
                path = code.Split('\n').First().Substring("// path:".Length).Trim();
            }
        }
        else
        {
            string line = (await System.IO.File.ReadAllLinesAsync(file.FullName)).First();
            repository = line?.StartsWith("// path:") == true;
            if(repository)
                path = line.Substring("// path:".Length).Trim();
        }

        return new Script
        {
            Uid = name,
            Name = name,
            Repository = repository,
            Type = type,
            Revision = revision,
            Path = path,
            Code = code
        };
    }

    /// <summary>
    /// Get a script
    /// </summary>
    /// <param name="name">The name of the script</param>
    /// <param name="type">The type of script</param>
    /// <returns>the script instance</returns>
    [HttpGet("{name}")]
    public async Task<Script> Get([FromRoute] string name, ScriptType type = ScriptType.Flow)
    {
        var result = FindScript(name, type);
        return await GetScript(type, new FileInfo(result.File), true);
    }

    private (bool System, string File) FindScript(string name, ScriptType type)
    {
        if (ValidScriptName(name) == false)
        {
            Logger.Instance.ELog("Script not found, name invalid: " + name);
            throw new Exception("Script not found");
        }

        string file = GetFullFilename(name, type);
        if (System.IO.File.Exists(file))
            return (true, file);

        Logger.Instance.ELog($"Script '{name}' not found: {file}");
        throw new Exception("Script not found");

    }

    /// <summary>
    /// Gets the code for a script
    /// </summary>
    /// <param name="name">The name of the script</param>
    /// <param name="type">The type of script</param>
    /// <returns>the code for a script</returns>
    [HttpGet("{name}/code")]
    public async Task<string> GetCode(string name, [FromQuery] ScriptType type = ScriptType.Flow)
    {
        if (ValidScriptName(name) == false)
            return $"Logger.ELog('invalid name: {name.Replace("'", "''")}');\nreturn -1";
        try
        {
            var result = FindScript(name, type);
            return await System.IO.File.ReadAllTextAsync(result.File);
        }
        catch (Exception ex)
        {
            return $"Logger.ELog('Failed reading script: {ex.Message}');\nreturn -1";
        }
    }


    /// <summary>
    /// Validates a script has valid code
    /// </summary>
    /// <param name="args">the arguments to validate</param>
    [HttpPost("validate")]
    public void ValidateScript([FromBody] ValidateScriptModel args)
    {
        var executor = new FileFlows.ScriptExecution.Executor();
        executor.Code = args.Code;
        
        if (args.IsFunction  && executor.Code.IndexOf("function Script") < 0)
        {
            executor.Code = "function Script() { " + executor.Code + "\n}\n";
            executor.Code += $"var scriptResult = Script();\nexport const result = scriptResult;";
        }
        
        executor.SharedDirectory = DirectoryHelper.ScriptsDirectoryShared;
        executor.HttpClient = HttpHelper.Client;
        executor.Logger = new ScriptExecution.Logger();
        string log = String.Empty;
        var logFunction = (object[] args) =>
        {
            log += string.Join(", ", args.Select(x =>
                x == null ? "null" :
                x.GetType().IsPrimitive || x is string ? x.ToString() :
                System.Text.Json.JsonSerializer.Serialize(x))) + "\n";
        };
        
        executor.Logger.DLogAction = logFunction;
        executor.Logger.ILogAction = logFunction;
        executor.Logger.WLogAction = logFunction;
        string error = string.Empty;
        executor.Logger.ELogAction = (args) =>
        {
            error = string.Join(", ", args.Select(x =>
                x == null ? "null" :
                x.GetType().IsPrimitive ? x.ToString() :
                x is string ? x.ToString() :
                System.Text.Json.JsonSerializer.Serialize(x)));
        };
        executor.Variables = args.Variables ?? new Dictionary<string, object>();
        executor.AdditionalArguments.Add("Flow", new NodeParameters(null, Logger.Instance, false, null));
        executor.AdditionalArguments.Add("PluginMethod", new Func<string, string, object[], object>((plugin, method, args) =>
            new ExpandoObject()
        ));
        if (executor.Execute() as bool? == false)
        {
            if(error.Contains("MISSING VARIABLE:") == false) // missing variables we don't care about
                throw new Exception(error?.EmptyAsNull() ?? "Invalid script");
        }
    }

    /// <summary>
    /// Saves a script
    /// </summary>
    /// <param name="script">The script to save</param>
    /// <returns>the saved script instance</returns>
    [HttpPost]
    public Script Save([FromBody] Script script)
    {
        ValidateScript(new ValidateScriptModel() { Code = script.Code, Variables = new Dictionary<string, object>()});
        
        if (script?.Code?.StartsWith("// path: ") == true)
            script.Code = Regex.Replace(script.Code, @"^\/\/ path:(.*?)$", string.Empty, RegexOptions.Multiline).Trim();
        
        script.Code = Regex.Replace(script.Code, @"(?<=(from[\s](['""])))(\.\.\/)*Shared\/", "Shared/");
        
        if(ValidScriptName(script.Name) == false)
            throw new Exception("Invalid script name\nCannot contain: " + UnsafeCharacters);
        
        if (SaveScript(script.Name, script.Code, script.Type) == false)
            throw new Exception("Failed to save script");
        if (script.Uid != script.Name)
        {
            if (DeleteScript(script.Uid, script.Type))
            {
                UpdateScriptReferences(script.Uid, script.Name);
            }
            script.Uid = script.Name;
        }

        IncrementConfigurationRevision();

        return script;
    }

    /// <summary>
    /// Increments the revision of the configuration
    /// </summary>
    protected void IncrementConfigurationRevision()
    {
        var service = new SettingsService();
        _ = service.RevisionIncrement();
    }
    private void UpdateScriptReferences(string oldName, string newName)
    {
        var service = new FlowService();
        var flows = service.GetAll();
        foreach (var flow in flows)
        {
            if (flow.Parts?.Any() != true)
                continue;
            bool changed = false;
            foreach (var part in flow.Parts)
            {
                if (part.FlowElementUid == "Script:" + oldName)
                {
                    part.FlowElementUid = "Script:" + newName;
                    changed = true;
                }
            }
            if(changed)
            {
                service.Update(flow);
            }
        }

        var taskService = new TaskService();
        var tasks = taskService.GetAll();
        foreach (var task in tasks)
        {
            if (task.Script != oldName)
                continue;
            task.Script = newName;
            taskService.Update(task);
        }
    }

    /// <summary>
    /// Delete scripts from the system
    /// </summary>
    /// <param name="model">A reference model containing UIDs to delete</param>
    /// <param name="type">The type of scripts being deleted</param>
    /// <returns>an awaited task</returns>
    [HttpDelete]
    public void Delete([FromBody] ReferenceModel<string> model, [FromQuery] ScriptType type = ScriptType.Flow)
    {
        foreach (string m in model.Uids)
        {
            if (ValidScriptName(m) == false)
                continue;
            string file = GetFullFilename(m, type);
            if (System.IO.File.Exists(file) == false)
                continue;
            try
            {
                System.IO.File.Delete(file);
            }
            catch (Exception ex)
            {
                Logger.Instance.ELog($"Failed to delete script '{m}': {ex.Message}");
            }
        }
        IncrementConfigurationRevision();
    }

    private bool DeleteScript(string script, ScriptType type)
    {
        if (ValidScriptName(script) == false)
            return false;
        string file = GetFullFilename(script, type);
        if (System.IO.File.Exists(file) == false)
            return false;
        try
        {
            System.IO.File.Delete(file);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog($"Failed to delete script '{script}': {ex.Message}");
            return false;
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
        // will throw if any errors
        name = name.Replace(".js", "").Replace(".JS", "");
        name = GetNewUniqueName(name);
        return Save(new () { Name = name, Code = code, Repository = false});
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

        script.Name = GetNewUniqueName(name);

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
        return Save(script);
    }

    private string GetNewUniqueName(string name)
    {
        List<string> names = new DirectoryInfo(DirectoryHelper.ScriptsDirectory).GetFiles("*.js", SearchOption.AllDirectories).Select(x => x.Name.Replace(".js", "")).ToList();
        return UniqueNameHelper.GetUnique(name, names);
    }
    
    private bool ValidScriptName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;
        if (name.Contains(".."))
            return false;
        foreach (char c in UnsafeCharacters.Union(Enumerable.Range(0, 31).Select(x => (char)x)))
        {
            if (name.IndexOf(c) >= 0)
                return false;
        }
        return true;
    }

    private string GetFullFilename(string name, ScriptType type)
    {
        string baseDir;
        if (type == ScriptType.Flow)
            baseDir = DirectoryHelper.ScriptsDirectoryFlow;
        else if (type == ScriptType.System)
            baseDir = DirectoryHelper.ScriptsDirectorySystem;
        else if (type == ScriptType.Shared)
            baseDir = DirectoryHelper.ScriptsDirectoryShared;
        else
            baseDir = DirectoryHelper.ScriptsDirectoryFlow;

        var file = new DirectoryInfo(baseDir).GetFiles("*.js", SearchOption.AllDirectories)
            .Where(x => x.Name.ToLower() == name.ToLower() + ".js").FirstOrDefault();
        if (file != null)
            return file.FullName;
        return Path.Combine(baseDir, name + ".js");
    } 

    private bool SaveScript(string name, string code, ScriptType type)
    {
        try
        {
            if(type == ScriptType.Flow) // system scripts dont need to be parsed as they have no parameters
                new ScriptParser().Parse(name ?? string.Empty, code);
            
            if(ValidScriptName(name) == false)
                throw new Exception("Invalid script name:" + name);
            string file = GetFullFilename(name, type);
            System.IO.File.WriteAllText(file, code);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog($"Failed saving script '{name}': {ex.Message}");
            return false;
        }
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
