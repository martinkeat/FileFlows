using FileFlows.Client.Components;
using FileFlows.Client.Components.Common;
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
    private Profile Profile { get; set; }

    /// <summary>
    /// The markup string of the EULA
    /// </summary>
    private MarkupString msEula;
    
    /// <summary>
    /// Gets or sets if the EULA has been accepted
    /// </summary>
    private bool EulaAccepted { get; set; }
    
    /// <summary>
    /// Gets or sets a list of available plugins
    /// </summary>
    private List<PluginPackageInfo> AvailablePlugins { get; set; }

    /// <summary>
    /// The plugins that are forced checked and cannot be unchecked
    /// These are plugins that are already installed
    /// </summary>
    private List<PluginPackageInfo> ForcedPlugins;

    /// <summary>
    /// The Plugin flowtable
    /// </summary>
    private FlowTable<PluginPackageInfo> PluginTable;
    /// <summary>
    /// Label for "Installed" shown next to installed plugins
    /// </summary>
    private string lblInstalled;
    /// <summary>
    /// If this component is fully loaded or not.
    /// Is false until the plugins have been loaded which may take a second or two
    /// </summary>
    private bool loaded;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        Blocker.Show("Labels.Loading");
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
        string html = Markdig.Markdown.ToHtml(EULA).Trim();
        msEula = new MarkupString(html);
        lblInstalled = Translater.Instant("Labels.Installed");

        // only show plugins if they haven't configured the system yet
        if((Profile.ConfigurationStatus & ConfigurationStatus.InitialConfig) != ConfigurationStatus.InitialConfig)
            await GetPlugins();
        
        Blocker.Hide();
        loaded = true;
    }

    /// <summary>
    /// Gets the plugins from the backend
    /// </summary>
    private async Task GetPlugins()
    {
        var request = await HttpHelper.Get<List<PluginPackageInfo>>("/api/plugin/plugin-packages");
        if (request.Success == false)
            return;

        AvailablePlugins = request.Data.OrderBy(x => x.Installed ? 0 : 1)
            .ThenBy(x => x.Name.ToLowerInvariant()).ToList();
        ForcedPlugins = AvailablePlugins.Where(x => x.Installed).ToList();
    }

    /// <summary>
    /// Savss the initial configuration
    /// </summary>
    private async Task Save()
    {
        if (EulaAccepted == false)
        {
            Toast.ShowError("Accept the EULA to continue.");
            return;
        }

        var plugins = PluginTable.GetSelected().ToList();
        
        Blocker.Show("Labels.Saving");
        try
        {
            var result = await HttpHelper.Post("/api/settings/initial-config", new
            {
                EulaAccepted,
                Plugins = plugins
            });
            if (result.Success)
            {
                await ProfileService.Refresh();
                if ((Profile.ConfigurationStatus & ConfigurationStatus.Flows) != ConfigurationStatus.Flows)
                    NavigationManager.NavigateTo("/flows/00000000-0000-0000-0000-000000000000", Profile.IsWebView);
                else if((Profile.ConfigurationStatus & ConfigurationStatus.Libraries) != ConfigurationStatus.Libraries)
                    NavigationManager.NavigateTo("/libraries");
                else
                    NavigationManager.NavigateTo("/");
                return;
            }
        }
        catch (Exception)
        {
            // ignored
        }

        Toast.ShowError("Failed to save initial configuration.");
        Blocker.Hide();
    }
}