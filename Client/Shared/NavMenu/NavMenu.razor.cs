using System.Threading;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace FileFlows.Client.Shared;

using System.Collections.Generic;
using System.Linq;
using FileFlows.Shared;
using Microsoft.AspNetCore.Components;

public partial class NavMenu : IDisposable
{
    [Inject] private INavigationService NavigationService { get; set; }
    [Inject] private NavigationManager NavigationManager { get; set; }
    /// <summary>
    /// Gets or sets teh client service
    /// </summary>
    [Inject] private ClientService ClientService { get; set; }
    [Inject] public IJSRuntime jSRuntime { get; set; }
    private List<NavMenuGroup> MenuItems = new List<NavMenuGroup>();
    private bool collapseNavMenu = true;

    public NavMenuItem Active { get; private set; }

    private string lblVersion, lblHelp, lblForum, lblDiscord;

    private string NavMenuCssClass => collapseNavMenu ? "collapse" : null;
    private NavMenuItem nmiFlows, nmiLibraries, nmiPause;

    private int Unprocessed = -1, Processing = -1, Failed = -1;
    /// <summary>
    /// Gets or sets the paused service
    /// </summary>
    [Inject] private IPausedService PausedService { get; set; }
    

    // private BackgroundTask bubblesTask;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        lblVersion = Translater.Instant("Labels.Version");
        lblHelp = Translater.Instant("Labels.Help");
        lblForum = Translater.Instant("Labels.Forum");
        lblDiscord = Translater.Instant("Labels.Discord");
        
        App.Instance.OnFileFlowsSystemUpdated += FileFlowsSystemUpdated;

        // bubblesTask = new BackgroundTask(TimeSpan.FromMilliseconds(10_000), () => _ = RefreshBubbles());
        _ = RefreshBubbles();
        // bubblesTask.Start();
        
        this.ClientService.FileStatusUpdated += ClientServiceOnFileStatusUpdated;
        PausedService.OnPausedLabelChanged += PausedServiceOnOnPausedLabelChanged;
        
