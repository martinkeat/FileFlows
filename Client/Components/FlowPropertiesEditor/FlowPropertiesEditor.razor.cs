using System.Text;
using FileFlows.Client.Components.Dialogs;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Components;

/// <summary>
/// Flow Properties editor
/// </summary>
public partial class FlowPropertiesEditor
{
    private List<FlowField> Fields => Flow.Properties.Fields;
    /// <summary>
    /// Gets or sets the JavaScript runtime
    /// </summary>
    [Inject] IJSRuntime jsRuntime { get; set; }
    
    /// <summary>
    /// Gets or sets the flow
    /// </summary>
    [Parameter] public Flow Flow { get; set; }

    private FlowField Editing;
    protected string lblClose, lblHelp, lblTitle;
    private bool Visible;

    private List<KeyValuePair<string, string>> _FlowVariables;
    private List<KeyValuePair<string, string>> FlowVariables
    {
        get => _FlowVariables;
        set
        {
            _FlowVariables = value ?? new ();
            Flow.Properties.Variables = _FlowVariables.ToDictionary<KeyValuePair<string, string>, string, object>(x => x.Key, x =>
            {
                if (int.TryParse(x.Value, out int iValue))
                    return iValue;
                if (bool.TryParse(x.Value, out bool bValue))
                    return bValue;
                return x.Value;
            });
        }
    }

    protected override void OnInitialized()
    {
        lblTitle = Translater.Instant("Labels.FlowProperties");
        lblClose = Translater.Instant("Labels.Close");
        lblHelp = Translater.Instant("Labels.Help");
        _FlowVariables = Flow.Properties.Variables?.Select(x => new KeyValuePair<string, string>(x.Key, x.Value.ToString()))
            ?.ToList() ?? new ();
    }

    /// <summary>
    /// Closes the properties editor
    /// </summary>
    public void Close()
    {
        Visible = false;
        StateHasChanged();
    }

    /// <summary>
    /// Shows the the editor
    /// </summary>
    public void Show()
    {
        Visible = true;
        StateHasChanged();
    }

    // Opens the helper
    void OpenHelp()
        => _ = jsRuntime.InvokeVoidAsync("open", "https://fileflows.com/docs/pages/flows/properties", "_blank");

    /// <summary>
    /// Adds a new property variable
    /// </summary>
    void Add()
        => Fields.Add(new());

    /// <summary>
    /// Edits a field
    /// </summary>
    /// <param name="item">the field to edit</param>
    void Edit(FlowField item)
        => Editing = item;

    /// <summary>
    /// Deletes a field
    /// </summary>
    /// <param name="item">the field to delete</param>
    async Task Delete(FlowField item)
    {
        if (await Confirm.Show("Labels.Delete", "Are you sure you want to delete this field?") == false)
            return;
        Fields.Remove(item);
        StateHasChanged();
    }

    /// <summary>
    /// Gets or sets the the default string value
    /// </summary>
    public string DefaultValueString
    {
        get => Editing?.DefaultValue as string ?? string.Empty;
        set
        {
            if (Editing?.Type == FlowFieldType.String || Editing?.Type == FlowFieldType.Directory)
                Editing.DefaultValue = value;
        } 
    }

    /// <summary>
    /// Gets or sets the default boolean value
    /// </summary>
    public bool DefaultValueBoolean
    {
        get => Editing?.DefaultValue as bool? == true;
        set
        {
            if (Editing?.Type == FlowFieldType.Boolean)
                Editing.DefaultValue = value;
        } 
    }
    /// <summary>
    /// Gets or sets the default number value
    /// </summary>
    public int DefaultValueNumber
    {
        get => Editing?.DefaultValue as int? ?? 0;
        set
        {
            if (Editing?.Type == FlowFieldType.Number)
                Editing.DefaultValue = value;
        } 
    }
}
