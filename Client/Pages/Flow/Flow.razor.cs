using System.Collections;
using Microsoft.AspNetCore.Components;
using FileFlows.Client.Components;
using ffPart = FileFlows.Shared.Models.FlowPart;
using ffElement = FileFlows.Shared.Models.FlowElement;
using FFlow = FileFlows.Shared.Models.Flow;
using Microsoft.JSInterop;
using FileFlows.Client.Components.Dialogs;
using System.Text.Json;
using FileFlows.Plugin;
using System.Text.RegularExpressions;
using BlazorContextMenu;
using FileFlows.Client.Components.Common;
using Microsoft.AspNetCore.Components.Web;

namespace FileFlows.Client.Pages;

public partial class Flow : ComponentBase, IDisposable
{
    public Editor Editor { get; set; }
    [Parameter] public System.Guid Uid { get; set; }
    [Inject] INavigationService NavigationService { get; set; }
    [Inject] public IBlazorContextMenuService ContextMenuService { get; set; }
    [CascadingParameter] Blocker Blocker { get; set; }
    [Inject] IHotKeysService HotKeyService { get; set; }
    public ffElement[] Available { get; private set; }
    private ffElement[] AvailablePlugins { get; set; }
    private ffElement[] AvailableScripts { get; set; }
    private ffElement[] AvailableSubFlows { get; set; }

    /// <summary>
    /// The flow element lists
    /// </summary>
    private FlowElementList eleListPlugins, eleListScripts, eleListSubFlows;

    public readonly List<FlowEditor> OpenedFlows = new();
    private FlowEditor ActiveFlow;

    /// <summary>
    /// Gets or sets the instance of the ScriptBrowser
    /// </summary>
    private ScriptBrowser ScriptBrowser { get; set; }
    
    /// <summary>
    /// Gets or sets the instance of the SubFlowBrowser
    /// </summary>
    private SubFlowBrowser SubFlowBrowser { get; set; }
    
    /// <summary>
    /// Gets or sets the properties editor
    /// </summary>
    private FlowPropertiesEditor PropertiesEditor { get; set; }
    /// <summary>
    /// The flow template picker instance
    /// </summary>
    private FlowTemplatePicker TemplatePicker;
    /// <summary>
    /// The Add editor instance
    /// </summary>
    private NewFlowEditor AddEditor;

    private string lblEdit, lblHelp, lblDelete, lblCopy, lblPaste, lblRedo, lblUndo, lblAdd, 
        lblProperties, lblEditSubFlow, lblPlugins, lblScripts, lblSubFlows, lblFields;


    /// <summary>
    /// The default group to show for the `Plugins` flow elements
    /// </summary>
    private string PluginsDefaultGroup;
    
    private int _Zoom = 100;
    private int Zoom
    {
        get => _Zoom;
        set
        {
            if(_Zoom != value)
            {
                _Zoom = value;
                _ = ZoomChanged(value);
            }
        }
    }

    private ElementReference eleFilter { get; set; }

    [Inject] public IJSRuntime jsRuntime { get; set; }
    private bool IsSaving { get; set; }

    private bool EditorOpen = false;

    const string API_URL = "/api/flow";

    private string lblSave, lblSaving, lblClose, lblUnsavedChanges;

    private bool _needsRendering = false;

    private bool ElementsVisible = false;

    private Func<Task<bool>> NavigationCheck;

    /// <summary>
    /// A reference to the floe elements tabs
    /// </summary>
    private FlowTabs tabsFlowElements;

    private FlowTab tabFields;

    /// <summary>
    /// Gets or sets if the active tab is the fields tab
    /// </summary>
    private bool FieldsTabOpened { get; set; }

    /// <summary>
    /// Gets or sets the profile service
    /// </summary>
    [Inject] protected ProfileService ProfileService { get; set; }
    
    /// <summary>
    /// Gets the profile
    /// </summary>
    protected Profile Profile { get; private set; }

