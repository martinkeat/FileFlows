using FileFlows.Client.Components;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Pages;

/// <summary>
/// Login page
/// </summary>
public partial class Login
{
    /// <summary>
    /// the username
    /// </summary>
    private string username = string.Empty;
    /// <summary>
    /// the password
    /// </summary>
    private string password = string.Empty;

    /// <summary>
    /// Username or password
    /// </summary>
    private string usernameOrEmail = string.Empty;

    /// <summary>
    /// the mode 0 == login, 1 == forgot password
    /// </summary>
    private int mode = 0;
    
    /// <summary>
    /// Gets or sets the local storage
    /// </summary>
    [Inject] private FFLocalStorageService LocalStorage { get; set; }
    
    /// <summary>
    /// Gets or sets the navigation manager
    /// </summary>
    [Inject] private NavigationManager NavigationManager { get; set; }

    /// <summary>
    /// If a loading operating is in progress
    /// </summary>
    private bool loading = false;

    /// <inheritdoc />
    protected async override Task OnInitializedAsync()
    {
        await LocalStorage.SetAccessToken(null);
        HttpHelper.Client.DefaultRequestHeaders.Authorization = null;
        var passwordReset = NavigationManager.Uri.Contains("pr=1");
        if (passwordReset)
        {
            Toast.ShowSuccess("Password reset");
            NavigationManager.NavigateTo("/login");
        }
    }

    /// <summary>
    /// Submits the login
    /// </summary>
    async Task Submit()
    {
        if (loading)
            return;

        if (mode == 0)
            await DoLogin();
        else if (mode == 1)
            await ResetPassword();
    }

    /// <summary>
    /// Performs the password reset
    /// </summary>
    async Task ResetPassword()
    {
        if (string.IsNullOrWhiteSpace(usernameOrEmail))
            return;

        loading = true;
        var result = await HttpHelper.Post("/authorize/reset-password",
            new {
                usernameOrEmail
            });
        loading = false;
        if (result.Success == false)
        {
            Toast.ShowError(result.Body);
            StateHasChanged();
            return;
        }
        Toast.ShowSuccess("Password reset");
        SwitchMode(0);
    }

    /// <summary>
    /// Performs the login
    /// </summary>
    async Task DoLogin()
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return;

        loading = true;
        
        var result = await HttpHelper.Post("/authorize",
        new {
            username, password
        });
        if (result.Success == false)
        {
            Toast.ShowError(result.Body);
            loading = false;
            StateHasChanged();
            return;
        }

        await LocalStorage.SetAccessToken(result.Body);
        await App.Instance.Reinitialize(true);
        NavigationManager.NavigateTo("/");
    }

    /// <summary>
    /// Forgot password
    /// </summary>
    private void SwitchMode(int mode)
    {
        this.mode = mode;
        username = string.Empty;
        password = string.Empty;
        usernameOrEmail = string.Empty;
        StateHasChanged();
    }
}