        this.LoadMenu();
    }
    
    private void PausedServiceOnOnPausedLabelChanged(string label)
    {
        if (nmiPause == null || nmiPause.Title == label)
            return;
        
        nmiPause.Title = label;
        StateHasChanged();
    }

    private void ClientServiceOnFileStatusUpdated(List<LibraryStatus> data)
    {
        Unprocessed = data.Where(x => x.Status == FileStatus.Unprocessed).Select(x => x.Count).FirstOrDefault();
        Processing = data.Where(x => x.Status == FileStatus.Processing).Select(x => x.Count).FirstOrDefault();
        Failed = data.Where(x => x.Status == FileStatus.ProcessingFailed).Select(x => x.Count).FirstOrDefault();
        this.StateHasChanged();
    }

    private async Task RefreshBubbles()
    {
        bool hasFocus = await jSRuntime.InvokeAsync<bool>("eval", "document.hasFocus()");
        if (hasFocus == false)
            return;
        
        var sResult = await HttpHelper.Get<List<LibraryStatus>>("/api/library-file/status");
        if (sResult.Success == false || sResult.Data?.Any() != true)
            return;
        Unprocessed = sResult.Data.Where(x => x.Status == FileStatus.Unprocessed).Select(x => x.Count).FirstOrDefault();
        Processing = sResult.Data.Where(x => x.Status == FileStatus.Processing).Select(x => x.Count).FirstOrDefault();
        Failed = sResult.Data.Where(x => x.Status == FileStatus.ProcessingFailed).Select(x => x.Count).FirstOrDefault();
        this.StateHasChanged();
    }

    void LoadMenu()
    {
        this.MenuItems.Clear();
        nmiPause = new(PausedService.PausedLabel, "far fa-pause-circle", "#pause");

        MenuItems.Add(new NavMenuGroup
        {
            Name = Translater.Instant("MenuGroups.Overview"),
            Icon = "fas fa-info-circle",
            Items = new List<NavMenuItem>
            {
                new ("Pages.Dashboard.Title", "fas fa-chart-pie", ""),
                new ("Pages.LibraryFiles.Title", "fas fa-copy", "library-files"),
                //nmiPause
            }
        });

        nmiFlows = new("Pages.Flows.Title", "fas fa-sitemap", "flows");
        nmiLibraries = new("Pages.Libraries.Title", "fas fa-folder", "libraries");

        MenuItems.Add(new NavMenuGroup
        {
            Name = Translater.Instant("MenuGroups.Configuration"),
            Icon = "fas fa-code-branch",
            Items = new List<NavMenuItem>
            {
                nmiFlows,
                nmiLibraries,
                new ("Pages.Nodes.Title", "fas fa-desktop", "nodes")
            }
        });

        MenuItems.Add(new NavMenuGroup
        {
            Name = Translater.Instant("MenuGroups.Extensions"),
            Icon = "fas fa-laptop-house",
            Items = new List<NavMenuItem>
            {
                new("Pages.Plugins.Title", "fas fa-puzzle-piece", "plugins"),
                new("Pages.Scripts.Title", "fas fa-scroll", "scripts"),
                new("Pages.Variables.Title", "fas fa-at", "variables"),
            }
        });
        MenuItems.Add(new NavMenuGroup
        {
            Name = Translater.Instant("MenuGroups.System"),
            Icon = "fas fa-desktop",
            Items = new List<NavMenuItem>
            {
                App.Instance.FileFlowsSystem.LicenseRevisions ? new ("Pages.Revisions.Title", "fas fa-history", "revisions") : null,
                App.Instance.FileFlowsSystem.LicenseTasks ? new ("Pages.Tasks.Title", "fas fa-clock", "tasks") : null,
                App.Instance.FileFlowsSystem.LicenseWebhooks ? new ("Pages.Webhooks.Title", "fas fa-handshake", "webhooks") : null,
                new ("Pages.Settings.Title", "fas fa-cogs", "settings"),
            }
        });

        MenuItems.Add(new NavMenuGroup
        {
            Name = Translater.Instant("MenuGroups.Information"),
            Icon = "fas fa-question-circle",
            Items = new List<NavMenuItem>
            {
                new ("Pages.Log.Title", "fas fa-file-alt", "log")
            }
        });

        try
        {
            string currentRoute = NavigationManager.Uri[NavigationManager.BaseUri.Length..];
            Active = MenuItems.SelectMany(x => x.Items).FirstOrDefault(x => x.Url == currentRoute);
            if (Active == null)
            {
                if (NavigationManager.Uri.Contains("/flows"))
                {
                    // flow editor
                    Active = MenuItems.SelectMany(x => x.Items).FirstOrDefault(x => x.Url.Contains("flows"));
                }
                
                Active ??= MenuItems[0].Items.First();
            }
        }
        catch (Exception)
        {
        }
    }

    private void FileFlowsSystemUpdated(FileFlowsStatus system)
    {
        this.LoadMenu();
        this.StateHasChanged();
    }

    private string GetStepLabel(NavMenuItem nmi)
    {
        if (nmi == Active)
            return null;
        if ((App.Instance.FileFlowsSystem.ConfigurationStatus & ConfigurationStatus.Flows) !=
            ConfigurationStatus.Flows)
        {
            return nmi == nmiFlows ? "Step 1" : null;
        }

        if ((App.Instance.FileFlowsSystem.ConfigurationStatus & ConfigurationStatus.Libraries) !=
            ConfigurationStatus.Libraries)
        {
            return nmi == nmiLibraries ? "Step 2" : null; 
        }

        return null;
    }
    

    private void ToggleNavMenu()
    {
        collapseNavMenu = !collapseNavMenu;
    }

    async Task Click(NavMenuItem item)
    {
        if (item == nmiPause)
        {
            await PausedService.Toggle();
            return;
        }
        bool ok = await NavigationService.NavigateTo(item.Url);
        if (ok)
        {
            await jSRuntime.InvokeVoidAsync("eval", $"document.title = 'FileFlows'");
            SetActive(item);
            collapseNavMenu = true;
            this.StateHasChanged();
        }
    }

    private void SetActive(NavMenuItem item)
    {
        Active = item;
        this.StateHasChanged();
    }


    public void Dispose()
    {
        // _ = bubblesTask?.StopAsync();
        // bubblesTask = null;
    }
}

public class NavMenuGroup
{
    public string Name { get; set; }
    public string Icon { get; set; }
    public List<NavMenuItem> Items { get; set; } = new List<NavMenuItem>();
}

public class NavMenuItem
{
    public string Title { get; set; }
    public string Icon { get; set; }
    public string Url { get; set; }

    public NavMenuItem(string title = "", string icon = "", string url = "")
    {
        this.Title = Translater.TranslateIfNeeded(title);
        this.Icon = icon;
        this.Url = url;
    }
}