    protected override async Task OnInitializedAsync()
    {
        Profile = await ProfileService.Get();
        lblSave = Translater.Instant("Labels.Save");
        lblClose = Translater.Instant("Labels.Close");
        lblSaving = Translater.Instant("Labels.Saving");
        lblAdd = Translater.Instant("Labels.Add");
        lblEdit = Translater.Instant("Labels.Edit");
        lblCopy = Translater.Instant("Labels.Copy");
        lblPaste = Translater.Instant("Labels.Paste");
        lblRedo = Translater.Instant("Labels.Redo");
        lblUndo = Translater.Instant("Labels.Undo");
        lblDelete = Translater.Instant("Labels.Delete");
        lblHelp = Translater.Instant("Labels.Help");
        lblProperties = Translater.Instant("Labels.Properties");
        lblFields = Translater.Instant("Labels.Fields");
        lblEditSubFlow = Translater.Instant("Labels.EditSubFlow");
        lblPlugins = Translater.Instant("Labels.Plugins");
        lblScripts = Translater.Instant("Labels.Scripts");
        lblSubFlows = Translater.Instant("Labels.SubFlows");
        lblUnsavedChanges =Translater.Instant("Labels.UnsavedChanges");

        NavigationCheck = async () =>
        {
            if (OpenedFlows?.Any(x => x.IsDirty) == true)
            {
                bool result = await Confirm.Show(lblUnsavedChanges, $"Pages.{nameof(Flow)}.Messages.Close");
                if (result == false)
                    return false;
            }
            return true;
        };

        NavigationService.RegisterNavigationCallback(NavigationCheck);

        HotKeyService.RegisterHotkey("FlowFilter", "/", callback: () =>
        {
            if (EditorOpen) return;
            Task.Run(async () =>
            {
                await Task.Delay(10);
                await eleFilter.FocusAsync();
            });
        });

        HotKeyService.RegisterHotkey("FlowUndo", "Z", ctrl: true, shift: false, callback: () => Undo());
        HotKeyService.RegisterHotkey("FlowUndo", "Z", ctrl: true, shift: true, callback: () => Redo());
        _ = Init();
    }

    private void OnFlowElementsTabChange(int index)
    {
        bool newValue = FieldsTabOpened;
        if (ActiveFlow?.Flow == null)
            newValue = false;
        else if (ActiveFlow.Flow.Type == FlowType.Failure)
            newValue = false;
        else if (index != 4)
            newValue = false;
        else
            newValue = true;

        if (newValue == FieldsTabOpened)
            return;
        FieldsTabOpened = newValue;
        StateHasChanged();
    }

    public void Dispose()
    {
        HotKeyService.DeregisterHotkey("FlowFilter");
        NavigationService.UnRegisterNavigationCallback(NavigationCheck);
    }


    private async Task Init()
    {
        Blocker.Show();
        StateHasChanged();
        try
        {
            await LoadFlowElements();
            
            if (Uid == Guid.Empty)
            {
                AddFlow(newOnly: true);
                return;
            }

            await OpenFlowInNewTab(Uid);

        }
        catch (Exception ex)
        {
            throw;
        }
        finally
        {
            Blocker.Hide();
            StateHasChanged();
        }
    }

    /// <summary>
    /// Gets or sets the active flows name
    /// </summary>
    public string ActiveFlowName
    {
        get => ActiveFlow?.Flow?.Name;
        set
        {
            if (ActiveFlow?.Flow != null)
            {
                ActiveFlow.Flow.Name = value;
                ActiveFlow.MarkDirty();
            }
        }
    }

    public async Task OpenFlowInNewTab(Guid uid, bool showBlocker = false)
    {
        var existing = OpenedFlows.FirstOrDefault(x => x.Flow.Uid == uid);
        if (existing != null)
        {
            ActivateFlow(existing); // already opened
            return;
        }
        if(showBlocker)
            Blocker.Show("Pages.Flow.Messages.LoadingFlow");
        try
        {

            var modelResult = await GetModel(API_URL + "/" + uid);
            if (modelResult.Success == false || modelResult.Data == null)
            {
                Toast.ShowWarning("Pages.Flow.Messages.FailedToLoadFlow");
                return;
            }

            FFlow flow = (modelResult.Success ? modelResult.Data : null) ?? new FFlow { Parts = new List<ffPart>() };

            var fEditor = new FlowEditor(this, flow, ProfileService);
            await fEditor.Initialize();
            OpenedFlows.Add(fEditor);
            ActivateFlow(fEditor);
        }
        finally
        {
            if (showBlocker)
                Blocker.Hide();
        }
    }

