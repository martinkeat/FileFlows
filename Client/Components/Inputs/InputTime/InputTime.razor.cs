using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Components.Inputs;

/// <summary>
/// Input for Time
/// </summary>
public partial class InputTime: Input<TimeSpan>
{
    /// <inheritdoc />
    public override bool Focus() => FocusUid();
    private DotNetObjectReference<InputTime> jsRef;

    /// <summary>
    /// The value as a string
    /// </summary>
    private string ValueString;

    /// <summary>
    /// If the component needs initializing
    /// </summary>
    private bool needsInitializing = true;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        ValueString = Value.ToString();
    }

    /// <inheritdoc />
    protected override void VisibleChanged(bool visible)
    {
        if (visible)
            needsInitializing = true;
    }
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (needsInitializing && Visible)
        {
            needsInitializing = false;
            jsRef = DotNetObjectReference.Create(this);
            await jsRuntime.InvokeVoidAsync("createTimeSpanInput", this.Uid, jsRef); // Initialize the JS class with Blazor reference
        }
    }
    
    [JSInvokable]
    public void OnTimeSpanChange(string timeSpanString)
    {
        if (TimeSpan.TryParse(timeSpanString, out TimeSpan newValue))
            Value = newValue;
        else
            Value = TimeSpan.Zero;
    }

    public override void Dispose()
    {
        base.Dispose();
        // Dispose the Blazor reference when the component is disposed
        jsRef?.Dispose(); 
    }
    
}