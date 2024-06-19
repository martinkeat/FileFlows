using System.Text.RegularExpressions;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace FileFlows.Client.Components.Inputs;

/// <summary>
/// Input component for handling mathematical value inputs.
/// </summary>
public partial class InputMathValue : Input<string>
{
    /// <summary>
    /// Gets or sets the current operation selected.
    /// </summary>
    private string Operation { get; set; } = "=";

    /// <summary>
    /// Gets or sets the first text value.
    /// </summary>
    private string TextValue { get; set; } = "";

    /// <summary>
    /// Gets or sets the second text value (used in two-value operations).
    /// </summary>
    private string TextValue2 { get; set; } = "";

    /// <summary>
    /// List of available operations with their corresponding labels.
    /// </summary>
    private List<ListOption> Operations =
    [
        new() { Value = "=", Label = Translater.Instant("Enums.MathValue.Equals") },
        new() { Value = "!=", Label = Translater.Instant("Enums.MathValue.NotEquals") },
        new() { Value = ">", Label = Translater.Instant("Enums.MathValue.GreaterThan") },
        new() { Value = "<", Label = Translater.Instant("Enums.MathValue.LessThan") },
        new() { Value = ">=", Label = Translater.Instant("Enums.MathValue.GreaterThanOrEqual") },
        new() { Value = "<=", Label = Translater.Instant("Enums.MathValue.LessThanOrEqual") },
        new() { Value = "><", Label = Translater.Instant("Enums.MathValue.Between") },
        new() { Value = "<>", Label = Translater.Instant("Enums.MathValue.NotBetween") }
    ];

    /// <summary>
    /// Determines if the current operation is a two-value operation (e.g., "between" or "not between").
    /// </summary>
    private bool TwoValue => Operation is "><" or "<>";

    /// <summary>
    /// Label for And
    /// </summary>
    private string lblAnd;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        lblAnd = Translater.Instant("Labels.And");

        if (string.IsNullOrWhiteSpace(Value))
            return;
        
        Logger.Instance.ILog("MathValue Value: " + Value);
        
        if (Regex.IsMatch(Value, @"^\d+(\.\d+)?><\d+(\.\d+)?$"))
        {
            Operation = "><";
            var values = Value.Split(["><"], StringSplitOptions.None);
            TextValue = values[0];
            TextValue2 = values[1];
        }
        else if (Regex.IsMatch(Value, @"^\d+(\.\d+)?<>\d+(\.\d+)?$"))
        {
            Operation = "<>";
            var values = Value.Split(["<>"], StringSplitOptions.None);
            TextValue = values[0];
            TextValue2 = values[1];
        }
        else if (Regex.IsMatch(Value, @"^\d+(\.\d+)?$"))
        {
            // Upgrades if a field changed from a float to a MathValue
            Operation = "=";
            TextValue = Value;
            Value = "=" + Value;
        }
        else
        {
            foreach (var operation in Operations.OrderByDescending(x => x.Value.ToString().Length))
            {
                if (Value.StartsWith(operation.Value.ToString()) == false) 
                    continue;
                Operation = operation.Value.ToString();
                TextValue = Value[operation.Value.ToString().Length..];
                break;
            }
        }
        
    }

    /// <summary>
    /// Updates the value based on the current text input and operation selection.
    /// </summary>
    private void UpdateValue()
    {
        Value = TwoValue ? $"{TextValue}{Operation}{TextValue2}" : $"{Operation}{TextValue}";
    }

    /// <summary>
    /// Handles the change event of the first text input field.
    /// </summary>
    /// <param name="e">The change event arguments.</param>
    private void ChangeValue(ChangeEventArgs e)
    {
        ErrorMessage = null;
        TextValue = e.Value?.ToString() ?? "";
        UpdateValue();
        ClearError();
    }

    /// <summary>
    /// Handles the change event of the second text input field (only used in two-value operations).
    /// </summary>
    /// <param name="e">The change event arguments.</param>
    private void ChangeValue2(ChangeEventArgs e)
    {
        ErrorMessage = null;
        TextValue2 = e.Value?.ToString() ?? "";
        UpdateValue();
        ClearError();
    }

    /// <summary>
    /// Handles key down events in the input fields (e.g., Enter and Escape).
    /// </summary>
    /// <param name="e">The keyboard event arguments.</param>
    private async Task OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Code == "Enter")
            await OnSubmit.InvokeAsync();
        else if (e.Code == "Escape")
            await OnClose.InvokeAsync();
    }

    /// <summary>
    /// Handles the selection change event of the operation dropdown.
    /// </summary>
    /// <param name="args">The change event arguments.</param>
    private void OperationSelectionChanged(ChangeEventArgs args)
    {
        ErrorMessage = null;
        Operation = args?.Value?.ToString() ?? "=";
        UpdateValue();
    }

    public override Task<bool> Validate()
    {
        if (TwoValue)
        {
            if (string.IsNullOrWhiteSpace(TextValue2))
            {
                ErrorMessage = Translater.Instant("ErrorMessages.BothMathValuesRequired");
                return Task.FromResult(false);
            }

            if (string.IsNullOrWhiteSpace(TextValue))
            {
                ErrorMessage = Translater.Instant("ErrorMessages.BothMathValuesRequired");
                return Task.FromResult(false);
            }
        }
        return base.Validate();
    }
}