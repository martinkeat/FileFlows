using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Components.Common;

/// <summary>
/// Flow drop down component
/// </summary>
public partial class FlowDropDown : ComponentBase, IDisposable
{
    /// <summary>
    /// Gets or sets the options to show
    /// </summary>
    [Parameter] public List<DropDownOption> Options { get; set; }
    
    /// <summary>
    /// Gets or sets the JS Runtime
    /// </summary>
    [Inject] public IJSRuntime jsRuntime { get; set; }

    /// <summary>
    /// The UID of this component
    /// </summary>
    private readonly string Uid = Guid.NewGuid().ToString(); 
    
    /// <summary>
    /// The dotnet reference to this component
    /// </summary>
    private DotNetObjectReference<FlowDropDown> dotnetObjRef;
    
    /// <summary>
    /// If this drop down is opened or not
    /// </summary>
    private bool Opened = false;
    
    /// <summary>
    /// The selected value
    /// </summary>
    private DropDownOption Selected { get; set; }

    /// <summary>
    /// Gets or sets the placeholder text to show when no item is selected 
    /// </summary>
    [Parameter] public string Placeholder { get; set; } = "Pick one";
    
    /// <summary>
    /// Gets or sets the placeholder icon to show when no item is selected
    /// </summary>
    [Parameter] public string PlaceholderIcon { get; set; }
    
    [Parameter] public EventCallback<object?> OnSelected { get; set; }

    private object _SelectedValue;
    
#pragma warning disable BL0007
    [Parameter]
    public object SelectedValue
    {
        get => _SelectedValue;
        set
        {
            if (_SelectedValue == value)
                return;
            if (_SelectedValue?.ToString() == value?.ToString())
                return;
            
            _SelectedValue = value;
            if (value != null)
                Selected = Options.FirstOrDefault(x => x.Value?.ToString() == value?.ToString());
            StateHasChanged();
        }
    }
#pragma warning restore BL0007

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            dotnetObjRef = DotNetObjectReference.Create(this);
            await jsRuntime.InvokeVoidAsync("ff.handleClickOutside", Uid, dotnetObjRef);
        }
    }
    
    /// <summary>
    /// Handles when the user clicked outside this component
    /// </summary>
    [JSInvokable]
    public void OnOutsideClick()
    {
        if (Opened == false)
            return;
        
        Opened = false;
        StateHasChanged();
    }
    
    /// <summary>
    /// Toggles this drop down being opened
    /// </summary>
    private void Toggle()
        => Opened = !Opened;

    /// <summary>
    /// Selected an option from the drop down
    /// </summary>
    /// <param name="option">the option to select</param>
    private async Task Select(DropDownOption option)
    {
        Selected = option;
        Opened = false;
        await OnSelected.InvokeAsync(option.Value);
    }

    /// <summary>
    /// Clears the selected value
    /// </summary>
    private async Task Clear()
    {
        Selected = null;
        await OnSelected.InvokeAsync(null);
    }
    
    /// <summary>
    /// Disposes of the component
    /// </summary>
    public void Dispose()
    {
        if (dotnetObjRef != null)
        {
            _ = jsRuntime.InvokeVoidAsync("ff.removeClickHandler", Uid);
            dotnetObjRef.Dispose();
        }
    }
}

/// <summary>
/// An option in the drop down
/// </summary>
public class DropDownOption
{
    /// <summary>
    /// Gets or sets the icon to show
    /// </summary>
    public string Icon { get; set; }
    /// <summary>
    /// Gets or sets the label to show
    /// </summary>
    public string Label { get; set; }
    /// <summary>
    /// Gets or sets the value of this option
    /// </summary>
    public object Value { get; set; }
}