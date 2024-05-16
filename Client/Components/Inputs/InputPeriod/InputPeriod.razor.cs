using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace FileFlows.Client.Components.Inputs;

/// <summary>
/// Input for a period, time in minutes
/// </summary>
public partial class InputPeriod : Input<int>
{
    /// <inheritdoc />
    public override bool Focus() => FocusUid();

    /// <summary>
    /// Gets or sets the selected period
    /// </summary>
    private int Period { get; set; }

    /// <summary>
    /// Gets or sets the number value
    /// </summary>
    private int Number { get; set; }

    /// <summary>
    /// Gets or sets if the weeks is shown
    /// </summary>
    [Parameter]
    public bool ShowWeeks { get; set; } = true;
    
    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();

        this.Period = 1440;

        if (Value > 0)
        {
            var ranges = ShowWeeks ? new[] { 10080, 1440, 60, 1 } : new[] { 1440, 60, 1 };
            foreach (int p in ranges)
            {
                if (Value % p == 0)
                {
                    // weeks
                    Number = Value / p;
                    Period = p;
                    break;
                }
            }
        }
        else
        {
            Number = 3;
            Period = 1440;
            Value = Number * Period;
        }
    }
    

    /// <summary>
    /// Changes the value
    /// </summary>
    /// <param name="e">the change event</param>
    private void ChangeValue(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString() ?? "", out int value) == false)
            return;
        
        int max = Period switch
        {
            1 => 100_000,
            60 => 10_000,
            1440 => 10_000,
            _ => 1_000
        };
        if (value > max)
            value = max;
        else if (value < 1)
            value = 1;
        this.Number = value;
        this.Value = this.Number * this.Period;
        this.ClearError();
    }

    /// <summary>
    /// Event called when a key is pressed
    /// </summary>
    /// <param name="e">the keyboard event</param>
    private async Task OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Code == "Enter")
            await OnSubmit.InvokeAsync();
        else if (e.Code == "Escape")
            await OnClose.InvokeAsync();
    }
    
    /// <summary>
    /// Event when the period changed
    /// </summary>
    /// <param name="args">the change event</param>
    private void PeriodSelectionChanged(ChangeEventArgs args)
    {
        if (int.TryParse(args?.Value?.ToString(), out int index))
        {
            Period = index;
            Value = Number * Period;
        }
        else
            Logger.Instance.DLog("Unable to find index of: ",  args?.Value);
    }

}