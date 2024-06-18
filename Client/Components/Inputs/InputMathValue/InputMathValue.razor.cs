using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace FileFlows.Client.Components.Inputs;

/// <summary>
/// Input for a Math Value
/// </summary>
public partial class InputMathValue : Input<string>
{
    /// <inheritdoc />
    public override bool Focus() => FocusUid();

    /// <summary>
    /// Gets or sets the matho operation
    /// </summary>
    private string Operation { get; set; }

    /// <summary>
    /// Gets or sets the text value
    /// </summary>
    private string TextValue { get; set; }

    private List<string> Operations =  [ "=", "<=", ">=", "!=", "<", ">" ];
    
    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();

        this.Operation = "=";
        this.TextValue = "";

        if (string.IsNullOrWhiteSpace(this.Value))
        {
            foreach (var operation in Operations)
            {
                if (Value.StartsWith(operation))
                {
                    Operation = operation;
                    TextValue = Value[operation.Length..];
                }
            }
        }
    }

    /// <summary>
    /// When the text field changes
    /// </summary>
    /// <param name="e">the event</param>
    private void ChangeValue(ChangeEventArgs e)
    {
        this.TextValue = e.Value?.ToString() ?? string.Empty;
        
        this.Value = this.Operation + this.TextValue;
        this.ClearError();
    }

    /// <summary>
    /// When a key is pressed in the text field
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
    /// When the opration select value changes
    /// </summary>
    /// <param name="args">the event arguments</param>
    private void OperationSelectionChanged(ChangeEventArgs args)
    {
        this.Operation = args?.Value?.ToString();
        this.Value = this.Operation + this.TextValue;
    }
}