using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using FileFlows.Shared;
using Microsoft.JSInterop;

namespace FileFlows.Client.Components.Dialogs;

/// <summary>
/// Message box popup with an OK button 
/// </summary>
public partial class MessageBox : ComponentBase, IDisposable
{
    /// <summary>
    /// Gets or sets th e javascrpt runtime
    /// </summary>
    [Inject] public IJSRuntime jsRuntime { get; set; }
    
    private string lblOk;
    private string Message, Title;
    TaskCompletionSource ShowTask;
    private string btnOkUid; 

    private static MessageBox Instance { get; set; }

    private bool Visible { get; set; }
    private bool focused = false;

    /// <summary>
    /// Initializes the component
    /// </summary>
    protected override void OnInitialized()
    {
        this.lblOk = Translater.Instant("Labels.Ok");
        App.Instance.OnEscapePushed += InstanceOnOnEscapePushed;
        Instance = this;
    }

    /// <summary>
    /// Closes the message box on escape
    /// </summary>
    /// <param name="args">the arguments from the event</param>
    private void InstanceOnOnEscapePushed(OnEscapeArgs args)
    {
        if (Visible)
        {
            Close();
            this.StateHasChanged();
        }
    }

    /// <summary>
    /// Shows a message
    /// </summary>
    /// <param name="title">the title of the message</param>
    /// <param name="message">the message of the message</param>
    /// <returns>the task to await for the message box to close</returns>
    public static Task Show(string title, string message)
    {
        if (Instance == null)
            return Task.FromResult(false);

        return Instance.ShowInstance(title, message);
    }

    /// <summary>
    /// Shows the instance of the message box
    /// </summary>
    /// <param name="title">the title of the message</param>
    /// <param name="message">the message of the message</param>
    /// <returns>the task to await for the message box to close</returns>
    private Task ShowInstance(string title, string message)
    {
        Task.Run(async () =>
        {
            // wait a short delay this is in case a "Close" from an escape key is in the middle
            // of processing, and if we show this confirm too soon, it may automatically be closed
            await Task.Delay(5);
            this.btnOkUid = Guid.NewGuid().ToString();
            this.focused = false;
            this.Title = Translater.TranslateIfNeeded(title?.EmptyAsNull() ?? "Labels.Message");
            this.Message = Translater.TranslateIfNeeded(message ?? "");
            this.Visible = true;
            this.StateHasChanged();
        });

        Instance.ShowTask = new TaskCompletionSource();
        return Instance.ShowTask.Task;
    }
    

    /// <summary>
    /// Called after rendering the message box
    /// </summary>
    /// <param name="firstRender">true if its the first render or not</param>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Visible && focused == false)
        {
            focused = true;
            await jsRuntime.InvokeVoidAsync("eval", $"document.getElementById('{this.btnOkUid}').focus()");
        }
    }

    /// <summary>
    /// Closes the message box
    /// </summary>
    private async void Close()
    {
        this.Visible = false;
        Instance.ShowTask.TrySetResult();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Disposes of this component
    /// </summary>
    public void Dispose()
    {
        App.Instance.OnEscapePushed -= InstanceOnOnEscapePushed;
    }
}