    private async Task LoadFlowElements()
    {
        var elementsResult =
            await GetElements(API_URL + "/elements"); //"?type=" + (int)_FlowType + "&flowUid=" + modelUid);
        if (elementsResult.Success == false)
            return;

        Available = elementsResult.Data;
    }

    private async Task UpdateFlowElementLists()
    {
        PluginsDefaultGroup = ActiveFlow?.Flow?.Type == FlowType.SubFlow ? "Sub Flow" : "File";
        
        if (eleListPlugins == null)
            return; // not initialized yet, no need to do this, the first render will contain the correct data

        await WaitForRender(); // ensures these lists exist or not


        eleListPlugins?.SetItems(AvailablePlugins, PluginsDefaultGroup);
        eleListScripts?.SetItems(AvailableScripts);
        eleListSubFlows?.SetItems(AvailableSubFlows);
    }
    
    private async Task InitializeFlowElements()
    {
        FFlow flow = ActiveFlow?.Flow;
        if (flow == null)
        {
            AvailablePlugins = new ffElement[] { };
            AvailableScripts = new ffElement[] { };
            AvailableSubFlows = new ffElement[] { };
            await UpdateFlowElementLists();
            return;
        }
        
        AvailablePlugins = Available.Where(x => x.Type is not FlowElementType.Script and not FlowElementType.SubFlow 
                                                && x.Uid.Contains(".Conditions.") == false
                                                && x.Uid.Contains(".Templating.") == false)
            .Where(x =>
            {
                if (flow.Type == FlowType.Failure)
                {
                    if (x.FailureNode == false)
                        return false;
                }
                else if (x.Type == FlowElementType.Failure)
                {
                    return false;
                }

                if (flow.Type == FlowType.SubFlow)
                {
                    if (x.Name.EndsWith("GotoFlow"))
                        return false; // don't allow gotos inside a sub flow
                }

                if (flow.Type != FlowType.SubFlow &&
                    (x.Uid.StartsWith("SubFlowInput") || x.Uid.StartsWith("SubFlowOutput")))
                    return false;
                
                return true;
            })
            .ToArray();
        
        AvailableScripts = Available.Where(x => x.Type is FlowElementType.Script).ToArray();

        if (flow.Type == FlowType.Failure)
            AvailableSubFlows = new ffElement[] { };
        else
            AvailableSubFlows = Available.Where(x =>
                    x.Uid != ("SubFlow:" + flow.Uid) &&
                    (x.Type == FlowElementType.SubFlow || x.Uid.Contains(".Conditions.") ||
                     x.Uid.Contains(".Templating.")))
                .ToArray();
        
        await UpdateFlowElementLists();
    }

    private async Task<RequestResult<FFlow>> GetModel(string url)
        => await HttpHelper.Get<FFlow>(url);

    private async Task<RequestResult<ffElement[]>> GetElements(string url)
        => await HttpHelper.Get<ffElement[]>(url);

    private async Task WaitForRender()
    {
        _needsRendering = true;
        StateHasChanged();
        while (_needsRendering)
        {
            await Task.Delay(50);
        }
    }

    async Task Close()
    {
        if (App.Instance.IsMobile && ElementsVisible)
        {
            ElementsVisible = false;
            return;
        }
        await NavigationService.NavigateTo("flows");
    }

    void CloseElements()
    {
        ElementsVisible = false;
        StateHasChanged();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        _needsRendering = false;
    }

    private async Task ZoomChanged(int zoom)
    {
        foreach (var editor in OpenedFlows)
            _ = editor.ffFlow.zoom(zoom);
    }

    private async Task<RequestResult<Dictionary<string, object>>> GetVariables(string url,
        List<FileFlows.Shared.Models.FlowPart> parts)
        => await HttpHelper.Post<Dictionary<string, object>>(url, parts);

    private Dictionary<string, object> EditorVariables;
    
