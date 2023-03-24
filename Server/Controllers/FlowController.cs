using FileFlows.Server.Workers;
using Microsoft.AspNetCore.Mvc;
using FileFlows.Shared.Models;
using FileFlows.Server.Helpers;
using System.Dynamic;
using System.IO.Compression;
using FileFlows.Plugin;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using FileFlows.ScriptExecution;
using FileFlows.Server.Services;
using Logger = FileFlows.Shared.Logger;

namespace FileFlows.Server.Controllers;
/// <summary>
/// Controller for Flows
/// </summary>
[Route("/api/flow")]
public class FlowController : Controller
{
    const int DEFAULT_XPOS = 450;
    const int DEFAULT_YPOS = 50;
    
    private static bool? _HasFlows;
    /// <summary>
    /// Gets if there are any flows
    /// </summary>
    internal static bool HasFlows
    {
        get
        {
            if (_HasFlows == null)
                UpdateHasFlows();
            return _HasFlows == true;
        }
        private set => _HasFlows = value;
    }
    
    /// <summary>
    /// Get all flows in the system
    /// </summary>
    /// <returns>all flows in the system</returns>
    [HttpGet]
    public IEnumerable<Flow> GetAll() => 
        new FlowService().GetAll().OrderBy(x => x.Name.ToLowerInvariant());

