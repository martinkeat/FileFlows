using Microsoft.AspNetCore.Components;
using FileFlows.Client.Components;

namespace FileFlows.Client.Shared;

public partial class MainLayout : LayoutComponentBase
{
    public NavMenu Menu { get; set; }
    public Blocker Blocker { get; set; }
    public Blocker DisconnectedBlocker { get; set; }
    public Editor Editor { get; set; }
    [Inject] private ClientService ClientService { get; set; }
    [Inject] private FFLocalStorageService LocalStorage { get; set; }

    public static MainLayout Instance { get; private set; }

    private bool SearchVisible = false;

    public MainLayout()
    {
        Instance = this;
    }

    protected override async Task OnInitializedAsync()
    {
        App.Instance.NavMenuCollapsed = await LocalStorage.GetItemAsync<bool>("NavMenuCollapsed");
            
        this.ClientService.Connected += ClientServiceOnConnected;
        this.ClientService.Disconnected += ClientServiceOnDisconnected;
    }

    private void ClientServiceOnDisconnected()
    {
        DisconnectedBlocker.Show("Disconnected");
    }

    private void ClientServiceOnConnected()
    {
        DisconnectedBlocker.Hide();
    }

    private void ToggleExpand()
    {
        App.Instance.NavMenuCollapsed = !App.Instance.NavMenuCollapsed;
        LocalStorage.SetItemAsync("NavMenuCollapsed", App.Instance.NavMenuCollapsed);
    }

    public void ShowSearch()
    {
        SearchVisible = true;
        this.StateHasChanged();
    }

    public void HideSearch()
    {
        SearchVisible = false;
        this.StateHasChanged();
        
    }
}