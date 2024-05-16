using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Inputs;

/// <summary>
/// Input for a Number Percent type
/// </summary>
public partial class InputNumberPercent
{
    /// <summary>
    /// Gets or sets the non percent unit
    /// </summary>
    [Parameter]public string Unit { get; set; }
    
    private string lblPercent, lblNonPercent;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        lblPercent = "%"; //Translater.Instant("Labels.Percent");
        lblNonPercent = Translater.TranslateIfNeeded(Unit?.EmptyAsNull() ?? "Labels.Number");
        Value ??= new();
    }
    
    /// <summary>
    /// Gets or sets the percentage value
    /// </summary>
    private bool Percentage
    {
        get => Value.Percentage;
        set => Value = new()
        {
            Value = Number,
            Percentage = value
        };
    }

    /// <summary>
    /// Gets or sets the number value
    /// </summary>
    private int Number
    {
        get => Value.Value;
        set => Value = new()
        {
            Value = value,
            Percentage = Percentage
        };
    }


    /// <summary>
    /// Called when the select value changes
    /// </summary>
    /// <param name="e">the event arguments</param>
    private void SelectChange(ChangeEventArgs e)
    {
        Percentage = e.Value?.ToString()?.ToLowerInvariant() == "true";
    }
}