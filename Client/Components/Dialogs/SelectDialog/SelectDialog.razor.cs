using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using NPoco.Expressions;

namespace FileFlows.Client.Components.Dialogs;

/// <summary>
/// Dialog that prompts a user to select from a select dropdown
/// </summary>
public partial class SelectDialog: ComponentBase, IDisposable
{
    private string lblOk, lblCancel;
    private string Message, Title;
    /// <summary>
    /// The task used for when the dialog is closed
    /// </summary>
    object ShowTask;

    /// <summary>
    /// Gets or sets the singleton instance
    /// </summary>
    private static SelectDialog Instance { get; set; }

    /// <summary>
    /// Gets or sets if this is visible
    /// </summary>
    private bool Visible { get; set; }

    /// <summary>
    /// Gets or sets the index of the selected value
    /// </summary>
    public int SelectedIndex { get; set; }

    /// <summary>
    /// The unique identifier for this dialog
    /// </summary>
    private readonly string Uid = Guid.NewGuid().ToString();

    /// <summary>
    /// If this has focus
    /// </summary>
    private bool Focus;

    /// <summary>
    /// Gets or sets the Javascript runtime
    /// </summary>
    [Inject] private IJSRuntime jsRuntime { get; set; }
    
    /// <summary>
    /// Gets or sets the list options
    /// </summary>
    private List<ListOption> Options { get; set; }

    /// <summary>
    /// Initializes the component
    /// </summary>
    protected override void OnInitialized()
    {
        this.lblOk = Translater.Instant("Labels.Ok");
        this.lblCancel = Translater.Instant("Labels.Cancel");
        Instance = this;
        App.Instance.OnEscapePushed += InstanceOnOnEscapePushed;
    }

    /// <summary>
    /// Called when escape is pushed
    /// </summary>
    /// <param name="args">the args for the event</param>
    private void InstanceOnOnEscapePushed(OnEscapeArgs args)
    {
        if (Visible)
        {
            Cancel();
            this.StateHasChanged();
        }
    }

    /// <summary>
    /// Show a dialog
    /// </summary>
    /// <param name="title">the title of the dialog</param>
    /// <param name="message">the message of the dialog</param>
    /// <param name="options">the options to show in the list</param>
    /// <param name="value">the current value</param>
    /// <returns>an task to await for the dialog result</returns>
    public static Task<T> Show<T>(string title, string message, List<ListOption> options, T value)
    {
        if (Instance == null)
            return Task.FromResult<T>(default);

        return Instance.ShowInstance(title, message, options, value);
    }

    /// <summary>
    /// Show an instance of the dialog
    /// </summary>
    /// <param name="title">the title of the dialog</param>
    /// <param name="message">the message of the dialog</param>
    /// <param name="options">the options to show in the list</param>
    /// <param name="value">the current value</param>
    /// <returns>an task to await for the dialog result</returns>
    private Task<T> ShowInstance<T>(string title, string message, List<ListOption> options, T value)
    {
        this.Title = Translater.TranslateIfNeeded(title?.EmptyAsNull() ?? "Labels.Prompt");
        this.Message = Translater.TranslateIfNeeded(message ?? "");
        this.Options = options;
        this.SelectedIndex = Math.Max(0, options.FindIndex(x => x.Value == (object?)value));
        this.Visible = true;
        this.Focus = true;
        this.StateHasChanged();

        var task = new TaskCompletionSource<T>();
        Instance.ShowTask = task;
        return task.Task;
    }

    /// <summary>
    /// After the component was rendered
    /// </summary>
    /// <param name="firstRender">true if this is after the first render of the component</param>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Focus)
        {
            Focus = false;
            await jsRuntime.InvokeVoidAsync("eval", $"document.getElementById('{Uid}').focus()");
        }
    }

    /// <summary>
    /// Accept the dialog
    /// </summary>
    private async void Accept()
    {
        this.Visible = false;
        SetResult(this.Options[this.SelectedIndex].Value);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Sets the result
    /// </summary>
    /// <param name="value">the value to set</param>
    private void SetResult(object value)
    {
        object instance = Instance.ShowTask; // Assuming you have stored the instance in a variable
        Type taskCompletionSourceType = instance.GetType();
        var trySetResultMethod = taskCompletionSourceType.GetMethod("TrySetResult");
        if (trySetResultMethod != null)
            trySetResultMethod.Invoke(instance, new [] { value });
        
    }

    /// <summary>
    /// Cancel the dialog
    /// </summary>
    private async void Cancel()
    {
        this.Visible = false;
        SetResult(null);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Disposes of the cancel
    /// </summary>
    public void Dispose()
    {
        App.Instance.OnEscapePushed -= InstanceOnOnEscapePushed;
    }
    
    /// <summary>
    /// Updates the selected index
    /// </summary>
    /// <param name="e">the change event args</param>
    private void UpdateSelectedOption(ChangeEventArgs e)
         => this.SelectedIndex = Convert.ToInt32(e.Value);

}