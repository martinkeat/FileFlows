using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using FileFlows.Shared;
using FileFlows.Shared.Models;
using ffElement = FileFlows.Shared.Models.FlowElement;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;
using FileFlows.Plugin.Attributes;
using System.Linq;
using System.ComponentModel;
using FileFlows.Client.Components.Inputs;
using FileFlows.Plugin;
using System.Text.Json;
using System.Text.RegularExpressions;
using FileFlows.Client.Components.Common;
using FileFlows.Client.Components.Dialogs;
using Microsoft.JSInterop;

namespace FileFlows.Client.Components;

public partial class Editor : InputRegister, IDisposable
{
    /// <summary>
    /// Gets or sets the JavaScript runtime
    /// </summary>
    [Inject] IJSRuntime jsRuntime { get; set; }
    
    
    /// <summary>
    /// Gets or sets if this is the flow element editor, and if it is, renders slightly differently
    /// </summary>
    [Parameter]
    public bool FlowElementEditor { get; set; }
    

    private readonly List<ActionButton> AdditionalButtons = new();

    public bool Visible { get; set; }

    public string Title { get; set; }
    public string HelpUrl { get; set; }
    public string Icon { get; set; }

    private string Uid = Guid.NewGuid().ToString();
    private bool UpdateResizer; // when set to true, the afterrender method will reinitailize the resizer in javascript
    
    /// <summary>
    /// Gets or sets if inputs should be full width and not use a maximum width
    /// </summary>
    public bool FullWidth { get; set; }
    
    protected bool Maximised { get; set; }

    private RenderFragment FieldsFragment;

    /// <summary>
    /// Get the name of the type this editor is editing
    /// </summary>
    public string TypeName { get; set; }
    protected bool IsSaving { get; set; }

    protected string lblSave, lblSaving, lblNext, lblCancel, lblClose, lblHelp, lblDownloadButton;

    protected List<ElementField> Fields { get; set; }

    protected Dictionary<string, List<ElementField>> Tabs { get; set; }

    public ExpandoObject Model { get; set; }

    TaskCompletionSource<ExpandoObject> OpenTask;

    public delegate Task<bool> SaveDelegate(ExpandoObject model);
    protected SaveDelegate SaveCallback;

    /// <summary>
    /// Gets or sets if the fields scrollbar should be hidden
    /// </summary>
    private bool HideFieldsScroller { get; set; }

    /// <summary>
    /// Gets if the editor has loaded and past the initial render
    /// </summary>
    public bool Loaded { get;private set; }

    protected bool ShowDownload { get; set; }
    /// <summary>
    /// Gets if this editor is readonly
    /// </summary>
    public bool ReadOnly { get; private set; }

    /// <summary>
    /// Gets if a confirmation prompt should be shown if there are changes made when the user cancels the editor
    /// </summary>
    public bool PromptUnsavedChanges { get; private set; }
    public bool Large { get; set; }

    public string EditorDescription { get; set; }


    protected bool FocusFirst = false;
    private bool _needsRendering = false;

    public delegate Task<bool> CancelDeletgate();
    public delegate Task BasicActionDelegate();
    public string DownloadUrl;
    public event CancelDeletgate OnCancel;
    public event BasicActionDelegate OnClosed;

    private string CleanModelJson;


    private RenderFragment _AdditionalFields;
    public RenderFragment AdditionalFields
    {
        get => _AdditionalFields;
        set
        {
            _AdditionalFields = value;
            this.StateHasChanged();
        }
    }

    protected override void OnInitialized()
    {
        lblSave = Translater.Instant("Labels.Save");
        lblSaving = Translater.Instant("Labels.Saving");
        lblCancel = Translater.Instant("Labels.Cancel");
        lblClose = Translater.Instant("Labels.Close");
        lblHelp = Translater.Instant("Labels.Help");
        lblNext = Translater.Instant("Labels.Next");
        this.Maximised = false;
        App.Instance.OnEscapePushed += InstanceOnOnEscapePushed;
    }

    private void InstanceOnOnEscapePushed(OnEscapeArgs args)
    {
        if (args.HasModal || this.Visible == false || args.HasLogPartialViewer)
            return;
        Cancel();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        Loaded = true;

        if(UpdateResizer)
            jsRuntime.InvokeVoidAsync("ff.resizableEditor", this.Uid);
        if (FocusFirst)
        {
            foreach (var input in RegisteredInputs.Values)
            {
                if (input.Focus())
                    break;
            }
            FocusFirst = false;
        }
    }

