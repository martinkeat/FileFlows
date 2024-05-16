using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace FileFlows.Client.Components.Inputs;

/// <summary>
/// Input for a file size
/// </summary>
public partial class InputFileSize : Input<long>
{
    /// <inheritdoc />
    public override bool Focus() => FocusUid();

    /// <summary>
    /// Gets or sets the unit for the number
    /// </summary>
    private long Unit { get; set; }

    /// <summary>
    /// Gets or sets the number in the input field
    /// </summary>
    private long Number { get; set; }

    
    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();

        this.Unit = 1000;

        if (Value > 0)
        {
            foreach (long p in new [] { 1000000000, 1000000, 1000, 1})
            {
                if (Value % p == 0)
                {
                    // weeks
                    Number = Value / p;
                    Unit = p;
                    break;
                }
            }
        }
        else
        {
            Number = base.Field?.Name == "ProbeSize" ? 5 : 10;
            Unit = 1_000_000;
            Value = Number * Unit;
        }
    }

    /// <summary>
    /// When the number field changes
    /// </summary>
    /// <param name="e">the event</param>
    private void ChangeValue(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString() ?? "", out int value) == false)
            return;
        
        int max = Unit switch
        {
            1 => 1_000_000,
            1_000 => 100_000,
            1_000_000 => 10_000,
            _ => 1000
        };
        if (value > max)
            value = max;
        else if (value < 1)
            value = 1;
        this.Number = value;
        this.Value = this.Number * this.Unit;
        this.ClearError();
    }

    /// <summary>
    /// When a key is pressed in the number field
    /// </summary>
    /// <param name="e">the event</param>
    private async Task OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Code == "Enter")
            await OnSubmit.InvokeAsync();
        else if (e.Code == "Escape")
            await OnClose.InvokeAsync();
    }
    
    
    /// <summary>
    /// When the unit select value changes
    /// </summary>
    /// <param name="args">the event arguments</param>
    private void UnitSelectionChanged(ChangeEventArgs args)
    {
        if (int.TryParse(args?.Value?.ToString(), out int index))
        {
            Unit = index;
            Value = Number * Unit;
        }
        else
            Logger.Instance.DLog("Unable to find index of: ",  args?.Value);
    }
}