using System.Threading;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System.Collections.Generic;
using System.Linq;
using FileFlows.Client.Components.Dialogs;
using FileFlows.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace FileFlows.Client.Shared;

/// <summary>
/// Navigation Menu
/// </summary>
public partial class NavMenu : IDisposable
{
    /// <summary>
    /// Gets or sets the navigation service
    /// </summary>
    [Inject] private INavigationService NavigationService { get; set; }
    /// <summary>
    /// Gets or sets the navigation manager
    /// </summary>
    [Inject] private NavigationManager NavigationManager { get; set; }
    /// <summary>
    /// Gets or sets teh client service
    /// </summary>
    [Inject] private ClientService ClientService { get; set; }
    /// <summary>
    /// Gets or sets the JavaScript runtime
    /// </summary>
    [Inject] private IJSRuntime jSRuntime { get; set; }
    
    
    private List<NavMenuGroup> MenuItems = new List<NavMenuGroup>();
    private bool collapseNavMenu = true;
    
    /// <summary>
    /// Gets or sets the change password dialog
    /// </summary>
    private ChangePassword ChangePassword { get; set; }

    public NavMenuItem Active { get; private set; }

    private string lblVersion, lblHelp, lblForum, lblDiscord, lblChangePassword, lblLogout;

    private string NavMenuCssClass => collapseNavMenu ? "collapse" : null;
    private NavMenuItem nmiFlows, nmiLibraries, nmiPause;
    /// <summary>
    /// If the user menu is opened or closed
    /// </summary>
    private bool UserMenuOpened = false;

    private int Unprocessed = -1, Processing = -1, Failed = -1;
    /// <summary>
    /// Gets or sets the paused service
    /// </summary>
    [Inject] private IPausedService PausedService { get; set; }
    
    /// <summary>
    /// Gets or sets the profile service
    /// </summary>
    [Inject] private ProfileService ProfileService { get; set; }

    private List<NavMenuItem> UserMenu = new();
    private Profile Profile;

    // private BackgroundTask bubblesTask;

    /// <inheritdoc />
    protected async override Task OnInitializedAsync()
    {
        lblVersion = Translater.Instant("Labels.Version");
        lblHelp = Translater.Instant("Labels.Help");
        lblForum = Translater.Instant("Labels.Forum");
        lblDiscord = Translater.Instant("Labels.Discord");
        lblChangePassword = Translater.Instant("Labels.ChangePassword");
        lblLogout = Translater.Instant("Labels.Logout");
        
        NavigationManager.LocationChanged += NavigationManagerOnLocationChanged;
        
        // App.Instance.OnFileFlowsSystemUpdated += FileFlowsSystemUpdated;
        

        // bubblesTask = new BackgroundTask(TimeSpan.FromMilliseconds(10_000), () => _ = RefreshBubbles());
        _ = RefreshBubbles();
        // bubblesTask.Start();
        
        this.ClientService.FileStatusUpdated += ClientServiceOnFileStatusUpdated;
        PausedService.OnPausedLabelChanged += PausedServiceOnOnPausedLabelChanged;

        ProfileService.OnRefresh += ProfileServiceOnOnRefresh; 
        Profile = await ProfileService.Get();
        this.LoadMenu();
    }

    private void ProfileServiceOnOnRefresh()
    {
        this.LoadMenu();
        StateHasChanged();
    }