    public async Task<object> Edit(FlowEditor editor, ffPart part, bool isNew, List<ffPart> parts)
    {
        // get flow variables that we can pass onto the form
        Blocker.Show();
        Dictionary<string, object> variables = new Dictionary<string, object>();
        try
        {
            var variablesResult = await GetVariables(API_URL + "/" + part.Uid + "/variables?isNew=" + isNew, parts);
            if (variablesResult.Success)
                variables = variablesResult.Data;
        }
        finally { Blocker.Hide(); }

        // add the flow variables to the variables dictionary
        foreach (var variable in editor.Flow.Properties?.Variables ?? new())
        {
            variables[variable.Key] = variable.Value;
        }


        var flowElement = this.Available.FirstOrDefault(x => x.Uid == part.FlowElementUid);
        if (flowElement == null)
        {
            // cant find it, cant edit
            Logger.Instance.DLog("Failed to locate flow element: " + part.FlowElementUid);
            return null;
        }

        string typeName;
        string typeDisplayName;
        string typeDescription = null; // should leave blank for most things, editor will look it up, for for sub flows etc, use description from that
        if (part.Type == FlowElementType.Script)
        {
            typeName = "Script";
            typeDisplayName = part.FlowElementUid[7..]; // 7 to remove Script:
        }
        else if (part.Type == FlowElementType.SubFlow)
        {
            typeName = "SubFlow";
            var fe = Available.FirstOrDefault(x => part.FlowElementUid == x.Uid);
            typeDisplayName = fe?.Name?.EmptyAsNull() ?? part.Name?.EmptyAsNull() ?? "Sub Flow";
            typeDescription = fe?.Description;
        }
        else
        {
            typeName = part.FlowElementUid[(part.FlowElementUid.LastIndexOf(".", StringComparison.Ordinal) + 1)..];
            typeDisplayName = Translater.TranslateIfHasTranslation($"Flow.Parts.{typeName}.Label", FlowHelper.FormatLabel(typeName));
        }

        var fields = flowElement.Fields == null
            ? new()
            : ObjectCloner.Clone(flowElement.Fields).Select(x =>
            {
                if (FieldsTabOpened)
                    x.CopyValue = $"{part.Uid}.{x.Name}";
                return x;
            }).ToList();
        // add the name to the fields, so a node can be renamed
        fields.Insert(0, new ElementField
        {
            Name = nameof(ffPart.Name),
            Label = Translater.Instant("Labels.Name"),
            Placeholder = typeDisplayName,
            InputType = FormInputType.Text
        });
        fields.Insert(1, new ElementField
        {
            Name = nameof(ffPart.Color),
            Label = Translater.Instant("Flow.Parts.Color"),
            Placeholder = Translater.Instant("Flow.Parts.Color-Placeholder"),
            InputType = FormInputType.Color
        });
        fields.Insert(2, new ElementField
        {
            InputType = FormInputType.HorizontalRule
        });


        if (FieldsTabOpened && part.Type != FlowElementType.Output) // output is for sub flow outputs, we dont want to show the UID
        {
            fields.Insert(0, new ElementField
            {
                Name = "UID",
                InputType = FormInputType.TextLabel,
                ReadOnlyValue = part.Uid.ToString(),
                UiOnly = true
            });
        }

        bool isFunctionNode = flowElement.Uid == "FileFlows.BasicNodes.Functions.Function";

        if (isFunctionNode)
        {
            // special case
            await FunctionNode(fields);
        }


        var model = part.Model ?? new ExpandoObject();
        // add the name/color to the model, since this is actually bound on the part not model, but we need this 
        // so the user can update the name
        if (model is IDictionary<string, object> dict)
        {
            dict["Name"] = part.Name ?? string.Empty;
            dict["Color"] = part.Color ?? string.Empty;
        }

        List<ListOption> flowOptions = null;
        List<ListOption> nodeOptions = null;
        List<ListOption> variableOptions = null;

        foreach (var field in fields)
        {
            field.Variables = variables;
            if(field.Conditions?.Any() == true)
            {
                foreach(var condition in field.Conditions)
                {
                    if (condition.Owner == null)
                        condition.Owner = field;
                    if (condition.Field == null && string.IsNullOrWhiteSpace(condition.Property) == false)
                    {
                        var other = fields.FirstOrDefault(x => x.Name == condition.Property);
                        if (other != null && model is IDictionary<string, object> mdict) 
                        {
                            object otherValue = mdict.ContainsKey(other.Name) ? mdict[other.Name] : null;
                            condition.SetField(other, otherValue);
                        }
                    }
                }
            }
            // special case, load "Flow" into FLOW_LIST
            // this lets a plugin request the list of flows to be shown
            if (field.Parameters?.Any() == true)
            {
                if (field.Parameters.ContainsKey("OptionsProperty") && field.Parameters["OptionsProperty"] is JsonElement optProperty)
                {
                    if (optProperty.ValueKind == JsonValueKind.String)
                    {
                        string optp = optProperty.GetString();
                        Logger.Instance.DLog("OptionsProperty = " + optp);
                        if (optp == "FLOW_LIST")
                        {
                            if (flowOptions == null)
                            {
                                flowOptions = new List<ListOption>();
                                var flowsResult = await HttpHelper.Get<FFlow[]>($"/api/flow");
                                if (flowsResult.Success)
                                {
                                    flowOptions = flowsResult.Data?.Where(x => x.Uid != editor.Flow?.Uid)?.OrderBy(x => x.Name)?.Select(x => new ListOption
                                    {
                                        Label = x.Name,
                                        Value = new ObjectReference
                                        {
                                            Name = x.Name,
                                            Uid = x.Uid,
                                            Type = x.GetType().FullName
                                        }
                                    })?.ToList() ?? new List<ListOption>();
                                }

                            }

                            field.Parameters["Options"] = flowOptions;
                        }
                        else if (optp == "VARIABLE_LIST")
                        {
                            if (variableOptions == null)
                            {
                                variableOptions = new List<ListOption>();
                                var variableResult = await HttpHelper.Get<Variable[]>($"/api/variable");
                                if (variableResult.Success)
                                {
                                    variableOptions = variableResult.Data?.OrderBy(x => x.Name)?.Select(x => new ListOption
                                    {
                                        Label = x.Name,
                                        Value = new ObjectReference
                                        {
                                            Name = x.Name,
                                            Uid = x.Uid,
                                            Type = x.GetType().FullName
                                        }
                                    })?.ToList() ?? new List<ListOption>();
                                }

                            }

                            field.Parameters["Options"] = variableOptions;
                        }
                        else if (optp == "NODE_LIST")
                        {
                            if (nodeOptions == null)
                            {
                                nodeOptions = new List<ListOption>();
                                var flowsResult = await HttpHelper.Get<ProcessingNode[]>($"/api/node");
                                if (flowsResult.Success)
                                {
                                    nodeOptions = flowsResult.Data?.Where(x => x.Enabled)?.OrderBy(x => x.Name)?.Select(x => new ListOption
                                    {
                                        Label = x.Name == "FileFlowsServer" ? "Internal Processing Node" : x.Name,
                                        Value = new ObjectReference
                                        {
                                            Name = x.Name,
                                            Uid = x.Uid,
                                            Type = x.GetType().FullName
                                        }
                                    })?.ToList() ?? new List<ListOption>();
                                }

                            }

                            field.Parameters["Options"] = nodeOptions;
                        }
                    }
                }
            }
        }



        string title = typeDisplayName;
        EditorOpen = true;
        StateHasChanged();
        EditorVariables = variables;
        var newModelTask = Editor.Open(new()
        {
            TypeName = "Flow.Parts." + typeName, Title = title, Fields = fields, Model = model, Description = typeDescription,
            Large = fields.Count > 1, HelpUrl = flowElement.HelpUrl,
            SaveCallback = isFunctionNode ? FunctionSaveCallback : null
        });           
        try
        {
            await newModelTask;
        }
        catch (Exception)
        {
            // can throw if canceled
            return null;
        }
        finally
        {
            EditorOpen = false;
            StateHasChanged();
            // await ffFlow.focusElement(part.Uid.ToString());
        }
        if (newModelTask.IsCanceled == false)
        {
            var newModel = newModelTask.Result;
            int outputs = -1;
            if (part.Model is IDictionary<string, object> dictNew && dictNew != null)
            {
                if (dictNew.TryGetValue("Outputs", out object oOutputs) && int.TryParse(oOutputs?.ToString(), out outputs)) { }
                else if (part.FlowElementUid == "FileFlows.BasicNodes.Functions.Matches")
                {
                    // special case, outputs is determine by the "Matches" count
                    if (dictNew?.TryGetValue("MatchConditions", out object oMatches) == true)
                    {
                        outputs = ObjectHelper.GetArrayLength(oMatches) + 1; // add +1 for not matching
                    }
                }
            }
            ActiveFlow?.MarkDirty();
            return new { outputs, model = newModel };
        }
        else
        {
            return null;
        }
    }
    