    private ExpandoObject ConvertToExando(object model)
    {
        if (model == null)
            return new ExpandoObject();
        if (model is ExpandoObject eo)
            return eo;

        var expando = new ExpandoObject();
        var dictionary = (IDictionary<string, object>)expando;

        foreach (var property in model.GetType().GetProperties())
            dictionary.Add(property.Name, property.GetValue(model));
        return expando;
    }

    /// <summary>
    /// Opens an editor
    /// </summary>
    /// <param name="args">the opening arguments</param>
    /// <returns>the updated model from the edit</returns>
    internal Task<ExpandoObject> Open(EditorOpenArgs args)
    {
        this.Loaded = false;
        this.RegisteredInputs.Clear();
        var expandoModel = ConvertToExando(args.Model);
        this.Model = expandoModel;
        this.SaveCallback = args.SaveCallback;
        this.HideFieldsScroller = args.HideFieldsScroller;
        this.PromptUnsavedChanges = args.PromptUnsavedChanges;
        if (args.PromptUnsavedChanges && args.ReadOnly == false) 
            this.CleanModelJson = ModelToJsonForCompare(expandoModel);
        this.TypeName = args.TypeName;
        this.Maximised = false;
        this.FullWidth = args.FullWidth;
        this.Uid = Guid.NewGuid().ToString();
        this.UpdateResizer = true;
        this.AdditionalButtons.Clear();
        if(args.AdditionalButtons?.Any() == true)
            this.AdditionalButtons.AddRange(args.AdditionalButtons);
        if (args.NoTranslateTitle)
            this.Title = args.Title;
        else
            this.Title = Translater.TranslateIfNeeded(args.Title);
        this.Fields = args.Fields;
        this.Tabs = args.Tabs;
        this.ReadOnly = args.ReadOnly;
        this.Large = args.Large;
        this.ShowDownload = string.IsNullOrWhiteSpace(args.DownloadUrl) == false;
        this.lblDownloadButton = Translater.TranslateIfNeeded(args.DownloadButtonLabel);
        this.DownloadUrl = args.DownloadUrl;
        this.Visible = true;
        this.HelpUrl = args.HelpUrl ?? string.Empty;
        this.AdditionalFields = args.AdditionalFields;


        this.lblSave = args.SaveLabel.EmptyAsNull() ?? "Labels.Save";
        this.lblCancel = Translater.TranslateIfNeeded(args.CancelLabel.EmptyAsNull() ?? "Labels.Cancel");

        if (lblSave == "Labels.Save") {
            this.lblSaving = Translater.Instant("Labels.Saving");
            this.lblSave = Translater.Instant(lblSave);
        }
        else
        {
            this.lblSave = Translater.Instant(lblSave);
            this.lblSaving = lblSave;
        }

        this.EditorDescription = Translater.TranslateIfNeeded(args.Description?.EmptyAsNull() ?? (args.TypeName + ".Description"));
        
        BuildFieldsRenderFragment();
        
        OpenTask = new TaskCompletionSource<ExpandoObject>();
        this.FocusFirst = true;
        this.StateHasChanged();
        return OpenTask.Task;
    }

    /// <summary>
    /// Gets the total number of buttons
    /// </summary>
    private int NumberOfButtons =>
        (AdditionalButtons?.Count ?? 0) + (ReadOnly ? 1 : 2) + (string.IsNullOrEmpty(HelpUrl) ? 0 : 1) + (ShowDownload ? 1 : 0); 

    private void BuildFieldsRenderFragment()
    {
        FieldsFragment = (builder) => { };
        this.WaitForRender();
        FieldsFragment = (builder) =>
        {
            int count = -1;
            if (string.IsNullOrEmpty(EditorDescription) == false)
            {
                builder.OpenElement(++count, "div");
                builder.AddAttribute(++count, "class", "description");
                string desc = Markdig.Markdown.ToHtml(EditorDescription).Trim();
                if (desc.StartsWith("<p>") && desc.EndsWith("</p>"))
                    desc = desc[3..^4].Trim();
                desc = desc.Replace("<a ", "<a rel=\"noreferrer\" target=\"_blank\" ");
                builder.AddContent(++count, new MarkupString(desc));
                builder.CloseElement();
            }

            if (Fields?.Any() == true)
            {
                builder.OpenComponent<FlowPanel>(++count);
                builder.AddAttribute(++count,  nameof(FlowPanel.Fields), Fields);
                builder.AddAttribute(++count, nameof(FlowPanel.OnSubmit), EventCallback.Factory.Create(this, OnSubmit));
                builder.AddAttribute(++count, nameof(FlowPanel.OnClose), EventCallback.Factory.Create(this, OnClose));
                builder.CloseComponent();
                if (AdditionalFields != null)
                {
                    builder.AddContent(++count, AdditionalFields);
                }
                if (Fields.Count > 4)
                {
                    builder.OpenElement(++count, "div");
                    builder.AddAttribute(++count, "class", "empty");
                    builder.CloseElement();
                }
            }

            if (Tabs?.Any() == true)
            {
                builder.OpenComponent<FlowTabsBuilder>(++count);
                builder.AddAttribute(++count, nameof(FlowTabsBuilder.Tabs), Tabs);
                builder.AddAttribute(++count, nameof(FlowTabsBuilder.OnSubmit), EventCallback.Factory.Create(this, OnSubmit));
                builder.AddAttribute(++count, nameof(FlowTabsBuilder.OnClose), EventCallback.Factory.Create(this, OnClose));
                builder.CloseComponent();
            }
        };
    }

