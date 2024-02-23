using System.Runtime.InteropServices.JavaScript;
using FileFlows.Client.Components.Dialogs;
using FileFlows.Client.Helpers;
using FileFlows.Plugin;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Components.Dashboard;

/// <summary>
/// Custom dashboard allows the user to rearrange and customize the dashboard
/// </summary>
public partial class CustomDashboard : IDisposable
{
    const string ApiUrl = "/api/worker";
    [Inject] public IJSRuntime jSRuntime { get; set; }
    [CascadingParameter] public Blocker Blocker { get; set; }
    [CascadingParameter] Editor Editor { get; set; }
    [CascadingParameter] private Pages.Dashboard Dashboard { get; set; }
    /// <summary>
    /// Gets or sets the client service
    /// </summary>
    [Inject]public ClientService ClientService { get; set; }

    /// <summary>
    /// Register callbacks in javascript for events
    /// </summary>
    private readonly Dictionary<string, Action<object>> JsCallbacks = new();

    private Guid _ActiveDashboardUid;
    private bool needsLoading = false;

    [Parameter]
    public Guid ActiveDashboardUid
    {
        get => _ActiveDashboardUid;
        set
        {
            if (_ActiveDashboardUid == value)
                return;
            if (jsCharts == null)
            {
                needsLoading = true;
                _ActiveDashboardUid = value;
                return;
            }
            _ = Task.Run(async () =>
            {
                var dotNetObjRef = DotNetObjectReference.Create(this);
                await jsCharts.InvokeVoidAsync($"destroyDashboard",  this.Widgets, dotNetObjRef); 
                _ActiveDashboardUid = value;
                await this.LoadDashboard();
            });
        }
    }

    /// <summary>
    /// Gets if the active dashboard is the default dashboard
    /// </summary>
    private bool IsDefaultDashboard => ActiveDashboardUid == FileFlows.Shared.Models.Dashboard.DefaultDashboardUid;

    public IJSObjectReference jsFunctions;

    private readonly List<WidgetUiModel> Widgets = new List<WidgetUiModel>();
    private IJSObjectReference jsCharts;
    
    protected override async Task OnInitializedAsync()
    {
        jsCharts = await jSRuntime.InvokeAsync<IJSObjectReference>("import", $"./scripts/Charts/FFCharts.js");
        Dashboard.AddWidgetEvent = (sender, args) => _ = AddWidgetDialog();

        if (needsLoading)
        {
            needsLoading = false;
            
            var dotNetObjRef = DotNetObjectReference.Create(this);
            await jsCharts.InvokeVoidAsync($"destroyDashboard",  this.Widgets, dotNetObjRef);
            await this.LoadDashboard();
        }
        
        ClientService.ExecutorsUpdated += ClientServiceOnExecutorsUpdated;
    }

    /// <summary>
    /// Called when the client service executors get an update
    /// </summary>
    /// <param name="data">the update data</param>
    private void ClientServiceOnExecutorsUpdated(List<FlowExecutorInfoMinified> data)
    {
        if(JsCallbacks.TryGetValue("FlowExecutorInfo", out Action<object> callback))
            callback.Invoke(data);
    }

    private async Task LoadDashboard()
    {
        var WidgetsResponse = await HttpHelper.Get<List<WidgetUiModel>>("/api/dashboard/" + ActiveDashboardUid + "/Widgets");
        if (WidgetsResponse.Success == false)
            return;
        this.Widgets.Clear();
        if(WidgetsResponse.Data?.Any() == true)
            this.Widgets.AddRange(WidgetsResponse.Data);
        var dotNetObjRef = DotNetObjectReference.Create(this);
        await jsCharts.InvokeVoidAsync($"initDashboard",  this.ActiveDashboardUid, this.Widgets, dotNetObjRef, IsDefaultDashboard); 
    }
    
    public void Dispose()
    {
        _ = jsCharts?.InvokeVoidAsync("disposeAll");
        ClientService.ExecutorsUpdated -= ClientServiceOnExecutorsUpdated;
    }
    
    
    /// <summary>
    /// Opens the full library file editor for a completed library file
    /// </summary>
    /// <param name="libraryFileUid">The UID of the library file</param>
    [JSInvokable]
    public async Task OpenFileViewer(Guid libraryFileUid)
    {
        await Helpers.LibraryFileEditor.Open(Blocker, Editor, libraryFileUid);
    }

