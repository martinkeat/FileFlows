using FileFlows.Client.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Pages;

/// <summary>
/// Initial configuration page
/// </summary>
public partial class InitialConfig : ComponentBase
{
    
    /// <summary>
    /// Gets or sets blocker instance
    /// </summary>
    [CascadingParameter] Blocker Blocker { get; set; }
    /// <summary>
    /// Gets or sets the javascript runtime used
    /// </summary>
    [Inject] IJSRuntime jsRuntime { get; set; }
    
    /// <summary>
    /// Gets or sets the navigation manager used
    /// </summary>
    [Inject] private NavigationManager NavigationManager { get; set; }
    
    /// <summary>
    /// Gets or sets the profile service
    /// </summary>
    [Inject] protected ProfileService ProfileService { get; set; }
    
    /// <summary>
    /// Gets the profile
    /// </summary>
    protected Profile Profile { get; private set; }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        Profile = await ProfileService.Get();
        if (Profile.IsAdmin == false)
        {
            await ProfileService.Logout("Labels.AdminRequired");
            return;
        }

        if ((Profile.ConfigurationStatus & ConfigurationStatus.InitialConfig) == ConfigurationStatus.InitialConfig &&
            (Profile.ConfigurationStatus & ConfigurationStatus.EulaAccepted) == ConfigurationStatus.EulaAccepted)
        {
            NavigationManager.NavigateTo("/");
            return;
        }
    }
}