    private void ShowElementsOnClick()
    {
        ElementsVisible = !ElementsVisible;
    }

    private async Task<bool> FunctionSaveCallback(ExpandoObject model)
    {
        // need to test code
        var dict = model as IDictionary<string, object>;
        string code  = (dict?.ContainsKey("Code") == true ? dict["Code"] as string : null) ?? string.Empty;
        var codeResult = await HttpHelper.Post<string>("/api/script/validate", new { Code = code, Variables = EditorVariables, IsFunction = true });
        string error = null;
        if (codeResult.Success)
        {
            if (string.IsNullOrEmpty(codeResult.Data))
                return true;
            error = codeResult.Data;
        }
        Toast.ShowError(error?.EmptyAsNull() ?? codeResult.Body, duration: 20_000);
        return false;
    }

    private async Task FunctionNode(List<ElementField> fields)
    {
        var templates = await GetCodeTemplates();
        var templatesOptions = templates.Select(x => new ListOption()
        {
            Label = x.Name,
            Value = x
        }).ToList();
        templatesOptions.Insert(0, new ListOption
        {
            Label = Translater.Instant("Labels.None"),
            Value = null
        });
        var efTemplate = new ElementField
        {
            Name = "Template",
            InputType = FormInputType.Select,                
            UiOnly = true,
            Parameters = new Dictionary<string, object>
            {
                { nameof(Components.Inputs.InputSelect.AllowClear), false },
                { nameof(Components.Inputs.InputSelect.Options), templatesOptions }
            }
        };
        var efCode = fields.FirstOrDefault(x => x.InputType == FormInputType.Code);
        efTemplate.ValueChanged += (object sender, object value) =>
        {
            if (value == null)
                return;
            CodeTemplate template = value as CodeTemplate;
            if (template == null || string.IsNullOrEmpty(template.Code))
                return;
            Editor editor = sender as Editor;
            if (editor == null)
                return;
            if (editor.Model == null)
                editor.Model = new ExpandoObject();
            IDictionary<string, object> model = editor.Model;

            SetModelProperty(nameof(template.Outputs), template.Outputs);
            SetModelProperty(nameof(template.Code), template.Code);
            if (efCode != null)
                efCode.InvokeValueChanged(this, template.Code);

            void SetModelProperty(string property, object value)
            {
                if (model.ContainsKey(property))
                    model[property] = value;
                else
                    model.Add(property, value);
            }
        };
        fields.Insert(2, efTemplate);
    }

