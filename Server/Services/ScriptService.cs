using System.Dynamic;
using System.Text.RegularExpressions;
using Esprima;
using FileFlows.Managers;
using FileFlows.Plugin;
using FileFlows.ScriptExecution;
using FileFlows.ServerShared.FileServices;
using FileFlows.ServerShared.Models;
using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;
using Logger = FileFlows.Shared.Logger;

namespace FileFlows.Server.Services;

/// <summary>
/// A service used to get script data from the FileFlows server
/// </summary>
public class ScriptService
{
    /// <summary>
    /// Get all scripts
    /// </summary>
    /// <returns>a collection of scripts</returns>
    public async Task<List<Script>> GetAll()
    {
        List<Script> scripts = (await new ScriptManager().GetAll())
            .Where(x => x.Type is ScriptType.Flow or ScriptType.Shared or ScriptType.System).ToList();
        FileFlowsRepository repo = new FileFlowsRepository();
        try
        {
            repo = await new RepositoryService().GetRepository();
        }
        catch (Exception)
        {
            // silently fail
        }

        var dictFlowScripts = repo.FlowScripts?.Where(x => x.Path != null).ToDictionary(x => x.Path!, x => x.Revision) ?? new ();
        var dictSystemScripts = repo.SystemScripts?.Where(x => x.Path != null).ToDictionary(x => x.Path!, x => x.Revision) ?? new ();
        var dictSharedScripts = repo.SharedScripts?.Where(x => x.Path != null).ToDictionary(x => x.Path!, x => x.Revision) ?? new ();
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
            if (dict.TryGetValue(script.Path, out var value))
                script.LatestRevision = value;
        }
        
        scripts = scripts.DistinctBy(x => x.Name).ToList();
        var dictScripts = scripts.ToDictionary(x => x.Uid, x => x);
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
                if(Guid.TryParse(p.FlowElementUid[7..], out var scriptUid) == false)
                    continue;
                if (dictScripts.TryGetValue(scriptUid, out var script) == false)
                    continue;
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

        var dictScriptUids = scripts.ToDictionary(x => x.Uid, x => x);
        var tasks = await ServiceLoader.Load<TaskService>().GetAllAsync();
        string taskTypeName = typeof(FileFlowsTask).FullName ?? string.Empty;
        foreach (var task in tasks)
        {
            if (dictScriptUids.TryGetValue(task.Script, out var script) == false)
                continue;
            script.UsedBy ??= new();
            script.UsedBy.Add(new ()
            {
                Name = task.Name,
                Type = taskTypeName,
                Uid = task.Uid
            });
        }

