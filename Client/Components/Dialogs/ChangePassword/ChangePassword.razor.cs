using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace FileFlows.Client.Components.Dialogs;

/// <summary>
/// Change password dialog
/// </summary>
public partial class ChangePassword: ComponentBase, IDisposable
{
    /// <summary>
    /// Gets or sets the javascript runtime
    /// </summary>
    [Inject] public IJSRuntime jsRuntime { get; set; }
    
    private string lblTitle, lblSave, lblCancel, lblOldPassword, lblNewPassword, lblNewPasswordConfirm;
    TaskCompletionSource ShowTask;

    private string txtOldPasswordUid;
    private string oldPassword, newPassword, newPasswordConfirm;

    /// <summary>
    /// Saving the password
    /// </summary>
    private bool saving = false;

    /// <summary>
    /// Gets or sets if the component is visible
    /// </summary>
    private bool Visible { get; set; }
    /// <summary>
    /// if the input requires a focus event
    /// </summary>
    private bool focused = false;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        lblSave = Translater.Instant("Labels.Save");
        lblCancel = Translater.Instant("Labels.Cancel");
        lblTitle = Translater.Instant("Dialogs.ChangePassword.Title");
        lblOldPassword = Translater.Instant("Dialogs.ChangePassword.OldPassword");
        lblNewPassword = Translater.Instant("Dialogs.ChangePassword.NewPassword"); 
        lblNewPasswordConfirm = Translater.Instant("Dialogs.ChangePassword.NewPasswordConfirm");
        App.Instance.OnEscapePushed += OnEscapePushed;
    }

    /// <summary>
    /// Event fired when escaped key is pushed
    /// </summary>
    /// <param name="args">the arguments</param>
    private void OnEscapePushed(OnEscapeArgs args)
    {
        if (Visible)
        {
            Cancel();
            StateHasChanged();
        }
    }
    
    /// <summary>
    /// Shows the password change dialog
    /// </summary>
    /// <returns>the task to await</returns>
    public Task Show()
    {
        oldPassword = string.Empty;
        newPassword = string.Empty;
        newPasswordConfirm = string.Empty;
        Task.Run(async () =>
        {
            // wait a short delay this is in case a "Close" from an escape key is in the middle
            // of processing, and if we show this confirm too soon, it may automatically be closed
            await Task.Delay(5);
            txtOldPasswordUid = Guid.NewGuid().ToString();
            focused = false;
            Visible = true;
            StateHasChanged();
        });

        ShowTask = new TaskCompletionSource();
        return ShowTask.Task;
    }

    /// <summary>
    /// After the component is rendered
    /// </summary>
    /// <param name="firstRender">the first render</param>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Visible && focused == false)
        {
            focused = true;
            await jsRuntime.InvokeVoidAsync("eval", $"document.getElementById('{txtOldPasswordUid}').focus()");
        }
    }

    /// <summary>
    /// Saves the password change
    /// </summary>
    private async void Save()
    {
        if(string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(newPasswordConfirm))
            return;

        if (newPassword != newPasswordConfirm)
        {
            Toast.ShowError("Dialogs.ChangePassword.PasswordMismatch");
            return;
        }

        saving = true;
        var result = await HttpHelper.Post("/authorize/change-password", new
        {
            oldPassword, newPassword
        });
        saving = false;
        if (result.Success == false)
        {
            Toast.ShowError(result.Body);
        }
        else
        {
            Toast.ShowSuccess("Dialogs.ChangePassword.Changed");
            Visible = false;
            ShowTask.TrySetResult();
        }
        StateHasChanged();
    }

    /// <summary>
    /// Cancels the password change
    /// </summary>
    private async void Cancel()
    {
        this.Visible = false;
        ShowTask.TrySetResult();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Disposes the component
    /// </summary>
    public void Dispose()
    {
        App.Instance.OnEscapePushed -= OnEscapePushed;
    }
    
    /// <summary>
    /// On key down
    /// </summary>
    /// <param name="args">the keyboard event args</param>
    private void OnKeyDown(KeyboardEventArgs args)
    {
        if(args.Key == "Enter")
        {
            Save();
        }
    }
}