    private async Task<List<CodeTemplate>> GetCodeTemplates()
    {
        var templateResponse = await HttpHelper.Get<List<Script>>("/api/script/templates");
        if (templateResponse.Success == false || templateResponse.Data?.Any() != true)
            return new List<CodeTemplate>();

        List<CodeTemplate> templates = new();
        var rgxComments = new Regex(@"\/\*(\*)?(.*?)\*\/", RegexOptions.Singleline);
        var rgxOutputs = new Regex(@"@outputs ([\d]+)");
        foreach (var template in templateResponse.Data)
        {
            var commentBlock = rgxComments.Match(template.Code);
            if (commentBlock.Success == false)
                continue;
            var outputs = rgxOutputs.Match(commentBlock.Value);
            if (outputs.Success == false)
                continue;

            string code = template.Code;
            if (code.StartsWith("// path:"))
                code = string.Join("\n", code.Split('\n').Skip(1));
            code = code.Replace(commentBlock.Value, string.Empty).Trim();

            var ct = new CodeTemplate
            {
                Name = template.Name,
                Code = code,
                Outputs = int.Parse(outputs.Groups[1].Value),
            };
            templates.Add(ct);
        }
        
        return templates.OrderBy(x => x.Name).ToList();
    }

    private async Task EditItem()
    {
        var item = ActiveFlow?.SelectedParts?.FirstOrDefault();
        if (item == null)
            return;
        await ActiveFlow.ffFlow.contextMenu("Edit", item);
    }
    private async Task EditSubFlow()
    {
        var item = ActiveFlow?.SelectedParts?.FirstOrDefault();
        if (item is not { Type: FlowElementType.SubFlow })
            return;

        if (item.FlowElementUid?.StartsWith("SubFlow:") != true)
            return;

        if (Guid.TryParse(item.FlowElementUid[8..], out Guid uid) == false)
            return;

        await OpenFlowInNewTab(uid, showBlocker: true);
    }
    