        return scripts.OrderBy(x => x.Name.ToLowerInvariant()).ToList();
    }
    
    /// <summary>
    /// Gets all scripts by type
    /// </summary>
    /// <param name="type">the type to get</param>
    /// <returns>a list of all scripts of the given type</returns>
    public async Task<IEnumerable<Script>> GetAllByType(ScriptType type)
        => (await new ScriptManager().GetAll()).Where(x => x.Type == type)
            .OrderBy(x => x.Name)
            .ToList();
    
    /// <summary>
    /// Get a script
    /// </summary>
    /// <param name="uid">The uid of the script</param>
    /// <returns>the script instance</returns>
    public Task<Script?> Get(Guid uid)
        => new ScriptManager().GetByUid(uid);
    
    /// <summary>
    /// Get a script by its name
    /// </summary>
    /// <param name="name">The name of the script</param>
    /// <returns>the script instance</returns>
    public Task<Script?> GetByName(string name)
        => new ScriptManager().GetByName(name);
    
    
    /// <summary>
    /// Validates a script has valid code
    /// </summary>
    /// <param name="code">the code to validate</param>
    public Result<bool> ValidateScript(string code)
    {
        string codeToValidate = code;
        try
        {
            // Replace single-line comments with whitespace
            codeToValidate = Regex.Replace(codeToValidate, @"//.*$", match => new string(' ', match.Length), RegexOptions.Multiline);
            
            // Replace multi-line comments with spaces while preserving line breaks
            codeToValidate = Regex.Replace(codeToValidate, @"/\*[\s\S]*?\*/", match =>
            {
                var replacement = new char[match.Length];
                for (int i = 0; i < match.Length; i++)
                {
                    replacement[i] = match.Value[i] == '\n' ? '\n' : ' ';
                }
                return new string(replacement);
            });

            // Split code into lines
            var lines = codeToValidate.Replace("\r\n", "\n").Split(new[] { '\n' }, StringSplitOptions.None);


            bool inFunction = false;
            // Detect top-level return statements and wrap them
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("function"))
                    inFunction = true;
                
                if (line.StartsWith("export class"))
                    lines[i] = lines[i].Replace("export class", "class");
                else if (line.StartsWith("import "))
                    lines[i] = "";
                else if (inFunction == false && line.StartsWith("return"))
                {
                    lines[i] = "(function() { " + line + " })();";
                }
            }

            // Join lines back into a single string
            codeToValidate = string.Join("\n", lines);

            var parser = new JavaScriptParser();
            parser.ParseScript(codeToValidate);
            return true;
        }
        catch (ParserException ex)
        {
            return Result<bool>.Fail(ex.Message);
        }
    }
    
    /// <summary>
    /// Validates a script has valid code
    /// </summary>
    /// <param name="code">the code to validate</param>
    /// <param name="isFunction">if this code is a function or a script block</param>
    /// <param name="variables">the variables to pass for validation</param>
    public void ValidateScriptOld(string code, bool isFunction, Dictionary<string, object> variables)
    {
        var executor = new Executor();
        executor.Code = code;
        
        if (isFunction  && executor.Code.IndexOf("function Script") < 0)
        {
            executor.Code = "function Script() { " + executor.Code + "\n}\n";
            executor.Code += $"var scriptResult = Script();\nexport const result = scriptResult;";
        }
        
        //executor.SharedDirectory = DirectoryHelper.ScriptsDirectoryShared;
        executor.HttpClient = HttpHelper.Client;
        executor.Logger = new ScriptExecution.Logger();
        string log = String.Empty;
        var logFunction = (object[] args) =>
        {
            log += string.Join(", ", args.Select(x =>
                x == null ? "null" :
                x.GetType().IsPrimitive || x is string ? x.ToString() :
                JsonSerializer.Serialize(x))) + "\n";
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
                JsonSerializer.Serialize(x)));
        };
        executor.Variables = variables ?? new ();
        executor.AdditionalArguments.Add("Flow", new NodeParameters(null, Logger.Instance, false, null, fileService: new LocalFileService())
        {
            AdditionalInfoRecorder = (s, o, arg3, arg4) => { }
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
    public async Task<Result<Script>> Save(Script script, AuditDetails auditDetails)
    {
        //ValidateScript(script.Code, false, new Dictionary<string, object>());
        if (ValidateScript(script.Code).Failed(out var error))
            return Result<Script>.Fail(error);

        var manager = new ScriptManager();

        Script? old = script.Uid != Guid.Empty ? await manager.GetByUid(script.Uid) : null;
        
        if((await manager.Update(script, auditDetails)).Failed(out error))
            return Result<Script>.Fail(error);
        
        if(old != null && old.Name != script.Name)
            await UpdateScriptReferences(old.Name, script.Name, auditDetails);

        if(script.Name == Globals.FileDisplayNameScript)
            ServiceLoader.Load<FileDisplayNameService>().Reinitialize();

        return script;
    }

    /// <summary>
    /// Imports a Script from the repository script
    /// </summary>
    /// <param name="type">the type of scripot to import</param>
    /// <param name="content">the repository script content</param>
    /// <param name="ro">the repository object</param>
    /// <param name="auditDetails">The audit details</param>
    public async Task<Result<bool>> ImportFromRepository(ScriptType type, RepositoryObject ro, string content, AuditDetails? auditDetails)
    {
        var scriptResult = Script.FromCode(ro.Name, content, type);
        if (scriptResult.Failed(out string error))
        {
            Logger.Instance.WLog($"Failed parsing script: {error}");
            return Result<bool>.Fail(error);
        }

        var script = scriptResult.Value;
        script.Type = type;
        script.Path = ro.Path;
        script.Repository = true;
        var result = await Save(script, null);
        if (result.Failed(out error))
        {
            Logger.Instance.WLog($"Failed saving script '{script.Name}': {error}");
            return Result<bool>.Fail(error);
        }

        return true;
    }

    /// <summary>
    /// Delete scripts from the system
    /// </summary>
    /// <param name="uid">The UID of the script to delete</param>
    /// <param name="auditDetails">The audit details</param>
    /// <returns>an awaited task</returns>
    public Task Delete(Guid uid, AuditDetails auditDetails)
        => new ScriptManager().Delete([uid], auditDetails);
    
    
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
    }

    /// <summary>
    /// Gets a new unique name
    /// </summary>
    /// <param name="name">the name to to base it off</param>
    /// <returns>a new unique name</returns>
    public Task<string> GetNewUniqueName(string name)
        => new ScriptManager().GetNewUniqueName(name);

    private static int sharedDirectoryRevision = -1;
    private static FairSemaphore _sharedSemaphore = new(1);

    /// <summary>
    /// Gets the shared directory where shared scripts will be saved to
    /// </summary>
    /// <returns>the shared directory</returns>
    public async Task<string> GetSharedDirectory()
    {
        await _sharedSemaphore.WaitAsync();
        try
        {
            var dir = Path.Combine(DirectoryHelper.DataDirectory, "shared-scripts");
            if (Directory.Exists(dir))
            {
                var currentRevision = await ServiceLoader.Load<ISettingsService>().GetCurrentConfigurationRevision();
                if (currentRevision == sharedDirectoryRevision)
                    return dir;
                sharedDirectoryRevision = currentRevision;
            }
            else
            {
                Directory.CreateDirectory(dir);
            }
            var scripts = (await new ScriptManager().GetAll()).Where(x => x.Type is ScriptType.Shared).ToList();
            foreach (var script in scripts)
            {
                try
                {
                    await File.WriteAllTextAsync(Path.Combine(dir, script.Name + ".js"), script.Code);
                }
                catch (Exception ex)
                {
                    Logger.Instance.WLog($"Failed writing shared script '{script.Name}': " + ex.Message);
                }
            }

            return dir;
        }
        finally
        {
            _sharedSemaphore.Release();
        }
    }

    private List<Script>? FunctionTemplates;

    /// <summary>
    /// Gets the function tempaltes
    /// </summary>
    /// <returns>the function templates</returns>
    public IEnumerable<Script> GetFunctionTemplates()
    {
        if(FunctionTemplates == null)
            RescanFunctionTemplates();
        return FunctionTemplates ?? [];
    }

    /// <summary>
    /// Rescans the function templates
    /// </summary>
    public void RescanFunctionTemplates()
    {
        var dir = DirectoryHelper.ScriptsDirectoryFunction;
        if (Directory.Exists(dir) == false)
        {
            Logger.Instance.WLog("Script Function Template Directory does not exist: " + dir);
            return;
        }

        FunctionTemplates = new();
        foreach (var js in new DirectoryInfo(dir).GetFiles("*.js", SearchOption.AllDirectories))
        {
            FunctionTemplates.Add(new ()
            {
                Name = js.Name[..^3], // remove the .js
                Code = File.ReadAllText(js.FullName)
            });
        }
    }
}