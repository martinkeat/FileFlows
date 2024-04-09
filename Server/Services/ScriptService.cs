using System.Dynamic;
using System.Text.RegularExpressions;
using FileFlows.Plugin;
using FileFlows.ScriptExecution;
using FileFlows.Server.Controllers;
using FileFlows.Server.Helpers;
using FileFlows.ServerShared.FileServices;
using FileFlows.ServerShared.Models;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;
using Logger = FileFlows.Shared.Logger;

namespace FileFlows.Server.Services;

/// <summary>
/// A service used to get script data from the FileFlows server
/// </summary>
public class ScriptService:IScriptService
{
    private const string UnsafeCharacters = "<>:\"/\\|?*";
    Regex rgxComments = new Regex(@"\/\*\*(?:.|[\r\n])*?\*\/");
    
    /// <summary>
    /// Get all scripts
    /// </summary>
    /// <returns>a collection of scripts</returns>
    public async Task<IEnumerable<Script>> GetAll()
    {
        List<Script> scripts = new();
        var taskScriptsFlow = GetAllByType(ScriptType.Flow);
        var taskScriptsSystem = GetAllByType(ScriptType.System);
        var taskScriptsShared = GetAllByType(ScriptType.Shared);

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
        var flows = (await ServiceLoader.Load<FlowService>().GetAllAsync()) ?? new ();
        string flowTypeName = typeof(Flow).FullName ?? string.Empty;
        foreach (var flow in flows)
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

        var tasks = await ServiceLoader.Load<TaskService>().GetAllAsync();
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
    /// Gets all scripts by type
    /// </summary>
    /// <param name="type">the type to get</param>
    /// <param name="loadCode">if code should be loaded</param>
    /// <returns>a list of all scripts of the given type</returns>
    public async Task<IEnumerable<Script>> GetAllByType(ScriptType type, bool loadCode = true)
    {
        List<Script> scripts = new();
        string dir = type == ScriptType.Flow ? DirectoryHelper.ScriptsDirectoryFlow : 
            type == ScriptType.Shared ? DirectoryHelper.ScriptsDirectoryShared : 
            type == ScriptType.Template ? DirectoryHelper.ScriptsDirectoryFunction :
            type == ScriptType.Webhook ? DirectoryHelper.ScriptsDirectoryWebhook : 
            DirectoryHelper.ScriptsDirectorySystem;
        if(Directory.Exists(dir) == false)
           return scripts;
        try
        {
            foreach (var file in new DirectoryInfo(dir).GetFiles("*.js", SearchOption.AllDirectories))
            {
                var script = await GetScript(type, file, loadCode);
                scripts.Add(script);
            }

            return scripts.OrderBy(x => x.Name);
        }
        catch (Exception ex)
        {
            Logger.Instance.WLog($"Error getting scripts by type ['{type}']: {ex.Message}");
            return scripts;
        }
    }
    
    /// <summary>
    /// Get a script
    /// </summary>
    /// <param name="name">The name of the script</param>
    /// <param name="type">The type of script</param>
    /// <returns>the script instance</returns>
    public async Task<Script> Get(string name, ScriptType type = ScriptType.Flow)
    {
        var result = FindScript(name, type);
        return await GetScript(type, new FileInfo(result.File), true);
    }

    /// <summary>
    /// Gets or sets a function used to load new instances of the service
    /// </summary>
    /// <param name="name">The name of the script</param>
    /// <param name="type">the type of script</param>
    /// <returns>the script code</returns>
    public async Task<string> GetCode(string name, ScriptType type)
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
    /// <param name="code">the code to validate</param>
    /// <param name="isFunction">if this code is a function or a script block</param>
    /// <param name="variables">the variables to pass for validation</param>
    public void ValidateScript(string code, bool isFunction, Dictionary<string, object> variables)
    {
        var executor = new FileFlows.ScriptExecution.Executor();
        executor.Code = code;
        
        if (isFunction  && executor.Code.IndexOf("function Script") < 0)
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
        executor.Variables = variables ?? new Dictionary<string, object>();
        executor.AdditionalArguments.Add("Flow", new NodeParameters(null, Logger.Instance, false, null, fileService: new LocalFileService())
        {
            AdditionalInfoRecorder = ((s, o, arg3, arg4) => { })
        });
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
    /// <param name="auditDetails">The audit details</param>
    /// <returns>the saved script instance</returns>
    public async Task<Script> Save(Script script, AuditDetails auditDetails)
    {
        ValidateScript(script.Code, false, new Dictionary<string, object>());
        
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
                await UpdateScriptReferences(script.Uid, script.Name, auditDetails);
            }
            script.Uid = script.Name;
        }

        if(script.Name == Globals.FileDisplayNameScript)
            FileDisplayNameService.Initialize();
        else
            IncrementConfigurationRevision();

        return script;
    }

    
    private async Task<Script> GetScript(ScriptType type, FileInfo file, bool loadCode)
    {
        string name = file.Name.Replace(".js", "");
        var code = await System.IO.File.ReadAllTextAsync(file.FullName);
        string comments = rgxComments.Match(code)?.Value ?? string.Empty;
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
            CommentBlock = comments,
            Path = path,
            Code = code
        };
    }
    
    
    /// <summary>
    /// Delete scripts from the system
    /// </summary>
    /// <param name="name">The name of the script to delete</param>
    /// <param name="type">The type of scripts being deleted</param>
    /// <returns>an awaited task</returns>
    public void Delete(string name, ScriptType type = ScriptType.Flow)
    {
        if (ValidScriptName(name) == false)
            return;
        string file = GetFullFilename(name, type);
        if (File.Exists(file) == false)
            return;
        try
        {
            File.Delete(file);
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog($"Failed to delete script '{name}': {ex.Message}");
        }
        IncrementConfigurationRevision();
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
    /// Gets a unique script name
    /// </summary>
    /// <param name="name">the name to get unique</param>
    /// <returns>a unique script name</returns>
    public string GetNewUniqueName(string name)
    {
        List<string> names = new DirectoryInfo(DirectoryHelper.ScriptsDirectory).GetFiles("*.js", SearchOption.AllDirectories).Select(x => x.Name.Replace(".js", "")).ToList();
        return UniqueNameHelper.GetUnique(name, names);
    }
    
    
    private async Task UpdateScriptReferences(string oldName, string newName, AuditDetails auditDetails)
    {
        var service = ServiceLoader.Load<FlowService>();
        var flows = await service.GetAllAsync();
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
                await service.Update(flow, auditDetails);
            }
        }

        var taskService = ServiceLoader.Load<TaskService>();
        var tasks = await taskService.GetAllAsync();
        foreach (var task in tasks)
        {
            if (task.Script != oldName)
                continue;
            task.Script = newName;
            await taskService.Update(task, auditDetails);
        }
    }
    
    
    /// <summary>
    /// Increments the revision of the configuration
    /// </summary>
    protected void IncrementConfigurationRevision()
    {
        var service = new SettingsService();
        _ = service.RevisionIncrement();
    }

    /// <summary>
    /// Checks if a script exists
    /// </summary>
    /// <param name="name">the name of the script</param>
    /// <param name="type">the type of script</param>
    /// <returns>true if exists, otherwise false</returns>
    public bool Exists(string name, ScriptType type)
    {
        string file = GetFullFilename(name, type);
        return File.Exists(file);
    }
}