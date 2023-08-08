using System.Collections;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using FileFlows.Client.Components.Inputs;
using FileFlows.Plugin;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace FileFlows.Client.Components;

/// <summary>
/// Editor for adding a new flow and showing the flow templates
/// </summary>
public partial class NewFlowEditor : Editor
{
    [Inject] NavigationManager NavigationManager { get; set; }
    [CascadingParameter] public Blocker Blocker { get; set; }
    TaskCompletionSource<Flow> ShowTask;

    private FlowType? _flowType = null;
    

    FlowTemplateModel CurrentTemplate;
    private const string FIELD_NAME = "Name";
    private const string FIELD_TEMPLATE = "Template";

    private List<ListOption> TemplateOptions;
    private string lblDescription;
    
    private ElementField efTemplate;
    private readonly Dictionary<string, TemplateFieldModel> TemplateFields = new ();
    private bool InitializingTemplate = false;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        this.TypeName = "Flow";
        this.Title = Translater.Instant("Pages.Flows.Template.Title");
        this.lblDescription = Translater.Instant("Pages.Flows.Template.Fields.PageDescription");
        
        base.SaveCallback = SaveCallback;
    }

    private async Task InitTemplate(FlowTemplateModel template)
    {
        if (Fields?.Any() == true)
            return;
        
        this.InitializingTemplate = true;
        try
        {
            DisposeCurrentTemplate();
            
            if (this.Model is IDictionary<string, object> oldDict && oldDict.TryGetValue("Name", out object oName) &&
                oName is string sName && string.IsNullOrWhiteSpace(sName) == false)
            {
                this.Model = new ExpandoObject();
                ((IDictionary<string, object>)this.Model).Add("Name", sName);
            }
            else
            {
                this.Model = new ExpandoObject();
            }

            var fields = new List<ElementField>();
            fields.Add(new ElementField
            {
                InputType = FormInputType.Text,
                Name = FIELD_NAME,
                Validators = new List<FileFlows.Shared.Validators.Validator>
                {
                    new FileFlows.Shared.Validators.Required()
                }
            });

            if (template?.Fields?.Any() == true)
            {
                foreach (var field in template.Fields)
                {
                    string efName = field.Label.Dehumanize();
                    var ef = new ElementField()
                    {
                        Name = efName,
                        Label = field.Label,
                        HelpText = field.Help,
                        Parameters = new(),
                        InputType = field.Type switch
                        {
                            "Directory" => FormInputType.Folder,
                            "Switch" => FormInputType.Switch,
                            "Select" => FormInputType.Select,
                            "Int" => FormInputType.Int,
                            _ => FormInputType.Text
                        }
                    };
                    if (ef.InputType == FormInputType.Select)
                    {
                        var parameters = JsonSerializer.Deserialize<SelectParameters>(
                            JsonSerializer.Serialize(field.Parameters), new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });
                        ef.Parameters.Add("Options", parameters.Options);
                    }
                    else if (ef.InputType == FormInputType.Int && field.Parameters != null)
                    {
                        var parameters = JsonSerializer.Deserialize<IntParameters>(
                            JsonSerializer.Serialize(field.Parameters), new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });
                        if(parameters.Minimum != 0)
                            ef.Parameters.Add("Min", parameters.Minimum);
                        if(parameters.Maximum != 0)
                            ef.Parameters.Add("Max", parameters.Maximum);
                    }
                    TemplateFields.Add(efName, new (field, ef));
                }

                foreach (var field in template.Fields)
                {
                    var tfm = TemplateFields[field.Label.Dehumanize()];
                    if (field.Conditions?.Any() == true)
                    {
                        foreach (var condition in field.Conditions)
                        {
                            if (TemplateFields.TryGetValue(condition.Property.Dehumanize(),
                                    out TemplateFieldModel efOther) == false)
                                continue;
                            tfm.ElementField.Conditions ??= new();
                            var newCon = new Condition(efOther.ElementField, efOther.TemplateField.Default, condition.Value, condition.IsNot);
                            newCon.Owner = tfm.ElementField;
                            tfm.ElementField.Conditions.Add(newCon);
                        }
                    }

                    fields.Add(tfm.ElementField);
                    if (tfm.TemplateField.Default != null)
                        UpdateValue(tfm.ElementField, tfm.TemplateField.Default);
                    else if(tfm.ElementField.InputType == FormInputType.Switch)
                        UpdateValue(tfm.ElementField, false);
                }
            }
            this.Fields = fields;
            this.StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Error initializing template: " + ex.Message + "\n" + ex.StackTrace);
        } 
        finally
        {
            InitializingTemplate = false;
        }
    }

    /// <summary>
    /// Shows the new flow editor
    /// </summary>
    public Task<Flow> Show(FlowTemplateModel flowTemplateModel)
    {
        ShowTask = new TaskCompletionSource<Flow>();
        Task.Run(async () =>
        {
            this.Model = null;
            this.CurrentTemplate = flowTemplateModel;
            this.Visible = true;
            await this.InitTemplate(flowTemplateModel);
            this.StateHasChanged();
        });
        return ShowTask.Task;
    }

    private void DisposeCurrentTemplate()
    {
        var keys = RegisteredInputs.Keys.ToArray();
        for(int i=keys.Length -1;i >= 0;i--)
        {
            var key = keys[i];
            var input = RegisteredInputs[key];
            
            if (input == null)
                continue;
            if (input?.Field?.Name == "Name")
                continue;
            if (input?.Field?.Name == "Template")
                continue;
            input.Dispose();
            this.RegisteredInputs.Remove(key);
        }
        Fields?.Clear();
        TemplateFields?.Clear();
        Fields ??= new();
    }
    

    private async Task<bool> SaveCallback(ExpandoObject model)
    {
        var dict = model as IDictionary<string, object>;
        var flow = CurrentTemplate.Flow;
        if (dict.TryGetValue("Name", out object oName) && oName is string sName)
            flow.Name = sName;

        foreach (var tfm in TemplateFields.Values)
        {
            var value = dict[tfm.ElementField.Name];
            if (value == null || value.Equals(string.Empty))
                continue;
            
            var part = flow.Parts.FirstOrDefault(x => x.Uid == tfm.TemplateField.Uid);
            if (part == null)
            {
                // flow variable
                if(string.IsNullOrEmpty(tfm.TemplateField.Name))
                    continue;
                if (int.TryParse(value?.ToString() ?? string.Empty, out int iValue))
                    flow.Properties.Variables[tfm.TemplateField.Name] = iValue;
                else if (bool.TryParse(value?.ToString() ?? string.Empty, out bool bValue))
                    flow.Properties.Variables[tfm.TemplateField.Name] = bValue;
                else
                    flow.Properties.Variables[tfm.TemplateField.Name] = value;
                continue;
            }

            if (dict.ContainsKey(tfm.ElementField.Name) == false)
            {
                if (tfm.ElementField.InputType == FormInputType.Switch)
                    dict.Add(tfm.ElementField.Name, false); // special case, switches sometimes dont have their false values set
                else
                    continue;
            }

            if (value?.ToString().StartsWith("OUTPUT:") == true)
            {
                // special case, removing this node and wiring up a different output
                var parts = value.ToString().Split(':');
                var outputNode = flow.Parts.FirstOrDefault(x => x.Uid.ToString() == parts[1]);
                int outputIndex = int.Parse(parts[2]);
                // remove any existing output connections
                outputNode.OutputConnections.RemoveAll(x => x.Output == outputIndex);
                outputNode.OutputConnections.Add(new ()
                {
                    Input = 1,
                    Output = outputIndex,
                    InputNode = Guid.Parse(parts[3])
                });
                flow.Parts.Remove(part);
                continue;
            }
            
            part.Model ??= new ExpandoObject();
            var dictPartModel = (IDictionary<string, object>)part.Model;
            string key = tfm.TemplateField.Name;
            if (string.IsNullOrEmpty(key))
            {
                // no name, means we are setting the entire model
                if (value is JsonElement jEle)
                    part.Model = jEle.Deserialize<ExpandoObject>();
                else if (value != null)
                    part.Model = JsonSerializer.Deserialize<ExpandoObject>(JsonSerializer.Serialize(value));
                else
                    part.Model = null;
                continue;
            }


            if (key == "Node")
            {
                // replace the node type
                part.FlowElementUid = value.ToString();
                continue;
            }

            if(tfm.TemplateField.Value is JsonElement jElement)
            {
                if(jElement.ValueKind == JsonValueKind.Object)
                {
                    var tv = jElement.Deserialize<Dictionary<string, object>>();
                    if (tv.ContainsKey("true") && value?.ToString()?.ToLower() == "true")
                        value = tv["true"];
                    else if(tv.ContainsKey("false") && value?.ToString()?.ToLower() == "false")
                        value = tv["false"];
                    else
                    {
                        // complex object, meaning we are setting properties
                        if (dictPartModel.ContainsKey(key) == false)
                        {
                            dictPartModel.Add(key, new ExpandoObject());
                        }
                        else if (dictPartModel[key] is IDictionary<string, object> == false)
                        {
                            dictPartModel[key] = new ExpandoObject();
                        }
                        dict = dictPartModel[key] as IDictionary<string, object>;
                    
                        foreach (var kv in tv.Keys)
                        {
                            if (dict.ContainsKey(kv))
                                dict[kv] = tv[kv];
                            else
                                dict.Add(kv, tv[kv]);
                        }
                        continue;
                    }
                }
                else if(jElement.ValueKind == JsonValueKind.String)
                {
                    value = jElement.GetString();
                }
                else if (jElement.ValueKind == JsonValueKind.Number)
                {
                    value = jElement.GetDouble();
                }
            }

            // do this after the JsonElement stuff so Value can be converted to true/false etc values
            if (key.StartsWith("Output-"))
            {
                if (Guid.TryParse(value?.ToString() ?? string.Empty, out Guid inputNode) == false)
                {
                    // if the input value means no connection,
                    // eg a switch where true is connecting Delete Original and false connects to nothing
                    continue;
                }

                int output = int.Parse(key.Substring("Output-".Length));
                // the value is then the UID of the node to connect this one too
                part.OutputConnections.Add(new ()
                {
                    Input = 1,
                    Output = output,
                    InputNode = inputNode
                });
                continue;
            }

            if (dictPartModel.ContainsKey(key))
                dictPartModel[key] = value;
            else
                dictPartModel.Add(key, value);
        }

        // shake lose any nodes that have no connections
        // stop at one to skip the input node
        TreeShake(flow);

        Logger.Instance.ILog("Flow", flow);

        if (CurrentTemplate.Save)
        {
            var newFlowResult = await HttpHelper.Put<Flow>("/api/flow", flow);
            if (newFlowResult.Success == false)
            {
                Toast.ShowError(newFlowResult.Body?.EmptyAsNull() ?? "Failed to create new flow");
                return false;
            }
            ShowTask.TrySetResult(newFlowResult.Data);
        }
        else
        {
            flow.Type = _flowType.Value;
            ShowTask.TrySetResult(flow);
        }
        return true;
    }

    /// <summary>
    /// Performs tree shaking on the flow removing any disconnected flow elements or unneeded flow elements
    /// </summary>
    /// <param name="flow">the flow to shake</param>
    private void TreeShake(Flow flow)
    {
        // ensure these aren't null
        flow.Properties ??= new();
        flow.Properties.Variables ??= new();
        
        // loop through looking if If Condition flow elements we can remove
        List<string> variablesToRemove = new();
        for (int i=flow.Parts.Count -1;i>=0;i--)
        {
            var part = flow.Parts[i];
            if (part.FlowElementUid == "FileFlows.BasicNodes.Conditions.IfBoolean")
            {
                // model here is a { Variable: "string" }
                var varResult = GetVariableFromModel<bool>(flow, part);
                if (varResult.Success == false)
                    continue;
                if(variablesToRemove.Contains(varResult.Variable) == false)
                    variablesToRemove.Add(varResult.Variable);

                Guid? newInput = varResult.Value
                    ? part.OutputConnections.Where(x => x.Output == 1).Select(x => x.InputNode).FirstOrDefault()
                    : part.OutputConnections.Where(x => x.Output == 2).Select(x => x.InputNode).FirstOrDefault();
                if (newInput == Guid.Empty)
                    newInput = null;

                var newTargetNode = newInput == null ? "DISCONNECTED" : flow.Parts.Where(x => x.Uid == newInput).Select(x => x.Name?.EmptyAsNull() ?? x.Label?.EmptyAsNull() ?? x.FlowElementUid).First();

                // loop through all other flow parts looking for an output connection to this node
                foreach (var otherPart in flow.Parts)
                {
                    // check if the other flow element has any output connections and not this node
                    if (otherPart == part || otherPart.OutputConnections?.Any() != true)
                        continue;
                    // check each output connection in this other flow element
                    for (int otherIndex = otherPart.OutputConnections.Count - 1; otherIndex>=0;otherIndex--)
                    {
                        var output = otherPart.OutputConnections[otherIndex];
                        // check this other connection is to the current flow element
                        if (output.InputNode != part.Uid)
                            continue;
                        // check if there is actually a connection to be made, or if it should be removed/disconnected
                        if (newInput == null)
                            otherPart.OutputConnections.RemoveAt(otherIndex);
                        else // else set the new connection
                        {
                            string msg =
                                $"Connecting '{otherPart.Name?.EmptyAsNull() ?? otherPart.Label?.EmptyAsNull() ?? otherPart.FlowElementUid}'[{output.Output}] -> '{newTargetNode}'";
                            Logger.Instance.ILog(msg);
                            output.InputNode = newInput.Value;
                        }
                    }
                }
                // remove this if boolean check
                flow.Parts.RemoveAt(i);
                continue;
            }
        }

        // remove any variables that we used in the If Conditions that we no longer need
        foreach (var variable in variablesToRemove)
            flow.Properties.Variables.Remove(variable);

        // if (CurrentTemplate.TreeShake == false)
        //     return;
        
        int count;
        do
        {
            var inputNodes = flow.Parts.SelectMany(x => x.OutputConnections.Select(x => x.InputNode)).ToList();
            count = flow.Parts.Count;
            for (int i = flow.Parts.Count - 1; i >= 1; i--)
            {
                if (inputNodes.Contains(flow.Parts[i].Uid) == false)
                {
                    flow.Parts.RemoveAt(i);
                }
            }
        } while
            (count != flow.Parts
                .Count); // loop over as we may have removed a connection that now makes other nodes disconnected/redundant

        // remove any missing connections
        var partUids = flow.Parts.Select(x => x.Uid).ToList();
        foreach (var part in flow.Parts)
        {
            if (part.OutputConnections?.Any() != true)
                continue;
            for (int i = part.OutputConnections.Count - 1; i >= 0; i--)
            {
                if (partUids.Contains(part.OutputConnections[i].InputNode) == false)
                {
                    part.OutputConnections.RemoveAt(i);
                }
            }
        }
    }

    private (bool Success, string Variable, T Value) GetVariableFromModel<T>(Flow flow, FlowPart part, string variableName = "Variable")
    {
        var dict = part.Model as IDictionary<string, object>;
        if (dict?.TryGetValue(variableName, out object oVariable) != true || oVariable == null)
            return (Success: false, Variable: string.Empty, Value: default);

        string varibleStr;
        if (oVariable is JsonElement jeVariable)
            varibleStr = jeVariable.GetString();
        else
            varibleStr = oVariable.ToString();

        if (flow.Properties.Variables.TryGetValue(varibleStr, out object oValue) != true || oValue == null)
            return (Success: false, Variable: varibleStr, Value: default);
        
        if(oValue is JsonElement je)
        {
            if (je.ValueKind == JsonValueKind.False)
                oValue = false;
            else if (je.ValueKind == JsonValueKind.True)
                oValue = true;
            else if (je.ValueKind == JsonValueKind.String)
                oValue = je.GetString();
            else if (je.ValueKind == JsonValueKind.Number)
                oValue = je.GetInt32();
            else if (je.ValueKind == JsonValueKind.Null)
                oValue = null;
        }

        if (typeof(T) == typeof(bool))
        {
            if (oValue is bool bValue)
                return (true, varibleStr, (T)(object)bValue);
            if(oValue is string sValue)
                return (true, varibleStr, (T)(object)(sValue.ToLowerInvariant() == "true" || sValue  == "1"));
            return (true, varibleStr, (T)(object)false);
        }
        return (Success: false, varibleStr, Value: default);
    }

    private class SelectParameters
    {
        public List<ListOption> Options { get; set; }
    }

    private class IntParameters
    {
        public int Minimum { get; set; }
        public int Maximum { get; set; }
    }
    
    private record TemplateFieldModel(TemplateField TemplateField, ElementField ElementField);
}