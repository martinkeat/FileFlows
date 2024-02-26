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
    public override bool Focus() => FocusUid();
    private DotNetObjectReference<InputTime> jsRef;

    private string ValueString;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        ValueString = Value.ToString();
    }

    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            jsRef = DotNetObjectReference.Create(this);
            await jsRuntime.InvokeVoidAsync("createTimeSpanInput", this.Uid, jsRef); // Initialize the JS class with Blazor reference
        }
    }
    
    [JSInvokable]
    public async Task OnTimeSpanChange(string timeSpanString)
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