    [HttpGet("list-all")]
    public IEnumerable<FlowListModel> ListAll()
    {
        var flows = new FlowService().GetAll();
        List<FlowListModel> list = new List<FlowListModel>();

        foreach(var item in flows)
        {
            list.Add(new FlowListModel
            {
                Default = item.Default,
                Name = item.Name,
                Type = item.Type,
                Uid = item.Uid
            });
        }
        var dictFlows  = list.ToDictionary(x => x.Uid, x => x);
        
        string flowTypeName = typeof(Flow).FullName ?? string.Empty;
        foreach (var flow in flows)
        {
            if (flow?.Parts?.Any() != true)
                continue;
            foreach (var p in flow.Parts)
            {
                if (p.Model == null || p.FlowElementUid != "FileFlows.BasicNodes.Functions.GotoFlow")
                    continue;
                try
                {
                    var gotoModel = JsonSerializer.Deserialize<GotoFlowModel>(JsonSerializer.Serialize(p.Model), new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (gotoModel?.Flow == null || dictFlows.ContainsKey(gotoModel.Flow.Uid) == false)
                        continue;
                    var dictFlow = dictFlows[gotoModel.Flow.Uid];
                    dictFlow.UsedBy ??= new();
                    if (dictFlow.UsedBy.Any(x => x.Uid == flow.Uid))
                        continue;
                    dictFlow.UsedBy.Add(new()
                    {
                        Name = flow.Name,
                        Type = flowTypeName,
                        Uid = flow.Uid
                    });
                }
                catch (Exception)
                {
                }
            }
        }

        string libTypeName = typeof(Library).FullName ?? string.Empty;
        var libraries = new LibraryService().GetAll();
        foreach (var lib in libraries)
        {
            if (lib.Flow == null)
                continue;
            if (dictFlows.ContainsKey(lib.Flow.Uid) == false)
                continue;
            var dictFlow = dictFlows[lib.Flow.Uid];
            if (dictFlow.UsedBy != null && dictFlow.UsedBy.Any(x => x.Uid == lib.Uid))
                continue;
            dictFlow.UsedBy ??= new();
            dictFlow.UsedBy.Add(new()
            {
                Name = lib.Name,
                Type = libTypeName,
                Uid = lib.Uid
            });
        }
        
        return list.OrderBy(x => x.Name.ToLowerInvariant());
    }

    private class GotoFlowModel
    {
        public ObjectReference Flow { get; set; }
    }

    /// <summary>
    /// Gets the failure flow for a particular library
    /// </summary>
    /// <param name="libraryUid">the UID of the library</param>
    /// <returns>the failure flow</returns>
    [HttpGet("failure-flow/by-library/{libraryUid}")]
    public Task<Flow?> GetFailureFlow([FromRoute] Guid libraryUid)
        => new FlowService().GetFailureFlow(libraryUid);

    /// <summary>
    /// Exports a flows
    /// </summary>
    /// <param name="uids">The Flow UIDs</param>
    /// <returns>A download response of the flow(s)</returns>
    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery(Name = "uid")] Guid[] uids)
    {
        var service = new FlowService();
        var flows = uids.Select(x => service.GetByUid(x))
                                    .Where(x => x != null).ToList();
        if (flows.Any() == false)
            return NotFound();
        if (flows.Count() == 1)
        {
            var flow = flows[0];
            string json = JsonSerializer.Serialize(flow, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            byte[] data = System.Text.Encoding.UTF8.GetBytes(json);
            return File(data, "application/octet-stream", flow.Name + ".json");
        }
        
        // multiple, send a zip
        using var ms = new MemoryStream();
        using var zip = new ZipArchive(ms, ZipArchiveMode.Create, true);
        foreach (var flow in flows)
        {
            var json = JsonSerializer.Serialize(flow, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            var fe = zip.CreateEntry(flow.Name + ".json");

            await using var entryStream = fe.Open();
            await using var streamWriter = new StreamWriter(entryStream);
            await streamWriter.WriteAsync(json);
        }
        zip.Dispose();

        ms.Seek(0, SeekOrigin.Begin);
        return File(ms.ToArray(), "application/octet-stream", "Flows.zip");
    }

    /// <summary>
    /// Imports a flow
    /// </summary>
    /// <param name="json">The json data to import</param>
    /// <returns>The newly import flow</returns>
    [HttpPost("import")]
    public Flow Import([FromBody] string json)
    {
        Flow? flow = JsonSerializer.Deserialize<Flow>(json);
        if (flow == null)
            throw new ArgumentNullException(nameof(flow));
        if (flow.Parts == null || flow.Parts.Count == 0)
            throw new ArgumentException(nameof(flow.Parts));

        // generate new UIDs for each part
        foreach (var part in flow.Parts)
        {
            Guid newGuid = Guid.NewGuid();
            json = json.Replace(part.Uid.ToString(), newGuid.ToString());
        }

        // reparse with new UIDs
        var service = new FlowService();
        flow = JsonSerializer.Deserialize<Flow>(json);
        flow.Uid = Guid.Empty;
        flow.Default = false;
        flow.DateModified = DateTime.Now;
        flow.DateCreated = DateTime.Now;
        flow.Name = service.GetNewUniqueName(flow.Name);
        service.Update(flow);
        return flow;
    }


    /// <summary>
    /// Duplicates a flow
    /// </summary>
    /// <param name="uid">The UID of the flow</param>
    /// <returns>The duplicated flow</returns>
    [HttpGet("duplicate/{uid}")]
    public Flow Duplicate([FromRoute] Guid uid)
    { 
        var flow = new FlowService().GetByUid(uid);
        if (flow == null)
            return null;
        
        string json = JsonSerializer.Serialize(flow, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        return Import(json);
    }

    /// <summary>
    /// Generates a template for of a flow
    /// </summary>
    /// <param name="uid">The Flow UID</param>
    /// <returns>A download response of the flow template</returns>
    [HttpGet("template/{uid}")]
    public IActionResult Template([FromRoute] Guid uid)
    {
        var flow = new FlowService().GetByUid(uid);
        if (flow == null)
            return NotFound();

        string json = JsonSerializer.Serialize(flow, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        int count = 1;
        foreach (var p in flow.Parts)
        {
            json = json.Replace(p.Uid.ToString(), $"00000000-0000-0000-0000-{count:000000000000}");
            ++count;
        }

        json = json.Replace("OutputConnections", "connections");
        json = json.Replace("InputNode", "node");
        json = Regex.Replace(json, "\"FlowElementUid\":", "\"node\":");
        json = Regex.Replace(json,
            "\"(Icon|Label|Inputs|Template|Type|Enabled|DateCreated|DateModified)\": [^,}]+,[\\s]*", string.Empty);

        byte[] data = System.Text.Encoding.UTF8.GetBytes(json);
        return File(data, "application/octet-stream", flow.Name + ".json");
    }

    /// <summary>
    /// Sets the enabled state of a flow
    /// </summary>
    /// <param name="uid">The flow UID</param>
    /// <param name="enable">Whether or not the flow should be enabled</param>
    /// <returns>The updated flow</returns>
    [HttpPut("state/{uid}")]
    public Flow SetState([FromRoute] Guid uid, [FromQuery] bool? enable)
    {
        var service = new FlowService();
        var flow = service.GetByUid(uid);
        if (flow == null)
            throw new Exception("Flow not found.");
        if (enable != null)
        {
            flow.Enabled = enable.Value;
            service.Update(flow);
        }

        return flow;
    }

    /// <summary>
    /// Sets the default state of a flow
    /// </summary>
    /// <param name="uid">The flow UID</param>
    /// <param name="isDefault">Whether or not the flow should be the default</param>
    [HttpPut("set-default/{uid}")]
    public void SetDefault([FromRoute] Guid uid, [FromQuery(Name = "default")] bool isDefault = true)
    {
        var service = new FlowService();
        var flow = service.GetByUid(uid);
        if (flow == null)
            throw new Exception("Flow not found.");
        if(flow.Type != FlowType.Failure)
            throw new Exception("Flow not a failure flow.");

        if (isDefault)
        {
            // make sure no others are defaults
            var others = service.GetAll().Where(x => x.Type == FlowType.Failure && x.Default && x.Uid != uid).ToList();
            foreach (var other in others)
            {
                other.Default = false;
                service.Update(other);
            }
        }

        if (isDefault == flow.Default)
            return;

        flow.Default = isDefault;
        service.Update(flow);
    }
    /// <summary>
    /// Delete flows from the system
    /// </summary>
    /// <param name="model">A reference model containing UIDs to delete</param>
    /// <returns>an awaited task</returns>
    [HttpDelete]
    public async Task Delete([FromBody] ReferenceModel<Guid> model)
    {
        if (model?.Uids?.Any() != true)
            return; // nothing to delete
        await new FlowService().Delete(model.Uids);
        UpdateHasFlows();
    }

    private static void UpdateHasFlows()
        => _HasFlows = new FlowService().GetAll().Any();


    /// <summary>
    /// Get a flow
    /// </summary>
    /// <param name="uid">The Flow UID</param>
    /// <returns>The flow instance</returns>
    [HttpGet("{uid}")]
    public async Task<Flow> Get(Guid uid)
    {
        if (uid != Guid.Empty)
        {

            var flow = new FlowService().GetByUid(uid);
            if (flow == null)
                return flow;

            var elements = await GetElements();

            var scripts = (await new ScriptController().GetAll()).Select(x => x.Name).ToList();
            foreach (var p in flow.Parts)
            {
                if (p.Type == FlowElementType.Script && string.IsNullOrWhiteSpace(p.Name))
                {
                    string feName = p.FlowElementUid[7..];
                    // set the name to the script name
                    if (scripts.Contains(feName))
                        p.Name = feName;
                    else
                        p.Name = "Missing Script";
                }

                if (p.FlowElementUid.EndsWith("." + p.Name))
                    p.Name = string.Empty;
                string icon =
                    elements?.Where(x => x.Uid == p.FlowElementUid)?.Select(x => x.Icon)?.FirstOrDefault() ??
                    string.Empty;
                if (string.IsNullOrEmpty(icon) == false)
                    p.Icon = icon;
                p.Label = Translater.TranslateIfHasTranslation(
                    $"Flow.Parts.{p.FlowElementUid.Substring(p.FlowElementUid.LastIndexOf(".") + 1)}.Label",
                    string.Empty);
            }

            return flow;
        }
        else
        {
            // create default flow
            var flowNames = new FlowService().GetAll().Select(x => x.Name).ToList();
            Flow flow = new Flow();
            flow.Parts = new();
            flow.Name = "New Flow";
            flow.Enabled = true;
            int count = 0;
            while (flowNames.Contains(flow.Name))
            {
                flow.Name = "New Flow " + (++count);
            }

            // try find basic node
            var elements = await GetElements();
            var info = elements.Where(x => x.Uid == "FileFlows.BasicNodes.File.InputFile").FirstOrDefault();
            if (info != null && string.IsNullOrEmpty(info.Name) == false)
            {
                flow.Parts.Add(new FlowPart
                {
                    Name = "InputFile",
                    xPos = DEFAULT_XPOS,
                    yPos = DEFAULT_YPOS,
                    Uid = Guid.NewGuid(),
                    Type = FlowElementType.Input,
                    Outputs = 1,
                    FlowElementUid = info.Name,
                    Icon = "far fa-file"
                });
            }

            return flow;
        }
    }


    /// <summary>
    /// Gets all nodes in the system
    /// </summary>
    /// <returns>Returns a list of all the nodes in the system</returns>
    [HttpGet("elements")]
    public async Task<FlowElement[]> GetElements(FlowType type = FlowType.Standard)
    {
        var plugins = await new PluginController().GetAll(includeElements: true);
        var results = plugins.Where(x => x.Enabled && x.Elements != null).SelectMany(x => x.Elements)?.Where(x =>
        {
            if ((int)type == -1) // special case used by get variables, we want everything
                return true;
            if (type == FlowType.Failure)
            {
                if (x.FailureNode == false)
                    return false;
            }
            else if (x.Type == FlowElementType.Failure)
            {
                return false;
            }

            return true;
        })?.ToList();

        // get scripts 
        var scripts = (await new ScriptController().GetAll())?
            .Where(x => x.Type == ScriptType.Flow)
            .Select(x => ScriptToFlowElement(x))
            .Where(x => x != null)
            .OrderBy(x => x.Name); // can be null if failed to parse
        results.AddRange(scripts);

        return results?.ToArray() ?? new FlowElement[] { };
    }

    /// <summary>
    /// Converts a script into a flow element
    /// </summary>
    /// <param name="script"></param>
    /// <returns></returns>
    private FlowElement ScriptToFlowElement(Script script)
    {
        try
        {
            var sm = new ScriptParser().Parse(script?.Name, script?.Code);
            FlowElement ele = new FlowElement();
            ele.Name = script.Name;
            ele.Uid = $"Script:{script.Name}";
            ele.Icon = "fas fa-scroll";
            ele.Inputs = 1;
            ele.Description = sm.Description;
            ele.OutputLabels = sm.Outputs.Select(x => x.Description).ToList();
            int count = 0;
            IDictionary<string, object> model = new ExpandoObject()!;
            ele.Fields = sm.Parameters.Select(x =>
            {
                ElementField ef = new ElementField();
                ef.InputType = x.Type switch
                {
                    ScriptArgumentType.Bool => FormInputType.Switch,
                    ScriptArgumentType.Int => FormInputType.Int,
                    ScriptArgumentType.String => FormInputType.TextVariable,
                    _ => throw new ArgumentOutOfRangeException()
                };
                ef.Name = x.Name;
                ef.Order = ++count;
                ef.Description = x.Description;
                model.Add(ef.Name, x.Type switch
                {
                    ScriptArgumentType.Bool => false,
                    ScriptArgumentType.Int => 0,
                    ScriptArgumentType.String => string.Empty,
                    _ => null
                });
                return ef;
            }).ToList();
            ele.Group = "Scripts";
            ele.Type = FlowElementType.Script;
            ele.Outputs = sm.Outputs.Count;
            ele.Model = model as ExpandoObject;
            return ele;
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Failed converting script to flow element: " + ex.Message + "\n" + ex.StackTrace);
            return null;
        }
    }

    /// <summary>
    /// Saves a flow
    /// </summary>
    /// <param name="model">The flow being saved</param>
    /// <param name="uniqueName">Whether or not a new unique name should be generated if the name already exists</param>
    /// <returns>The saved flow</returns>
    [HttpPut]
    public Flow Save([FromBody] Flow model, [FromQuery] bool uniqueName = false)
    {
        if (model == null)
            throw new Exception("No model");

        if (string.IsNullOrWhiteSpace(model.Name))
            throw new Exception("ErrorMessages.NameRequired");

        
        var service = new FlowService();
        model.Name = model.Name.Trim();
        if (uniqueName == false)
        {
            bool inUse = service.NameInUse(model.Uid, model.Name);
            if (inUse)
                throw new Exception("ErrorMessages.NameInUse");
        }
        else
        {
            model.Name = service.GetNewUniqueName(model.Name);
        }

        if (model.Parts?.Any() != true)
            throw new Exception("Flow.ErrorMessages.NoParts");

        foreach (var p in model.Parts)
        {
            if (Guid.TryParse(p.Name, out Guid guid))
                p.Name = string.Empty; // fixes issue with Scripts being saved as the Guids
            if (string.IsNullOrEmpty(p.Name))
                continue;
            if (p.FlowElementUid.ToLower().EndsWith("." + p.Name.Replace(" ", "").ToLower()))
                p.Name = string.Empty; // fixes issue with flow part being named after the display
        }

        int inputNodes = model.Parts
            .Where(x => x.Type == FlowElementType.Input || x.Type == FlowElementType.Failure).Count();
        if (inputNodes == 0)
            throw new Exception("Flow.ErrorMessages.NoInput");
        else if (inputNodes > 1)
            throw new Exception("Flow.ErrorMessages.TooManyInputNodes");

        if (model.Uid == Guid.Empty && model.Type == FlowType.Failure)
        {
            // if first failure flow make it default
            var others = service.GetAll().Where(x => x.Type == FlowType.Failure).Count();
            if (others == 0)
                model.Default = true;
        }

        bool nameChanged = false;
        if (model.Uid != Guid.Empty)
        {
            // existing, check for name change
            var existing = service.GetByUid(model.Uid);
            nameChanged = existing != null && existing.Name != model.Name;
        }

        service.Update(model);
        if(nameChanged)
            _ = new ObjectReferenceUpdater().RunAsync();

        return model;
    }

    /// <summary>
    /// Rename a flow
    /// </summary>
    /// <param name="uid">The Flow UID</param>
    /// <param name="name">The new name</param>
    /// <returns>an awaited task</returns>
    [HttpPut("{uid}/rename")]
    public async Task Rename([FromRoute] Guid uid, [FromQuery] string name)
    {
        if (uid == Guid.Empty)
            return; // renaming a new flow

        var service = new FlowService();
        var flow = service.GetByUid(uid);
        if (flow == null)
            throw new Exception("Flow not found");
        if (flow.Name == name)
            return; // name already is the requested name

        flow.Name = name;
        service.Update(flow);

        // update any object references
        await new LibraryFileService().UpdateFlowName(flow.Uid, flow.Name);
        new LibraryController().UpdateFlowName(flow.Uid, flow.Name);
    }

    /// <summary>
    /// Get variables for flow parts
    /// </summary>
    /// <param name="flowParts">The flow parts</param>
    /// <param name="partUid">The specific part UID</param>
    /// <param name="isNew">If the flow part is a new part</param>
    /// <returns>The available variables for the flow part</returns>
    [HttpPost("{uid}/variables")]
    public async Task<Dictionary<string, object>> GetVariables([FromBody] List<FlowPart> flowParts,
        [FromRoute(Name = "uid")] Guid partUid, [FromQuery] bool isNew = false)
    {
        var variables = new Dictionary<string, object>();
        bool windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        bool dir = flowParts?.Any(x => x.FlowElementUid.EndsWith("InputDirectory")) == true;

        if (dir)
        {
            variables.Add("folder.Name", "FolderName");
            variables.Add("folder.FullName", windows ? @"C:\Folder\SubFolder" : "/folder/subfolder");
            variables.Add("folder.Date", DateTime.Now);
            variables.Add("folder.Date.Day", DateTime.Now.Day);
            variables.Add("folder.Date.Month", DateTime.Now.Month);
            variables.Add("folder.Date.Year", DateTime.Now.Year);
            variables.Add("folder.OrigName", "FolderOriginalName");
            variables.Add("folder.OrigFullName",
                windows ? @"C:\OriginalFolder\SubFolder" : "/originalFolder/subfolder");
        }
        else
        {
            variables.Add("ext", ".mkv");
            variables.Add("file.Name", "Filename.ext");
            variables.Add("file.NameNoExtension", "Filename");
            variables.Add("file.Extension", ".mkv");
            variables.Add("file.Size", 1000);
            variables.Add("file.FullName",
                windows ? @"C:\Folder\temp\randomfile.ext" : "/media/temp/randomfile.ext");
            variables.Add("file.Orig.Extension", ".mkv");
            variables.Add("file.Orig.FileName", "OriginalFile.ext");
            variables.Add("file.Orig.RelativeName", "files/filename.ext");
            variables.Add("file.Orig.FileNameNoExtension", "OriginalFile");
            variables.Add("file.Orig.FullName",
                windows ? @"C:\Folder\files\filename.ext" : "/media/files/filename.ext");
            variables.Add("file.Orig.Size", 1000);

            variables.Add("file.Create", DateTime.Now);
            variables.Add("file.Create.Day", DateTime.Now.Day);
            variables.Add("file.Create.Month", DateTime.Now.Month);
            variables.Add("file.Create.Year", DateTime.Now.Year);
            variables.Add("file.Modified", DateTime.Now);
            variables.Add("file.Modified.Day", DateTime.Now.Day);
            variables.Add("file.Modified.Month", DateTime.Now.Month);
            variables.Add("file.Modified.Year", DateTime.Now.Year);

            variables.Add("folder.Name", "FolderName");
            variables.Add("folder.FullName", windows ? @"C:\Folder\SubFolder" : "/folder/subfolder");
            variables.Add("folder.Orig.Name", "FolderOriginalName");
            variables.Add("folder.Orig.FullName",
                windows ? @"C:\OriginalFolder\SubFolder" : "/originalFolder/subfolder");
        }

        //p.FlowElementUid == FileFlows.VideoNodes.DetectBlackBars
        var flowElements = await GetElements((FlowType)(-1));
        flowElements ??= new FlowElement[] { };
        var dictFlowElements = flowElements.ToDictionary(x => x.Uid, x => x);

        if (isNew)
        {
            // we add all variables on new, so they can hook up a connection easily
            foreach (var p in flowParts ?? new List<FlowPart>())
            {
                if (dictFlowElements.ContainsKey(p.FlowElementUid) == false)
                    continue;
                var partVariables = dictFlowElements[p.FlowElementUid].Variables ??
                                    new Dictionary<string, object>();
                foreach (var pv in partVariables)
                {
                    if (variables.ContainsKey(pv.Key) == false)
                        variables.Add(pv.Key, pv.Value);
                }
            }

            return variables;
        }

        // get the connected nodes to this part
        var part = flowParts?.Where(x => x.Uid == partUid)?.FirstOrDefault();
        if (part == null)
            return variables;

        List<FlowPart> checkedParts = new List<FlowPart>();

        var parentParts = FindParts(part, 0);
        if (parentParts.Any() == false)
            return variables;

        foreach (var p in parentParts)
        {
            if (dictFlowElements.ContainsKey(p.FlowElementUid) == false)
                continue;

            var partVariables = dictFlowElements[p.FlowElementUid].Variables ?? new Dictionary<string, object>();
            foreach (var pv in partVariables)
            {
                if (variables.ContainsKey(pv.Key) == false)
                    variables.Add(pv.Key, pv.Value);
            }
        }

        return variables;

        List<FlowPart> FindParts(FlowPart part, int depth)
        {
            List<FlowPart> results = new List<FlowPart>();
            if (depth > 30)
                return results; // prevent infinite recursion

            foreach (var p in flowParts ?? new List<FlowPart>())
            {
                if (checkedParts.Contains(p) || p == part)
                    continue;

                if (p.OutputConnections?.Any() != true)
                {
                    checkedParts.Add(p);
                    continue;
                }

                if (p.OutputConnections.Any(x => x.InputNode == part.Uid))
                {
                    results.Add(p);
                    if (checkedParts.Contains(p))
                        continue;
                    checkedParts.Add(p);
                    results.AddRange(FindParts(p, ++depth));
                }
            }

            return results;
        }
    }

    /// <summary>
    /// Gets all the flow template files
    /// </summary>
    /// <returns>a array of all flow template files</returns>
    private FileInfo[] GetTemplateFiles() 
        => new DirectoryInfo(DirectoryHelper.TemplateDirectoryFlow).GetFiles("*.json", SearchOption.AllDirectories);

    /// <summary>
    /// Get flow templates
    /// </summary>
    /// <param name="type">the flow type</param>
    /// <returns>A list of flow templates</returns>
    [HttpGet("templates")]
    public async Task<IDictionary<string, List<FlowTemplateModel>>> GetTemplates([FromQuery] FlowType type = FlowType.Standard)
    {
        var elements = await GetElements((FlowType)(-1)); // special case to load all template typs
        var parts = elements.ToDictionary(x => x.Uid, x => x);

        Dictionary<string, List<FlowTemplateModel>> templates = new();
        string group = string.Empty;
        templates.Add(group, new List<FlowTemplateModel>());
        templates.Add("Basic", new List<FlowTemplateModel>());

        var templateList = GetFlowTemplates(parts)
            .Where(x => x.Template.Type == type)
            .OrderBy(x => x.Template.Group.ToLowerInvariant())
            .ThenBy(x => x.Template.Name == x.Template.Group + " File" ? 0 : 1)
            .ThenBy(x => x.Template.Name.ToLowerInvariant());
        foreach (var item in templateList)
        {

            if (templates.ContainsKey(item.Template.Group ?? String.Empty) == false)
                templates.Add(item.Template.Group ?? String.Empty, new List<FlowTemplateModel>());

            templates[item.Template.Group ?? String.Empty].Add(new FlowTemplateModel
            {
                Fields = item.Template.Fields,
                Save = item.Template.Save,
                Type = item.Template.Type,
                Flow = new Flow
                {
                    Name = item.Template.Name,
                    Template = item.Template.Name,
                    Enabled = true,
                    Description = item.Template.Description,
                    Parts = item.Parts
                }
            });
        }

        return templates;
    }

    private List<(FlowTemplate Template, List<FlowPart> Parts)> GetFlowTemplates(Dictionary<string, FlowElement> parts)
    {
        var templates = new List<(FlowTemplate, List<FlowPart>)>();
        foreach (var tf in GetTemplateFiles())
        {
            try
            {
                string json = System.IO.File.ReadAllText(tf.FullName);
                if (json.StartsWith("// path"))
                {
                    json = string.Join("\n", json.Split('\n').Skip(1)).Trim();
                }
                
                for (int i = 1; i < 50; i++)
                {
                    Guid oldUid = new Guid("00000000-0000-0000-0000-0000000000" + (i < 10 ? "0" : "") + i);
                    Guid newUid = Guid.NewGuid();
                    json = json.Replace(oldUid.ToString(), newUid.ToString());
                }

                json = TemplateHelper.ReplaceWindowsPathIfWindows(json);
                var jst = JsonSerializer.Deserialize<FlowTemplate>(json, new JsonSerializerOptions
                {
                    AllowTrailingCommas = true,
                    PropertyNameCaseInsensitive = true
                });
                if (jst == null)
                    continue;
                try
                {

                    List<FlowPart> flowParts = new List<FlowPart>();
                    int y = DEFAULT_YPOS;
                    bool invalid = false;
                    foreach (var jsPart in jst.Parts)
                    {
                        if (jsPart.Node == null || parts.ContainsKey(jsPart.Node) == false)
                        {
                            invalid = true;
                            break;
                        }

                        var element = parts[jsPart.Node];

                        flowParts.Add(new FlowPart
                        {
                            yPos = jsPart.yPos ?? y,
                            xPos = jsPart.xPos ?? DEFAULT_XPOS,
                            FlowElementUid = element.Uid,
                            Outputs = jsPart.Outputs ?? element.Outputs,
                            Inputs = element.Inputs,
                            Type = element.Type,
                            Name = jsPart.Name ?? string.Empty,
                            Uid = jsPart.Uid,
                            Icon = element.Icon,
                            Model = jsPart.Model,
                            OutputConnections = jsPart.Connections?.Select(x => new FlowConnection
                            {
                                Input = x.Input,
                                Output = x.Output,
                                InputNode = x.Node
                            }).ToList() ?? new List<FlowConnection>()
                        });
                        y += 150;
                    }

                    if (invalid)
                        continue;

                    templates.Add((jst, flowParts));
                }
                catch (Exception ex)
                {
                    Logger.Instance.ELog("Template: " + jst.Name);
                    Logger.Instance.ELog("Error reading template: " + ex.Message + Environment.NewLine +
                                         ex.StackTrace);
                }
                
            }
            catch (Exception ex)
            {
                Logger.Instance.ELog("Error reading template: " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }
        return templates;
    }
}