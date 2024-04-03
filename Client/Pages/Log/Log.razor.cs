using BlazorDateRangePicker;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;
using FileFlows.Client.Components;
using System.Timers;
using FileFlows.Client.Helpers;
using Microsoft.JSInterop;

namespace FileFlows.Client.Pages;


public partial class Log : ComponentBase
{
    [CascadingParameter] Blocker Blocker { get; set; }
    [Inject] protected IJSRuntime jsRuntime { get; set; }
    [Inject] NavigationManager NavigationManager { get; set; }
    private string LogText { get; set; }
    private string lblDownload, lblSearch, lblSearching;
    private string DownloadUrl;
    private bool scrollToBottom = false;

    private bool Searching = false;

    SearchPane SearchPane { get; set; }

    
    private Timer AutoRefreshTimer;

    private LogType LogLevel { get; set; } = LogType.Info;

    private List<ListOption> LoggingSources = new ();
    /// <summary>
    /// Gets or sets the profile service
    /// </summary>
    [Inject] public ProfileService ProfileService { get; set; }
    /// <summary>
    /// The users profile
    /// </summary>
    private Profile Profile;

    private readonly LogSearchModel SearchModel = new()
    {
        Message = string.Empty,
        Source = string.Empty,
        Type = LogType.Info,
        TypeIncludeHigherSeverity = true
    };

    protected override async Task OnInitializedAsync()
    {
        Profile = await ProfileService.Get();
        
        SearchModel.FromDate = DateRangeHelper.LiveStart;
        SearchModel.ToDate = DateRangeHelper.LiveEnd;


        LoggingSources = (await HttpHelper.Get<List<ListOption>>("/api/fileflows-log/log-sources")).Data;

        this.lblSearch = Translater.Instant("Labels.Search");
        this.lblSearching = Translater.Instant("Labels.Searching");
        this.lblDownload = Translater.Instant("Labels.Download");
#if (DEBUG)
        this.DownloadUrl = "http://localhost:6868/api/fileflows-log/download";
#else
        this.DownloadUrl = "/api/fileflows-log/download";
#endif
        NavigationManager.LocationChanged += NavigationManager_LocationChanged;
        AutoRefreshTimer = new Timer();
        AutoRefreshTimer.Elapsed += AutoRefreshTimerElapsed;
        AutoRefreshTimer.Interval = 5_000;
        AutoRefreshTimer.AutoReset = true;
        AutoRefreshTimer.Start();
        _ = Refresh();
    }
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(100); // 100ms
                await jsRuntime.InvokeVoidAsync("ff.scrollToBottom", new object[]{ ".page .content", true});
                await Task.Delay(400); // 500ms
                await jsRuntime.InvokeVoidAsync("ff.scrollToBottom", new object[]{ ".page .content", true});
                await Task.Delay(200); // 700ms
                await jsRuntime.InvokeVoidAsync("ff.scrollToBottom", new object[]{ ".page .content", true});
                await Task.Delay(300); // 1second
                await jsRuntime.InvokeVoidAsync("ff.scrollToBottom", new object[]{ ".page .content", true});
            });
        }
        if (scrollToBottom)
        {
            await jsRuntime.InvokeVoidAsync("ff.scrollToBottom", new object[]{ ".page .content"});
            scrollToBottom = false;
        }
    }

    private void NavigationManager_LocationChanged(object sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
    {
        Dispose();
    }

    public void Dispose()
    {
        if (AutoRefreshTimer != null)
        {
            AutoRefreshTimer.Stop();
            AutoRefreshTimer.Elapsed -= AutoRefreshTimerElapsed;
            AutoRefreshTimer.Dispose();
            AutoRefreshTimer = null;
        }
    }
    void AutoRefreshTimerElapsed(object sender, ElapsedEventArgs e)
    {
        if (Searching)
            return;
        
        if (Profile.LicensedFor(LicenseFlags.ExternalDatabase))
        {
            if (SearchModel.ToDate != DateRangeHelper.LiveEnd || SearchModel.FromDate != DateRangeHelper.LiveStart)
                return;
        }
        
        _ = Refresh();
    }

    async Task Search()
    {
        this.Searching = true;
        try
        {
            Blocker.Show(lblSearching);
            await Refresh();
            Blocker.Hide();
        }
        finally
        {
            this.Searching = false;
        }
    }

    async Task Refresh()
    {
        bool nearBottom = string.IsNullOrWhiteSpace(LogText) == false && await jsRuntime.InvokeAsync<bool>("ff.nearBottom", new object[]{ ".page .content"});
        if (Profile.LicensedFor(LicenseFlags.ExternalDatabase))
        {
            var response = await HttpHelper.Post<string>("/api/fileflows-log/search", SearchModel);
            if (response.Success)
            {
                this.LogText = response.Data;
                this.scrollToBottom = nearBottom;
                this.StateHasChanged();
            }
        }
        else
        {
            var response = await HttpHelper.Get<string>("/api/fileflows-log?logLevel=" + LogLevel);
            if (response.Success)
            {
                this.LogText = response.Data;
                this.scrollToBottom = nearBottom;
                this.StateHasChanged();
            }
        }
    }

    async Task ChangeLogType(ChangeEventArgs args)
    {
        this.LogLevel = (LogType)int.Parse(args.Value.ToString());
        await Refresh();
    }

    
    public void OnRangeSelect(DateRange range)
    {
        SearchModel.FromDate = range.Start.Date;
        SearchModel.ToDate = range.End.Date;
    }
}
