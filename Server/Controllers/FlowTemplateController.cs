using System.Text.RegularExpressions;
using FileFlows.Plugin;
using FileFlows.Server.Helpers;
using FileFlows.Server.Services;
using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace FileFlows.Server.Controllers;


/// <summary>
/// Controller for Flow Templates
/// </summary>
[Route("/api/flow-template")]
public class FlowTemplateController : Controller
{
    const int DEFAULT_XPOS = 450;
    const int DEFAULT_YPOS = 50;
    private static List<FlowTemplateModel> Templates;
    private static DateTime FetchedAt = DateTime.MinValue;
    private static readonly string FlowTemplatesFile = Path.Combine(DirectoryHelper.TemplateDirectoryFlow, "flow-templates.json");
    
    /// <summary>
    /// Gets all the flow templates
    /// </summary>
    /// <param name="type">the type of templates to get</param>
    /// <returns>all the flow templates</returns>
    [HttpGet]
    public async Task<List<FlowTemplateModel>> GetAll([FromQuery] FlowType type = FlowType.Standard)
    {
        if (Templates == null || FetchedAt < DateTime.Now.AddMinutes(-10))
            await RefreshTemplates();
        var plugins = new PluginService().GetAll().Where(x => x.Enabled)
            .Select(x => x.Name.Replace(" ", string.Empty).ToLowerInvariant())
            .ToList();
        var templateList = Templates.Where(x => x.Type == type).ToList();
        foreach (var template in templateList)
        {
            template.MissingDependencies = template.Plugins.Where(pl =>
                plugins.Contains(pl.ToLowerInvariant().Replace(" ", String.Empty)) == false)
                .ToList();
        }
        return (templateList ?? new()).Union(LocalFlows()).ToList();
    }

    private List<FlowTemplateModel> LocalFlows()
    {
        var flows = new FlowService().GetAll().Where(x => x.Properties?.Fields?.Any() == true 
                                                          && string.IsNullOrWhiteSpace(x.Properties?.Author) == false
                                                          && string.IsNullOrWhiteSpace(x.Properties?.Description) == false).OrderBy(x => x.Name).ToList();
        var results = new List<FlowTemplateModel>();
        foreach (var flow in flows)
        {
            var ftm = new FlowTemplateModel();
            ftm.Author = flow.Properties.Author;
            ftm.Description = flow.Properties.Description;
            ftm.Fields = FlowFieldToTemplateField(flow);
            ftm.Path = "local:" + flow.Uid;
            ftm.Name = flow.Name;
            ftm.Revision = flow.Revision;
            ftm.Tags = flow.Properties.Tags?.ToList() ?? new ();
            ftm.Tags.Add("Local");
            ftm.Type = flow.Type;
            results.Add(ftm);
        }
        return results;
    }

    /// <summary>
    /// Fetches a flow template
    /// </summary>
    /// <param name="model">the flow to fetch</param>
    /// <returns>the flow</returns>
    [HttpPost]
    public async Task<IActionResult> FetchTemplate([FromBody] FlowTemplateModel model)
    {
        string json;
        if (model.Path.StartsWith("local:"))
        {
            var uid = Guid.Parse(model.Path[6..]);
            var tFlow = new FlowService().GetByUid(uid);
            json = JsonSerializer.Serialize(tFlow); // we serialize this so any changes we make arent on the original flow object
        }
        else
        {
            string fileName = Path.Combine(DirectoryHelper.TemplateDirectoryFlow,
                MakeSafeFilename(model.Path.Replace(".json", "_" + model.Revision + ".json")));
            if (System.IO.File.Exists(fileName) == false)
            {
                // need to download the flow template from github
                var result =
                    await HttpHelper.Get<string>(
                        "https://raw.githubusercontent.com/revenz/FileFlowsRepository/master/" + model.Path);
                if (result.Success == false)
                    return BadRequest("Failed to download from repository");
                await System.IO.File.WriteAllTextAsync(fileName, result.Data);
                json = result.Data;
            }
            else
            {
                json = await System.IO.File.ReadAllTextAsync(fileName);
            }
        }

        json = TemplateHelper.ReplaceOutputPathVariable(json);

        var flow = JsonSerializer.Deserialize<Flow>(json);
        
        model.Fields = FlowFieldToTemplateField(flow);
        model.Flow = flow;
        return Ok(model);
    }

    /// <summary>
    /// Refreshes the templates
    /// </summary>
    async Task RefreshTemplates()
    {
        var result = await HttpHelper.Get<List<FlowTemplateModel>>(
            "https://raw.githubusercontent.com/revenz/FileFlowsRepository/master/flows.json?dt=" +
            DateTime.UtcNow.Ticks);

        
        if (result.Success == false)
        {
            // try loading from disk
            LoadFlowTemplatesFromLocalStorage();
        }
        else
        {
            Templates = result.Data;
            FetchedAt = DateTime.Now;

            // save to disk
            string json = JsonSerializer.Serialize(result.Data);
            _ = System.IO.File.WriteAllTextAsync(FlowTemplatesFile, json);
        }
        Templates = Templates.OrderBy(x => x.Name.IndexOf(" ", StringComparison.Ordinal) < 0 ? 1 : Regex.IsMatch(x.Name, "^[\\w]+ File") ? 2 : 3)
            .ThenBy(x => x.Author == "FileFlows" ? 1 : 2)
            .ThenBy(x => Regex.IsMatch(x.Name, @"^Convert [\w]+$") ? 1 : 2)
            .ThenBy(x => x.Name)
            .ToList();
    }

