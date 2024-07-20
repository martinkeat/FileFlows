using Microsoft.AspNetCore.Components;
using System.Linq.Expressions;

namespace FileFlows.Client.Components.Common;

public partial class FlowSwitch: ComponentBase
{
    /// <summary>
    /// Gets or sets the value
    /// </summary>
    [Parameter]
    public bool Value { get; set; }

    /// <summary>
    /// Event called when the value changes
    /// </summary>
    [Parameter] 
    public EventCallback<bool> ValueChanged { get; set; }

    [Parameter]
    public Expression<Func<bool>> ValueExpression { get; set; }
    
    /// <summary>
    /// Gets or sets if this control is read-only
    /// </summary>
    [Parameter]
    public bool ReadOnly { get;set; }

    private void OnChange(ChangeEventArgs args)
    {
        this.Value = args.Value as bool? == true;
    }

    private void ToggleValue(EventArgs args)
    {
        if (ReadOnly)
            return;
        
        this.Value = !this.Value;
        if(ValueChanged.HasDelegate)
            ValueChanged.InvokeAsync(this.Value);
    }
}
