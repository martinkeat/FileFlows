using Microsoft.AspNetCore.Components.Web;

namespace FileFlows.Client.Components.Inputs;

/// <summary>
/// Input Text component
/// </summary>
public partial class InputText : Input<string>
{
    /// <inheritdoc />
    public override bool Focus() => FocusUid();

    /// <inheritdoc />
    protected override void ValueUpdated()
    {
        ClearError();
    }

    /// <summary>
    /// Called when a key is pressed in the input
    /// </summary>
    /// <param name="e">the event args</param>
    private async Task OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Code == "Enter")
            await OnSubmit.InvokeAsync();
        else if (e.Code == "Escape")
            await OnClose.InvokeAsync();
    }
}