    /// <summary>
    /// Registers a event callback
    /// </summary>
    /// <param name="eventName">the event name</param>
    /// <param name="callback">the callback</param>
    [JSInvokable]
    public void RegisterCallback(string eventName, Action<object> callback)
    {
        if (string.IsNullOrWhiteSpace(eventName) || callback == null)
            return;
        JsCallbacks[eventName] = callback;
    }
    
    /// <summary>
    /// Opens a log for a executing library file
    /// </summary>
    /// <param name="libraryFileUid">The UID of the library file</param>
    /// <param name="libraryFileName">the filename of the library file</param>
    [JSInvokable]
    public async Task OpenLog(Guid libraryFileUid, string libraryFileName)
    {
        Blocker.Show();
        string log = string.Empty;
        string url = $"{ApiUrl}/{libraryFileUid}/log?lineCount=200";
        try
        {
            var logResult = await GetLog(url);
            if (logResult.Success == false || string.IsNullOrEmpty(logResult.Data))
            {
                Toast.ShowError( Translater.Instant("Pages.Dashboard.ErrorMessages.LogFailed"));
                return;
            }
            log = logResult.Data;
        }
        finally
        {
            Blocker.Hide();
        }

        List<ElementField> fields = new List<ElementField>();
        fields.Add(new ElementField
        {
            InputType = FormInputType.LogView,
            Name = "Log",
            Parameters = new Dictionary<string, object> {
                { nameof(Components.Inputs.InputLogView.RefreshUrl), url },
                { nameof(Components.Inputs.InputLogView.RefreshSeconds), 3 },
            }
        });

        await Editor.Open(new()
        {
            TypeName = "Pages.Dashboard", Title = libraryFileName, Fields = fields, Model = new { Log = log },
            Large = true, ReadOnly = true
        });
    }

    private async Task<RequestResult<string>> GetLog(string url)
    {
        return await HttpHelper.Get<string>(url);
    }

    /// <summary>
    /// Cancels an runner
    /// </summary>
    /// <param name="runnerUid">The UID of the runner</param>
    /// <param name="libraryFileUid">The UID of the library file</param>
    /// <param name="name">the filename for the confirm prompt</param>
    [JSInvokable]
    public async Task CancelRunner(Guid runnerUid, Guid libraryFileUid, string name)
    {

        if (await Confirm.Show("Labels.Cancel",
                Translater.Instant("Pages.Dashboard.Messages.CancelMessage", new {RelativeFile = name})) == false)
            return;
        
        Blocker.Show();
        try
        {
            await HttpHelper.Delete($"{ApiUrl}/{runnerUid}?libraryFileUid={libraryFileUid}");
            await Task.Delay(1000);
        }
        finally
        {
            Blocker.Hide();
        }
    }

    /// <summary>
    /// Removes a Widget from the dashboard
    /// </summary>
    /// <param name="WidgetUid">The UID of the Widget</param>
    [JSInvokable]
    public async Task<bool> RemoveWidget(Guid WidgetUid)
    {
        if (IsDefaultDashboard)
            return false; // cannot change default dashboard
        
        bool confirmed = await Confirm.Show("Labels.Remove", "Pages.Dashboard.Messages.DeleteWidget");
        if (confirmed)
            this.Widgets.RemoveAll(x => x.Uid == WidgetUid);
        return confirmed;
    }
    
    /// <summary>
    /// Saves a dashboard
    /// </summary>
    /// <param name="dashboardUid">The UID of the dashboard</param>
    /// <param name="Widgets">The Widgets to be saved</param>
    [JSInvokable]
    public async Task SaveDashboard(Guid dashboardUid, WidgetUiModel[] Widgets)
    {
        if (dashboardUid == FileFlows.Shared.Models.Dashboard.DefaultDashboardUid)
            return; // cannot change default dashboard

        var pActual = Widgets.Select(x => new Widget()
        {
            Height = x.Height,
            Width = x.Width,
            X = x.X,
            Y = x.Y,
            WidgetDefinitionUid = x.Uid
        }).ToArray();
        
        await HttpHelper.Put($"/api/dashboard/{dashboardUid}", pActual);
    }

    /// <summary>
    /// Humanizes a step name
    /// </summary>
    /// <param name="step">The unhumanized step name</param>
    /// <returns>the humanized step name</returns>
    [JSInvokable]
    public string HumanizeStepName(string step)
    {
        if (string.IsNullOrWhiteSpace(step))
            return "Starting...";
        string humanized = step.Humanize(LetterCasing.Title).Replace("Ffmpeg", "FFMPEG");
        return humanized;
    }
}