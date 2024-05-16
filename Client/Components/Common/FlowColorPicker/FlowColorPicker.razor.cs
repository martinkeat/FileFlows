using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Components.Common;

/// <summary>
/// Color picker component
/// </summary>
public partial class FlowColorPicker : ComponentBase
{
    /// <summary>
    /// The UID of the input 
    /// </summary>
    private readonly string Uid = Guid.NewGuid().ToString();

    private string jsValue;

    /// <summary>
    /// Gets or sets the placeholder text
    /// </summary>
    [Parameter]
    public string Placeholder { get; set; }

    /// <summary>
    /// Gets or sets the javascript runtime
    /// </summary>
    [Inject]
    private IJSRuntime jsRuntime { get; set; }

    private string _Value;
    private bool IsPickerOpened = false;
    
#pragma warning disable BL0007
    /// <summary>
    /// Gets or sets the value
    /// </summary>
    [Parameter]
    public string Value
    {
        get => _Value;
        set
        {
            if (_Value == value)
                return;
            _Value = value;
            if(IsPickerOpened == false)
                ValueChanged.InvokeAsync(value);
        }
    }
#pragma warning restore BL0007

    /// <summary>
    /// Gets or sets the callback when the value changes
    /// </summary>
    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    /// <summary>
    /// The javascript textarea object
    /// </summary>
    private IJSObjectReference jsColorPicker;

    private string lblClear;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        this.lblClear = Translater.Instant("Labels.Clear");
        var jsObjectReference = await jsRuntime.InvokeAsync<IJSObjectReference>("import",
            $"./Components/Common/FlowColorPicker/FlowColorPicker.razor.js?v={Globals.Version}");
        
        jsColorPicker =
            await jsObjectReference.InvokeAsync<IJSObjectReference>("createFlowColorPicker",
                DotNetObjectReference.Create(this), Uid, Value);
    }

    /// <summary>
    /// Clears the value
    /// </summary>
    void Clear()
    {
        this.Value = string.Empty;
        this.jsValue = string.Empty;
    }

    /// <summary>
    /// Value has been updated from JavaScript
    /// </summary>
    /// <param name="value">the new value</param>
    /// <returns>a task to await</returns>
    [JSInvokable("updateValue")]
    public Task UpdateValue(string value)
    {
        Value = value;
        StateHasChanged();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Value has been updated from JavaScript
    /// </summary>
    /// <param name="opened">whether not not its opened</param>
    /// <param name="value">the value from the color picker</param>
    /// <returns>a task to await</returns>
    [JSInvokable("pickerOpened")]
    public async Task PickerOpened(bool opened, string value)
    {
        IsPickerOpened = opened;
        Value = value;
        StateHasChanged();
        if(opened == false)
            await ValueChanged.InvokeAsync(value);
    }
}