    private void Copy() => _ = ActiveFlow?.ffFlow?.contextMenu("Copy", ActiveFlow?.SelectedParts);
    private void Paste() => _ = ActiveFlow?.ffFlow?.contextMenu("Paste");
    private void Add() => _ = ActiveFlow?.ffFlow?.contextMenu("Add");
    private void Undo() => _ = ActiveFlow?.ffFlow?.undo();
    private void Redo() => _ = ActiveFlow?.ffFlow?.redo();
    private void DeleteItems() => _ = ActiveFlow?.ffFlow?.contextMenu("Delete", ActiveFlow?.SelectedParts);

    private void AddSelectedElement(string uid)
    {
        ActiveFlow?.ffFlow?.addElementActual(uid, 100, 100);
        this.ElementsVisible = false;
    } 
    
    private async Task OpenHelp()
    {
        await jsRuntime.InvokeVoidAsync("ff.open", "https://fileflows.com/docs/webconsole/configuration/flows/editor");
    }

    private class CodeTemplate
    {
        public string Name { get; init; }
        public string Code { get; init; }
        public int Outputs { get; init; }
    }

    /// <summary>
    /// Arguments passed from JavaScript when opening the context menu
    /// </summary>
    public class OpenContextMenuArgs
    {
        /// <summary>
        /// Gets the X coordinate of the mouse
        /// </summary>
        public int X { get; init; }
        /// <summary>
        /// Gets the Y coordinate of the mouse
        /// </summary>
        public int Y { get; init; }
        /// <summary>
        /// Gets the selected parts
        /// </summary>
        public List<FlowPart> Parts { get; init; }
    }

    /// <summary>
    /// Opens the script browser
    /// </summary>
    private async Task OpenScriptBrowser()
    {
        bool result = await ScriptBrowser.Open(ScriptType.Flow);
        if (result == false)
            return; // no new scripts downloaded

        // force clearing them to update the list
        AvailableScripts = null;
        //StateHasChanged();

        await LoadFlowElements();
        await InitializeFlowElements();
        await UpdateFlowElementLists();
        StateHasChanged();
    }

    /// <summary>
    /// Opens the sub flow browser
    /// </summary>
    private async Task OpenSubFlowBrowser()
    {
        bool result = await SubFlowBrowser.Open();
        if (result == false)
            return; // no new scripts downloaded

        // force clearing them to update the list
        AvailableSubFlows = null;
        //StateHasChanged();

        await LoadFlowElements();
        await InitializeFlowElements();
        await UpdateFlowElementLists();
        StateHasChanged();
    }
    
    private void HandleDragStart((DragEventArgs args, FlowElement element) args)
        => ActiveFlow?.ffFlow?.dragElementStart(args.element.Uid);


    private void ActivateFlow(FlowEditor flow)
    {
        if (flow == ActiveFlow)
            return;
        
        foreach (var other in OpenedFlows)
            other.SetVisibility(other == flow);
        ActiveFlow = flow;
        
        _ = InitializeFlowElements();
        //StateHasChanged();
    }


