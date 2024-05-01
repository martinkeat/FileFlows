using FileFlows.Client.Components;
using FileFlows.Client.Components.Dialogs;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using FileFlows.Plugin;

namespace FileFlows.Client.Pages;

public partial class Dashboard : ComponentBase, IDisposable
{
    private ConfigurationStatus ConfiguredStatus = ConfigurationStatus.Flows | ConfigurationStatus.Libraries;
    [Inject] public IJSRuntime jSRuntime { get; set; }
    [Inject] private FFLocalStorageService LocalStorage { get; set; }
    [CascadingParameter] public Blocker Blocker { get; set; }
    [CascadingParameter] Editor Editor { get; set; }
    public EventHandler AddWidgetEvent { get; set; }
    
    /// <summary>
    /// Gets or sets the paused service
    /// </summary>
    [Inject] private IPausedService PausedService { get; set; }
    

    private string lblAddWidget;
    private bool IsPaused;
    
    private List<ListOption> Dashboards;

    private Guid? _ActiveDashboardUid = null;

    private bool ActiveDashboardSet => _ActiveDashboardUid != null;
    /// <summary>
    /// Gets or sets the client service
    /// </summary>
    [Inject] public ClientService ClientService { get; set; }
    /// <summary>
    /// Gets or sets the profile service
    /// </summary>
    [Inject] public ProfileService ProfileService { get; set; }

    private Profile Profile;

    /// <summary>
    /// Gets the UID of the active dashboard
    /// </summary>
    public Guid ActiveDashboardUid
    {
        get => _ActiveDashboardUid ?? Guid.Empty;
        private set
        {
            _ActiveDashboardUid = value;
            _ = LocalStorage.SetItemAsync("ACTIVE_DASHBOARD", value);
        }
    }

    protected override async Task OnInitializedAsync()
    {
        Profile = await ProfileService.Get();
        ConfiguredStatus = Profile.ConfigurationStatus;
        lblAddWidget = Translater.Instant("Pages.Dashboard.Labels.AddWidget");
        ClientService.SystemPausedUpdated += ClientServiceOnSystemPausedUpdated;
        PausedService.OnPausedLabelChanged += PausedServiceOnOnPausedLabelChanged;
        IsPaused = PausedService.IsPaused;

        await LoadDashboards();
    }

    private void PausedServiceOnOnPausedLabelChanged(string label)
    {
        if (PausedService.IsPaused == IsPaused)
            return;
        IsPaused = PausedService.IsPaused;
        StateHasChanged();
    }

    /// <summary>
    /// Called when the paused time is updated
    /// </summary>
    /// <param name="paused">if the system is paused</param>
    private void ClientServiceOnSystemPausedUpdated(bool paused)
        => StateHasChanged();

    private async Task LoadDashboards()
    {
        var dbResponse = await HttpHelper.Get<List<ListOption>>("/api/dashboard/list");
        if (dbResponse.Success == false || dbResponse.Data == null)
            return;
        this.Dashboards = dbResponse.Data;
        foreach (var db in this.Dashboards)
            db.Value = Guid.Parse(db.Value.ToString());
        SortDashboards();
        var lsActiveDashboard = await LocalStorage.GetItemAsync<Guid?>("ACTIVE_DASHBOARD");
        if (lsActiveDashboard != null &&  this.Dashboards.Any(x => x.Value != null && ((Guid)x.Value) == lsActiveDashboard))
        {
            this.ActiveDashboardUid = lsActiveDashboard.Value;
        }
        else
        {
            this.ActiveDashboardUid = (Guid)this.Dashboards[0].Value!;
        }
    }

    private void SortDashboards()
    {
        this.Dashboards = this.Dashboards.OrderBy(x =>
        {
            if ((Guid)x.Value! == FileFlows.Shared.Models.Dashboard.DefaultDashboardUid)
                return -2;
            if ((Guid)x.Value == Guid.Empty)
                return -1;
            return 0;
        }).ThenBy(x => x.Label).ToList();
    }



    private async Task AddDashboard()
    {
        string name = await Prompt.Show("New Dashboard", "Enter a name of the new dashboard");
        if (string.IsNullOrWhiteSpace(name))
            return; // was canceled
        this.Blocker.Show();
        try
        {
            var newDashboardResult = await HttpHelper.Put<FileFlows.Shared.Models.Dashboard>("/api/dashboard", new { Name = name });
            if (newDashboardResult.Success == false)
            {
                var error = newDashboardResult.Body?.EmptyAsNull() ?? "Pages.Dashboard.ErrorMessages.FailedToCreate";
                Toast.ShowError(error);
                return;
            }

            this.Dashboards ??= new ();
            this.Dashboards.Add(new ()
            {
                Label = newDashboardResult.Data.Name,
                Value = newDashboardResult.Data.Uid
            });
            SortDashboards();
            this.ActiveDashboardUid = newDashboardResult.Data.Uid;
        }
        finally
        {
            this.Blocker.Hide();
        }
    }

    private async Task DeleteDashboard()
    {
        if (DashboardDeletable == false)
            return;
        bool confirmed = await Confirm.Show("Labels.Delete", "Pages.Dashboard.Messages.DeleteDashboard");
        if (confirmed == false)
            return;
        Blocker.Show();
        try
        {
            await HttpHelper.Delete("/api/dashboard/" + ActiveDashboardUid);
            this.Dashboards.RemoveAll(x => x.Value != null && (Guid)x.Value == ActiveDashboardUid);
            this.ActiveDashboardUid = (Guid)this.Dashboards[0].Value!;
        }
        finally
        {
            Blocker.Hide();
        }
    }

    private bool DashboardDeletable => ActiveDashboardUid != Guid.Empty &&
                                      ActiveDashboardUid != FileFlows.Shared.Models.Dashboard.DefaultDashboardUid;

    private void AddWidget() => AddWidgetEvent?.Invoke(this, new EventArgs());

    /// <summary>
    /// Disposes of the component
    /// </summary>
    public void Dispose()
    {
        Editor?.Dispose();
        ClientService.SystemPausedUpdated -= ClientServiceOnSystemPausedUpdated;
        PausedService.OnPausedLabelChanged -= PausedServiceOnOnPausedLabelChanged;
    }
}