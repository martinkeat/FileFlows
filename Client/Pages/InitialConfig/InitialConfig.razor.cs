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

    private FlowTable<PluginPackageInfo> PluginTable;
    private string lblFlowElement, lblFlowElements;

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
        string html = Markdig.Markdown.ToHtml(EULA).Trim();
        msEula = new MarkupString(html);
        lblFlowElement = Translater.Instant("Labels.FlowElement");
        lblFlowElements = Translater.Instant("Labels.FlowElements");

        await GetPlugins();
    }

    private async Task GetPlugins()
    {
        var request = await HttpHelper.Get<List<PluginPackageInfo>>("/api/plugin/plugin-packages");
        if (request.Success)
            AvailablePlugins = request.Data;
    }
}