    public void TriggerStateHasChanged()
        => StateHasChanged();

    private async Task CloseEditor(FlowEditor editor)
    {
        if (editor.IsDirty && await Confirm.Show(lblClose, $"Pages.{nameof(Flow)}.Messages.Close") == false)
            return;
        
        int index = Math.Max(0, OpenedFlows.IndexOf(editor) - 1);
        OpenedFlows.Remove(editor);
        editor.Dispose();
        ActivateFlow(OpenedFlows.Any() ? OpenedFlows[index] : null);
    }

    private async Task SaveEditor(FlowEditor editor)
    {
        this.Blocker.Show(lblSaving);
        this.IsSaving = true;
        try
        {
            var model = await editor.GetModel();
            bool isFirst = OpenedFlows.IndexOf(editor) == 0;
            var result = await HttpHelper.Put<FFlow>(API_URL, model);
            if (result.Success)
            {
                if (isFirst && Uid == Guid.Empty)
                {
                    // update url to save flow
                    await jsRuntime.InvokeVoidAsync("ff.updateUrlWithNewUid", result.Data.Uid.ToString());
                }
                if ((Profile.ConfigurationStatus & ConfigurationStatus.Flows) != ConfigurationStatus.Flows)
                {
                    // refresh the app configuration status
                    await ProfileService.Refresh();
                }

                editor.UpdateModel(result.Data, clean: true);

                if (model.Type == FlowType.SubFlow)
                {
                    // need to refresh the sub flows as their options may have changed
                    await LoadFlowElements();
                }
            }
            else
            {
                Toast.ShowError(
                    result.Success || string.IsNullOrEmpty(result.Body) ? Translater.Instant($"ErrorMessages.UnexpectedError") : Translater.TranslateIfNeeded(result.Body),
                    duration: 60_000
                );
            }
        }
        finally
        {
            this.IsSaving = false;
            this.Blocker.Hide();
        }
    }


    private async Task AddNewFlow(FFlow flow, bool isDirty)
    {
        if (flow.Uid == Guid.Empty)
            flow.Uid = Guid.NewGuid();
        
        FlowEditor editor = new FlowEditor(this, flow, ProfileService);
        editor.IsDirty = isDirty;
        await editor.Initialize();
        bool first = OpenedFlows.Any() == false;
        OpenedFlows.Add(editor);
        ActivateFlow(editor);
        if(this.Zoom != 100)
            _ = editor.ffFlow.zoom(this.Zoom);
        await editor.ffFlow.focusName();
        if (first)
             await WaitForRender();
    }
    
    /// <summary>
    /// Shows the add flow wizard
    /// </summary>
    /// <param name="newOnly">if only new options should be shown and no open existing</param>
    private async void AddFlow(bool newOnly = false)
    {
        var result  = await TemplatePicker.Show((FlowType)(-1));
        if(result == null || result.Result == FlowTemplatePickerResult.ResultCode.Cancel)
            return; // twas canceled
        if (result.Result == FlowTemplatePickerResult.ResultCode.Open)
        {
            if (result.Uid != null)
                await OpenFlowInNewTab(result.Uid.Value, showBlocker: true);
            return;
        }

        var flowTemplateModel = result.Model;
        if (flowTemplateModel.Fields?.Any() != true)
        {
            // nothing extra to fill in, go to the flow editor, typically this if basic flows
            await AddNewFlow(flowTemplateModel.Flow, isDirty: true);
            return;
        }
        
        Logger.Instance.ILog("flowTemplateModel: " , flowTemplateModel);
        var newFlow = await AddEditor.Show(flowTemplateModel);
        if (newFlow == null)
            return; // was canceled
        
        if (newFlow.Uid != Guid.Empty)
        {
            if ((Profile.ConfigurationStatus & ConfigurationStatus.Flows) != ConfigurationStatus.Flows)
            {
                // refresh the app configuration status
                await ProfileService.Refresh();
            }
            await AddNewFlow(newFlow, isDirty: false);
        }
        else
        {
            // edit it
            await AddNewFlow(newFlow, isDirty: true);
        }
    }

} 