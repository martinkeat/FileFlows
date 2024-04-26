
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace FileFlows.Client.Components.Inputs;

/// <summary>
/// Represents a partial class for an input text area, inheriting from the generic Input class with a string type parameter.
/// </summary>
public partial class InputTextArea : Input<string>
{  
    /// <summary>
    /// Gets or sets the JavaScript runtime
    /// </summary>
    [Inject] private IJSRuntime jSRuntime { get; set; }
    
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
    
    /// <summary>
    /// The javascript textarea object
    /// </summary>
    private IJSObjectReference jsTextArea;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        base.OnInitialized();
        var jsObjectReference = await jSRuntime.InvokeAsync<IJSObjectReference>("import", $"./Components/Inputs/InputTextArea/InputTextArea.razor.js?v={Globals.Version}");
        await jsObjectReference.InvokeVoidAsync("createInputTextArea", this.Uid, new Dictionary<string, object>
        {
            { "a.alfred", "alfred" },
            { "a.batman", "batman" },
            { "a.batgirl", "batgirl" },
            { "a.b.c", "ccccc" },
            { "a.b.d", "dddd" },
            { "b.alfred", "alfred" },
            { "b.batman", "batman" },
            { "b.batgirl", "batgirl" },
            { "b.b.c", "ccccc" },
            { "b.b.d", "dddd" },
            { "c.alfred", "alfred" },
            { "c.batman", "batman" },
            { "c.batgirl", "batgirl" },
            { "c.b.c", "ccccc" },
            { "c.b.d", "dddd" },
            { "library.Name", "some library" },
            { "library.UID", Guid.NewGuid() },
            { "MyVariable", "my variable value" },
        });
    }
}