    /// <summary>
    /// Loads the flows templates from the local storage
    /// </summary>
    private void LoadFlowTemplatesFromLocalStorage()
    {
        if(System.IO.File.Exists(FlowTemplatesFile) == false)
            return;
        try
        {
            string json = System.IO.File.ReadAllText(FlowTemplatesFile);
            Templates = JsonSerializer.Deserialize<List<FlowTemplateModel>>(json);
            FetchedAt = DateTime.Now;
        }
        catch (Exception ex)
        {
            Logger.Instance.WLog("Error parsing flow-templates.json, deleting:  " + ex.Message);
            System.IO.File.Delete(FlowTemplatesFile);
        }
    }
    
    /// <summary>
    /// Makes a string safe for use as a filename by removing or replacing invalid characters.
    /// </summary>
    /// <param name="input">The input string representing the desired filename.</param>
    /// <returns>A safe filename with invalid characters replaced by underscores.</returns>
    static string MakeSafeFilename(string input)
    {
        // Remove or replace invalid characters
        string invalidCharsRegex = string.Join("", Path.GetInvalidFileNameChars());
        string safeFilename = Regex.Replace(input, "[" + Regex.Escape(invalidCharsRegex) + "]", "_");

        return safeFilename;
    }


    private (bool Success, FlowTemplate? Template, Flow Flow) GetFlowTemplate(Dictionary<string, FlowElement> parts, string json)
    {
        try
        {
            if (json.StartsWith("//"))
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
            FlowTemplate jst = JsonSerializer.Deserialize<FlowTemplate>(json, new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true
            });

            if (jst == null)
                return (false, null, null);

            try
            {

                List<FlowPart> flowParts = new ();
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
                    return (false, null, null);

                var flow = new Flow()
                {
                    Template = jst.Name,
                    Name = jst.Name,
                    Type = jst.Type,
                    Uid = Guid.Empty,
                    Parts = flowParts,
                    Enabled = true,
                    Properties = new ()
                    {
                        Author = jst.Author,
                        Description = jst.Description
                    }
                };

                return (true, jst, flow);
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
        return (false, null, null);
    }

    /// <summary>
    /// Converts flow fields to template fields
    /// </summary>
    /// <param name="flow">the flow</param>
    /// <returns>the results</returns>
    private List<TemplateField> FlowFieldToTemplateField(Flow flow)
    {
        List<TemplateField> results = new();
        foreach (var field in flow.Properties?.Fields ?? new())
        {
            var tf = new TemplateField();
            tf.Name = field.Name;
            tf.Label = field.Name.Replace("_" , " ");
            tf.Default = field.DefaultValue;
            tf.Help = field.Description;
            if (string.IsNullOrWhiteSpace(field.FlowElementField) == false && Regex.IsMatch(field.FlowElementField,
                    @"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\.[a-zA-Z_][a-zA-Z0-9_]*$"))
            {
                // this is a strong name to a field
                var parts = field.FlowElementField.Split('.');
                tf.Uid = Guid.Parse(parts[0]);
                tf.Name = parts[1];
            }
            tf.Type = field.Type switch
            {
                FlowFieldType.Directory => "Directory",
                FlowFieldType.Boolean => "Switch",
                FlowFieldType.Number => "Int",
                FlowFieldType.Select => "Select",
                _ => "Text"
            };

            if (field.Type == FlowFieldType.Directory && string.IsNullOrWhiteSpace(tf.Default as string))
                tf.Default = DirectoryHelper.GetUsersHomeDirectory();

            if (field.Type == FlowFieldType.Select)
            {
                tf.Parameters = new
                {
                    options = field.Options.Select(x =>
                    {
                        var parts = x.Split('|');
                        if (parts.Length == 1)
                            return new { label = parts[0], value = parts[0] };
                        return new { label = parts[0], value = parts[1] };
                    })
                };
            }
            
            results.Add(tf);

            if (string.IsNullOrWhiteSpace(field.IfName))
                continue;
            var other = flow.Properties.Fields.FirstOrDefault(x => x.Name == field.IfName);
            if (other == null)
                continue;

            var condition = new Condition();
            condition.Property = other.Name;
            if (other.Type == FlowFieldType.Boolean)
                condition.Value = field.IfValue?.ToLowerInvariant()?.Trim() == "true";
            else if (other.Type == FlowFieldType.Number && int.TryParse(field.IfValue?.Trim(), out int iOther))
                condition.Value = iOther;
            else
                condition.Value = field.IfValue;
            condition.IsNot = field.IfNot;
            tf.Conditions ??= new();
            tf.Conditions.Add(condition);
        }

        return results;
    }
}