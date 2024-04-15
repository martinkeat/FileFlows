using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Inputs;

/// <summary>
/// Input for text that allows for variables
/// </summary>
public partial class InputTextVariables : Input<string>
{
    /// <summary>
    /// The Preview text to show for the rendered variable value
    /// </summary>
    private string Preview = string.Empty;
    
    /// <summary>
    /// The variables dictionary
    /// </summary>
    private Dictionary<string, object> _Variables = new ();

    /// <summary>
    /// Gets or sets the variables available
    /// </summary>
    [Parameter]
    public Dictionary<string, object> Variables
    {
        get => _Variables;
        set { _Variables = value ?? new Dictionary<string, object>(); }
    }

    /// <summary>
    /// Gets or stes the value
    /// </summary>
    public new string Value
    {
        get => base.Value;
        set
        {
            if (base.Value == value)
                return;

            base.Value = value ?? string.Empty;
            UpdatePreview();
        }
    }

    
    /// <inheritdoc />
    public override bool Focus() => FocusUid();
        
    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();       
        UpdatePreview();
    }   

    /// <summary>
    /// Updates the preview text
    /// </summary>
    private void UpdatePreview()
    {
        string preview = Plugin.VariablesHelper.ReplaceVariables(this.Value, Variables, false);
        this.Preview = preview;             
    }

    /// <summary>
    /// When the user submits in this field
    /// </summary>
    private void VariableOnSubmit()
    {
        _ = base.OnSubmit.InvokeAsync();
    }
}