    protected async Task WaitForRender()
    {
        _needsRendering = true;
        StateHasChanged();
        while (_needsRendering)
        {
            await Task.Delay(50);
        }
    }

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        _needsRendering = false;
        return base.OnAfterRenderAsync(firstRender);
    }


    protected async Task OnSubmit()
    {
        await this.Save();
    }

    protected async Task OnClose()
    {
        this.Cancel();
    }

    protected async Task Save()
    {
        if (ReadOnly)
        {
            Logger.Instance.ILog("Cannot save, readonly");
            return;
        }
        
        bool valid = await this.Validate();
        if (valid == false)
            return;

        if (SaveCallback != null)
        {
            bool saved = await SaveCallback(this.Model);
            if (saved == false)
                return;
        }
        OpenTask?.TrySetResult(this.Model);

        this.Visible = false;
        this.Fields?.Clear();
        this.Tabs?.Clear();
        this.OnClosed?.Invoke();
    }

    protected async void Cancel()
    {
        if(OnCancel != null)
        {
            bool result = await OnCancel.Invoke();
            if (result == false)
                return;
        }

        if (PromptUnsavedChanges && ReadOnly == false)
        {
            string currentModelJson = ModelToJsonForCompare(Model);
            if(currentModelJson != CleanModelJson)
            {
                Logger.Instance.ILog("CleanModelJson");
                Logger.Instance.ILog(CleanModelJson);
                Logger.Instance.ILog("currentModelJson");
                Logger.Instance.ILog(currentModelJson);
                bool confirmResult = await Confirm.Show("Labels.Confirm", "Labels.CancelMessage");
                if(confirmResult == false)
                    return;
            }
        }

        OpenTask?.TrySetCanceled();
        this.Visible = false;
        if(this.Fields != null)
            this.Fields.Clear();
        if(this.Tabs != null)
            this.Tabs.Clear();

        await this.WaitForRender();
        this.OnClosed?.Invoke();
    }

    private string ModelToJsonForCompare(ExpandoObject model)
    {
        string json = model == null ? string.Empty : JsonSerializer.Serialize(Model);
        json = json.Replace("[]", "null");
        // remove default values, templates do not always set these
        json = Regex.Replace(json, @"\""[^\""]+""[\s]*:[\s]*(null|false|0)(,)?", string.Empty);
        while (json.IndexOf(",,") > 0)
            json = json.Replace(",,", ",");
        json = json.Replace(",}", "}");
        return json;
    }

    /// <summary>
    /// Finds a field by its name
    /// </summary>
    /// <param name="name">the name of the field</param>
    /// <returns>the field if found, otherwise null</returns>
    internal ElementField? FindField(string name)
    {
        var field = this.Fields?.Where(x => x.Name == name)?.FirstOrDefault();
        return field;
    }

    /// <summary>
    /// Finds an input by name and its type
    /// </summary>
    /// <param name="name">the element field of the input</param>
    /// <typeparam name="T">the type of field</typeparam>
    /// <returns>the input if found</returns>
    internal T FindInput<T>(string name)
        => (T)this.RegisteredInputs.Values.FirstOrDefault(x => x.Field?.Name == name && x is T);
    
    /// <summary>
    /// Updates a value
    /// </summary>
    /// <param name="field">the field whose value is being updated</param>
    /// <param name="value">the value of the field</param>
    internal void UpdateValue(ElementField field, object value)
    {
        if (field.UiOnly)
            return;
        if (Model == null)
            return;
        var dict = (IDictionary<string, object>)Model;
        if (dict.ContainsKey(field.Name))
            dict[field.Name] = value;
        else
            dict.Add(field.Name, value);
    }

    /// <summary>
    /// Gets a parameter value for a field
    /// </summary>
    /// <param name="field">the field to get the value for</param>
    /// <param name="parameter">the name of the parameter</param>
    /// <param name="default">the default value if not found</param>
    /// <typeparam name="T">the type of parameter</typeparam>
    /// <returns>the parameter value</returns>
    internal T GetParameter<T>(ElementField field, string parameter, T @default = default(T))
    {
        var dict = field?.Parameters as IDictionary<string, object>;
        if (dict?.ContainsKey(parameter) != true)
            return @default;
        var val = dict[parameter];
        if (val == null)
            return @default;
        try
        {
            var converted = Converter.ConvertObject(typeof(T), val);
            T result = (T)converted;
            if(result is List<ListOption> options)
            {
                foreach(var option in options)
                {
                    if(option.Value is JsonElement je)
                    {
                        if (je.ValueKind == JsonValueKind.String)
                            option.Value = je.GetString();
                        else if (je.ValueKind == JsonValueKind.Number)
                            option.Value = je.GetInt32();
                    }
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Failed converted: " + parameter, val);
            return @default;
        }
    }
    
    /// <summary>
    /// Gets the minimum and maximum from a range validator (if exists)
    /// </summary>
    /// <param name="field">The field to get the range for</param>
    /// <returns>the range</returns>
    internal (int min, int max) GetRange(ElementField field)
    {
        var range = field?.Validators?.Where(x => x is FileFlows.Shared.Validators.Range)?.FirstOrDefault() as FileFlows.Shared.Validators.Range;
        return range == null ? (0, 0) : (range.Minimum, range.Maximum);
    }

    /// <summary>
    /// Gets the default of a specific type
    /// </summary>
    /// <param name="type">the type</param>
    /// <returns>the default value</returns>
    private object GetDefault(Type type)
    {
        if(type?.IsValueType == true)
        {
            return Activator.CreateInstance(type);
        }
        return null;
    }

    /// <summary>
    /// Gets a value for a field
    /// </summary>
    /// <param name="field">the field whose value to get</param>
    /// <param name="type">the type of value to get</param>
    /// <returns>the value</returns>
    internal object GetValue(string field, Type type)
    {
        if (Model == null)
        {
            Logger.Instance.ILog("GetValue: Model was null");
            return GetDefault(type);
        }

        var dict = (IDictionary<string, object>)Model;
        if (dict.ContainsKey(field) == false)
        {
            Logger.Instance.ILog("GetValue: Model does not contain key: " + field);
            return GetDefault(type);
        }

        object value = dict[field];
        if (value == null)
        {
            Logger.Instance.ILog("GetValue: value was null");
            return GetDefault(type);
        }

        if (value is JsonElement je)
        {
            Logger.Instance.ILog("GetValue: value is json element");
            if (type == typeof(string))
                return je.GetString();
            if (type== typeof(int))
                return je.GetInt32();
            if (type == typeof(bool))
                return je.GetBoolean();
            if (type == typeof(float))
                return (float)je.GetInt64();
        }

        if (value.GetType().IsAssignableTo(type))
        {
            Logger.Instance.ILog("GetValue: value is assignable to type");
            return value;
        }

        try
        {
            Logger.Instance.ILog($"GetValue: trying to convert to type '{value.GetType()}' to '{type}'");
            return Converter.ConvertObject(type, value);
        }
        catch(Exception ex)
        {
            Logger.Instance.ILog("GetValue: failed converting ot type, returning default: " + ex.Message);
            return GetDefault(type);
        }
    }
    
    /// <summary>
    /// Gets a value for a field
    /// </summary>
    /// <param name="field">the field whose value to get</param>
    /// <param name="default">the default value if none is found</param>
    /// <typeparam name="T">the type of value to get</typeparam>
    /// <returns>the value</returns>
    internal T GetValue<T>(string field, T @default = default(T))
    {
        if (Model == null)
            return @default;
        var dict = (IDictionary<string, object>)Model;
        if (dict.ContainsKey(field) == false)
        {
            return @default;
        }
        object value = dict[field];
        if (value == null)
        {
            return @default;
        }

        if (value is JsonElement je)
        {
            if (typeof(T) == typeof(string))
                return (T)(object)je.GetString();
            if (typeof(T) == typeof(int))
                return (T)(object)je.GetInt32();
            if (typeof(T) == typeof(bool))
            {
                if (je.ValueKind == JsonValueKind.False)
                    return (T)(object)false;
                if (je.ValueKind == JsonValueKind.True)
                    return (T)(object)true;
                if (je.ValueKind == JsonValueKind.String)
                {
                    var str = je.GetString().ToLowerInvariant();
                    return (T)(object)(str == "true" || str == "1");
                }
                if (je.ValueKind == JsonValueKind.Number)
                    return (T)(object)(je.GetInt32() > 0);

                return (T)(object)false;
            }

            if (typeof(T) == typeof(float))
            {
                try
                {
                    return (T)(object)(float)je.GetInt64();
                }
                catch (Exception)
                {
                    return (T)(object)(float.Parse(je.ToString()));
                }
            }
        }

        if (value is T)
        {
            return (T)value;
        }

        try
        {
            return (T)Converter.ConvertObject(typeof(T), value);
        }
        catch(Exception)
        {
            return default;
        }
    }

    protected void OpenHelp()
    {
        if (string.IsNullOrWhiteSpace(HelpUrl))
            return;
        _ = jsRuntime.InvokeVoidAsync("open", HelpUrl.ToLower(), "_blank");
    }

    protected void OnMaximised(bool maximised)
    {
        this.Maximised = maximised;
    }

    /// <summary>
    /// Triggers state has changed on the editor
    /// </summary>
    public void TriggerStateHasChanged() => StateHasChanged();

    public void Dispose()
    {
        App.Instance.OnEscapePushed -= InstanceOnOnEscapePushed;
    }
}

/// <summary>
/// UI Button
/// </summary>
public class ActionButton
{
    private string _Label;

    /// <summary>
    /// Gets or sets the label of the button
    /// </summary>
    public string Label
    {
        get => _Label;
        set => _Label = Translater.TranslateIfNeeded(value);
    }

    /// <summary>
    /// Gets or sets the click action
    /// </summary>
    public Action<object, EventArgs> Clicked { get; set; }
}

/// <summary>
/// Arguments used when opening the editor
/// </summary>
public class EditorOpenArgs
{
    /// <summary>
    /// Gets or sets the type name used in translation
    /// </summary>
    public string TypeName { get; set; }
    /// <summary>
    /// Gets or sets the title of the editor
    /// </summary>
    public string Title { get; set; }
    /// <summary>
    /// Gets or sets the description to show, if not set, editor will try to find a description using the TypeName
    /// </summary>
    public string Description { get; set; }
    /// <summary>
    /// Gets or sets the main fields to show in the editor
    /// </summary>
    public List<ElementField> Fields { get; set; }
    /// <summary>
    /// Gets or sets the model to bind to the editor
    /// </summary>
    public object Model { get; set; }
    /// <summary>
    /// Gets or sets a callback that is called when the editor is saved
    /// </summary>
    public Editor.SaveDelegate SaveCallback  { get; set; }
    /// <summary>
    /// Gets or sets if the editor is readonly
    /// </summary>
    public bool ReadOnly  { get; set; }
    /// <summary>
    /// Gets or sets if the editor is a large editor and takes up more width
    /// </summary>
    public bool Large  { get; set; }

    /// <summary>
    /// Gets or sets if inputs should be full width and not use a maximum width
    /// </summary>
    public bool FullWidth { get; set; }

    /// <summary>
    /// Gets or sets the label to show on the save button
    /// </summary>
    public string SaveLabel  { get; set; }
    /// <summary>
    /// Gets or sets the label to show on the cancel button
    /// </summary>
    public string CancelLabel  { get; set; }
    /// <summary>
    /// Gets or sets any additional fields ot show
    /// </summary>
    public RenderFragment AdditionalFields  { get; set; }
    /// <summary>
    /// Gets or sets the tabs for the editor
    /// </summary>
    public Dictionary<string, List<ElementField>> Tabs { get; set; }
    /// <summary>
    /// Gets or sets the URL for the help button
    /// </summary>
    public string HelpUrl  { get; set; }
    /// <summary>
    /// Gets or sets it the title should not be translated
    /// </summary>
    public bool NoTranslateTitle  { get; set; }
    /// <summary>
    /// Gets or sets the label to shown on the download button
    /// </summary>
    public string DownloadButtonLabel { get; set; } = "Labels.Download";
    /// <summary>
    /// Gets or sets the URL for the download button
    /// </summary>
    public string DownloadUrl { get; set; }
    /// <summary>
    /// Gets or sets if a prompt should be shown the the user if they try to close the editor with changes
    /// </summary>
    public bool PromptUnsavedChanges  { get; set; }
    /// <summary>
    /// Gets or sets 
    /// </summary>
    public IEnumerable<ActionButton> AdditionalButtons { get; set; }

    /// <summary>
    /// Gets or sets if the fields scrollbar should be hidden
    /// </summary>
    public bool HideFieldsScroller { get; set; }
}