
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace FileFlows.Client.Components.Inputs;

/// <summary>
/// Represents a partial class for an input text area, inheriting from the generic Input class with a string type parameter.
/// </summary>
public partial class InputTextArea : Input<string>
{  
    /// <summary>
    /// Overrides the Focus method to focus on the element with the specified UID.
    /// </summary>
    /// <returns>Returns true if the focus operation is successful.</returns>
    public override bool Focus() => FocusUid();

    private int _Rows = 8;

    /// <summary>
    /// Gets or sets the number of rows to show in the text area
    /// </summary>
    [Parameter]
    public int Rows
    {
        get => _Rows < 1 ? 8 : _Rows;
        set => _Rows = value;
    }

    /// <summary>
    /// Overrides the ValueUpdated method to clear any error associated with the input value update.
    /// </summary>
    protected override void ValueUpdated()
    {
        ClearError();
    }
    
    /// <summary>
    /// Asynchronously handles the key down event for the text area input.
    /// </summary>
    /// <param name="e">The KeyboardEventArgs representing the key down event.</param>
    private async Task OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Code == "Enter" && e.ShiftKey) // for textarea the shortcut to submit is shift enter
            await OnSubmit.InvokeAsync();
        else if (e.Code == "Escape")
            await OnClose.InvokeAsync();
    }
}