    private void NavigationManagerOnLocationChanged(object sender, LocationChangedEventArgs e)
    {
        var lastRoute = e.Location?.Split('/').LastOrDefault();
        if (string.IsNullOrWhiteSpace(lastRoute))
            return;
        var item = MenuItems.SelectMany(x => x.Items).FirstOrDefault(x => x.Url == lastRoute);
        if (item == null)
            return;
        
        Active = item;
        StateHasChanged();
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
        nmiPause = Profile.HasRole(UserRole.PauseProcessing) ? new(PausedService.PausedLabel, "far fa-pause-circle", "#pause") : null;

        MenuItems.Add(new NavMenuGroup
        {
            Name = Translater.Instant("MenuGroups.Overview"),
            Icon = "fas fa-info-circle",
            Items = new List<NavMenuItem>
            {
                new ("Pages.Dashboard.Title", "fas fa-chart-pie", ""),
                Profile.HasRole(UserRole.Files) ? new ("Pages.LibraryFiles.Title", "fas fa-copy", "library-files") : null,
                nmiPause
            }
        });

        nmiFlows = Profile.HasRole(UserRole.Flows) ? new("Pages.Flows.Title", "fas fa-sitemap", "flows") : null;
        nmiLibraries = Profile.HasRole(UserRole.Libraries) ? new("Pages.Libraries.Title", "fas fa-folder", "libraries") : null;

        MenuItems.Add(new NavMenuGroup
        {
            Name = Translater.Instant("MenuGroups.Configuration"),
            Icon = "fas fa-code-branch",
            Items = new List<NavMenuItem>
            {
                nmiFlows,
                nmiLibraries,
                Profile.HasRole(UserRole.Nodes) ? new ("Pages.Nodes.Title", "fas fa-desktop", "nodes") : null
            }
        });

        MenuItems.Add(new NavMenuGroup
        {
            Name = Translater.Instant("MenuGroups.Extensions"),
            Icon = "fas fa-laptop-house",
            Items = new List<NavMenuItem>
            {
                Profile.HasRole(UserRole.Plugins) ? new("Pages.Plugins.Title", "fas fa-puzzle-piece", "plugins") : null,
                Profile.HasRole(UserRole.Scripts) ? new("Pages.Scripts.Title", "fas fa-scroll", "scripts") : null,
                Profile.HasRole(UserRole.Variables) ? new("Pages.Variables.Title", "fas fa-at", "variables") : null,
            }
        });
        MenuItems.Add(new NavMenuGroup
        {
            Name = Translater.Instant("MenuGroups.System"),
            Icon = "fas fa-desktop",
            Items = new List<NavMenuItem>
            {
                Profile.HasRole(UserRole.Log) ? new ("Pages.Log.Title", "fas fa-file-alt", "log") : null,
                Profile.HasRole(UserRole.Revisions) && Profile.LicensedFor(LicenseFlags.Revisions) ? new ("Pages.Revisions.Title", "fas fa-history", "revisions") : null,
                Profile.HasRole(UserRole.Tasks) && Profile.LicensedFor(LicenseFlags.Tasks) ? new ("Pages.Tasks.Title", "fas fa-clock", "tasks") : null,
                Profile.HasRole(UserRole.Webhooks) && Profile.LicensedFor(LicenseFlags.Webhooks) ? new ("Pages.Webhooks.Title", "fas fa-handshake", "webhooks") : null,
            }
        });
        if(Profile.IsAdmin)
        {
            MenuItems.Add(new NavMenuGroup
            {
                Name = Translater.Instant("MenuGroups.Admin"),
                Icon = "fas fa-user-shield",
                Items = new List<NavMenuItem>
                {
                    new ("Pages.Settings.Title", "fas fa-cogs", "settings"),
                    Profile.LicensedFor(LicenseFlags.UserSecurity) ? new ("Pages.Users.Title", "fas fa-users", "users") : null
                }
            });
        }

        if (App.Instance.IsMobile)
        {
            MenuItems.Add(new NavMenuGroup
            {
                Name = Translater.Instant("MenuGroups.Information"),
                Icon = "fas fa-question-circle",
                Items = new List<NavMenuItem>
                {
                    new (lblHelp, "fas fa-question-circle", "https://fileflows.com/docs"), 
                    new (lblForum, "fas fa-comments", "https://fileflows.com/forum"),
                    new (lblDiscord, "fab fa-discord", "https://fileflows.com/discord")
                }
            });
        }

        if (App.Instance.IsMobile && Profile.Security != SecurityMode.Off)
        {
            MenuItems.Add(new NavMenuGroup
            {
                Name = Translater.Instant("MenuGroups.User"),
                Icon = "fas fa-user",
                Items = new List<NavMenuItem>
                {
                    Profile.Security == SecurityMode.Local ? new (lblChangePassword, "fas fa-key", "#change-password") : null,
                    new (lblLogout, "fas fa-unlock", "#logout"),
                }
            });
        }

        UserMenu.Clear();
        UserMenu.Add(new("fileflows.com", "fas fa-globe", "https://fileflows.com"));
        UserMenu.Add(new(lblHelp, "fas fa-question-circle", "https://fileflows.com/docs"));
        if(Profile.Security == SecurityMode.Local)
            UserMenu.Add(new (lblChangePassword, "fas fa-key", "#change-password"));
        if(Profile.Security != SecurityMode.Off)
            UserMenu.Add(new (lblLogout, "fas fa-unlock", "#logout"));

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
        if ((Profile.ConfigurationStatus & ConfigurationStatus.Flows) !=
            ConfigurationStatus.Flows)
        {
            return nmi == nmiFlows ? "Step 1" : null;
        }

        if ((Profile.ConfigurationStatus & ConfigurationStatus.Libraries) !=
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
        UserMenuOpened = false;
        if (item == nmiPause)
        {
            await PausedService.Toggle();
            return;
        }

        if (item.Url.StartsWith("http"))
        {
            await jSRuntime.InvokeVoidAsync("open", item.Url);
            return;
        }

        if (item.Url == "#change-password")
        {
            await ChangePassword.Show();
            return;
        }

        if (item.Url == "#logout")
        {
            await ProfileService.Logout();
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

    /// <summary>
    /// Toggles the visibility of the user menu
    /// </summary>
    private void ToggleUserMenu()
    {
        UserMenuOpened = !UserMenuOpened;
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
        this.Title = title == "fileflows.com" ? title : Translater.TranslateIfNeeded(title);
        this.Icon = icon;
        this.Url = url;
    }
}