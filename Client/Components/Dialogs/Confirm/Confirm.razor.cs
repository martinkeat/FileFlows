using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using FileFlows.Shared;
using Microsoft.JSInterop;

namespace FileFlows.Client.Components.Dialogs;

/// <summary>
/// Confirm dialog that prompts the user for confirmation 
/// </summary>
public partial class Confirm : ComponentBase, IDisposable
{
    [Inject] public IJSRuntime jsRuntime { get; set; }
    
    private string lblYes, lblNo;
    private string Message, Title, SwitchMessage;
    /// <summary>
    /// The default value of the button to focus when shown, true for yes, false for no
    /// </summary>
    private bool DefaultValue;
    TaskCompletionSource<bool> ShowTask;
    TaskCompletionSource<(bool, bool)> ShowSwitchTask;
    private bool ShowSwitch;
    private bool SwitchState;
    private bool RequireSwitch;

    private string btnYesUid, btnNoUid; 

    private static Confirm Instance { get; set; }

    private bool Visible { get; set; }
    private bool focused = false;

    protected override void OnInitialized()
    {
        this.lblYes = Translater.Instant("Labels.Yes");
        this.lblNo = Translater.Instant("Labels.No");
        App.Instance.OnEscapePushed += InstanceOnOnEscapePushed;
        Instance = this;
    }

    private void InstanceOnOnEscapePushed(OnEscapeArgs args)
    {
        if (Visible)
        {
            No();
            this.StateHasChanged();
        }
    }

    /// <summary>
    /// Shows a confirm message
    /// </summary>
    /// <param name="title">the title of the confirm message</param>
    /// <param name="message">the message of the confirm message</param>
    /// <param name="defaultValue">the default value to have highlighted, true for confirm, false for reject</param>
    /// <returns>the task to await for the confirm result</returns>
    public static Task<bool> Show(string title, string message, bool defaultValue = true)
    {
        if (Instance == null)
            return Task.FromResult(false);

        return Instance.ShowInstance(title, message, defaultValue);
    }
    
    /// <summary>
    /// Shows a confirm message
    /// </summary>
    /// <param name="title">the title of the confirm message</param>
    /// <param name="message">the message of the confirm message</param>
    /// <param name="switchMessage">message to show with an extra switch</param>
    /// <param name="switchState">the switch state</param>
    /// <param name="requireSwitch">if the switch is required to be checked for the YES button to become enabled</param>
    /// <returns>the task to await for the confirm result</returns>
    public static Task<(bool Confirmed, bool SwitchState)> Show(string title, string message, string switchMessage, bool switchState = false, bool requireSwitch = false)
    {
        if (Instance == null)
            return Task.FromResult((false, false));

        return Instance.ShowInstance(title, message, switchMessage, switchState, requireSwitch);
    }

    private Task<bool> ShowInstance(string title, string message, bool defaultValue)
    {
        Task.Run(async () =>
        {
            // wait a short delay this is in case a "Close" from an escape key is in the middle
            // of processing, and if we show this confirm too soon, it may automatically be closed
            await Task.Delay(5);
            this.btnYesUid = Guid.NewGuid().ToString();
            this.btnNoUid = Guid.NewGuid().ToString();
            this.DefaultValue = defaultValue;
            this.focused = false;
            this.Title = Translater.TranslateIfNeeded(title?.EmptyAsNull() ?? "Labels.Confirm");
            this.Message = Translater.TranslateIfNeeded(message ?? "");
            this.ShowSwitch = false;
            this.Visible = true;
            this.StateHasChanged();
        });

        Instance.ShowTask = new TaskCompletionSource<bool>();
        return Instance.ShowTask.Task;
    }
    private Task<(bool, bool)> ShowInstance(string title, string message, string switchMessage, bool switchState, bool requireSwitch, bool defaultValue = true)
    {
        this.btnYesUid = Guid.NewGuid().ToString();
        this.btnNoUid = Guid.NewGuid().ToString();
        this.focused = false;
        this.DefaultValue = defaultValue;
        this.Title = Translater.TranslateIfNeeded(title?.EmptyAsNull() ?? "Labels.Confirm");
        this.Message = Translater.TranslateIfNeeded(message ?? "");
        this.SwitchMessage = Translater.TranslateIfNeeded(switchMessage ?? "");
        this.ShowSwitch = true;
        this.RequireSwitch = requireSwitch;
        this.SwitchState = switchState;
        this.Visible = true;
        this.StateHasChanged();

        Instance.ShowSwitchTask  = new TaskCompletionSource<(bool, bool)>();
        return Instance.ShowSwitchTask.Task;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Visible && focused == false)
        {
            focused = true;
            await jsRuntime.InvokeVoidAsync("eval", $"document.getElementById('{(DefaultValue ? btnYesUid : btnNoUid )}').focus()");
        }
    }

    private async void Yes()
    {
        this.Visible = false;
        if (ShowSwitch)
        {
            Instance.ShowSwitchTask.TrySetResult((true, SwitchState));
            await Task.CompletedTask;
        }
        else
        {
            Instance.ShowTask.TrySetResult(true);
            await Task.CompletedTask;
        }
    }

    private async void No()
    {
        this.Visible = false;
        if (ShowSwitch)
        {
            Instance.ShowSwitchTask.TrySetResult((false, SwitchState));
            await Task.CompletedTask;
        }
        else
        {
            Instance.ShowTask.TrySetResult(false);
            await Task.CompletedTask;
        }
    }

    public void Dispose()
    {
        App.Instance.OnEscapePushed -= InstanceOnOnEscapePushed;
    }
}