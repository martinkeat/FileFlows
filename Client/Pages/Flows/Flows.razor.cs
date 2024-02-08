using FileFlows.Client.Components.Common;
using Microsoft.AspNetCore.Components;
using FileFlows.Client.Components;
using FileFlows.Client.Components.Dialogs;
using FileFlows.Plugin;
using Microsoft.JSInterop;
using ffFlow = FileFlows.Shared.Models.Flow;

namespace FileFlows.Client.Pages;

public partial class Flows : ListPage<Guid, FlowListModel>
{
    /// <summary>
    /// Gets or sets the navigation manager
    /// </summary>
    [Inject] NavigationManager NavigationManager { get; set; }
    /// <summary>
    /// Gets or sets the JavaScript runtime
    /// </summary>
    [Inject] public IJSRuntime jsRuntime { get; set; }

    private FlowTemplatePicker TemplatePicker;
    private NewFlowEditor AddEditor;
    private string TableIdentifier => "Flows-" + this.SelectedType;

    public override string ApiUrl => "/api/flow";

    private FlowSkyBox<FlowType> Skybox;

    private List<FlowListModel> DataStandard = new();
    private List<FlowListModel> DataSubFlows = new();
    private List<FlowListModel> DataFailure = new();
    private FlowType SelectedType = FlowType.Standard;

    public override string FetchUrl => ApiUrl + "/list-all";


    async Task Enable(bool enabled, ffFlow flowWrapper)
    {
        Blocker.Show();
        try
        {
            await HttpHelper.Put<ffFlow>($"{ApiUrl}/state/{flowWrapper.Uid}?enable={enabled}");
        }
        finally
        {
            Blocker.Hide();
        }
    }

    private async void Add()
    {
        if (TemplatePicker == null)
        {
            NavigationManager.NavigateTo("flows/" + Guid.Empty);
            return;
        }

        var flowTemplateModel = await TemplatePicker.Show(SelectedType);
        if (flowTemplateModel == null)
            return; // twas canceled
        if (flowTemplateModel.Fields?.Any() != true)
        {
            // nothing extra to fill in, go to the flow editor, typically this if basic flows
            App.Instance.NewFlowTemplate = flowTemplateModel.Flow;
            NavigationManager.NavigateTo("flows/" + Guid.Empty);
            return;
        }
        
        Logger.Instance.ILog("flowTemplateModel: " , flowTemplateModel);
        var newFlow = await AddEditor.Show(flowTemplateModel);
        if (newFlow == null)
            return; // was canceled
        
        if (newFlow.Uid != Guid.Empty)
        {
            if ((App.Instance.FileFlowsSystem.ConfigurationStatus & ConfigurationStatus.Flows) != ConfigurationStatus.Flows)
            {
                // refresh the app configuration status
                await App.Instance.LoadAppInfo();
            }
            // was saved, refresh list
            await this.Refresh();
        }
        else
        {
            // edit it
            App.Instance.NewFlowTemplate = newFlow;
            NavigationManager.NavigateTo("flows/" + Guid.Empty);
        }
    }

    public override async Task<bool> Edit(FlowListModel item)
    {
        if(item != null)
            NavigationManager.NavigateTo("flows/" + item.Uid);
        return await Task.FromResult(false);
    }

    private async Task Export()
    {
        var items = Table.GetSelected()?.ToList() ?? new (); 
        if (items.Any() != true)
            return;
        string url = $"/api/flow/export?{string.Join("&", items.Select(x => "uid=" + x.Uid))}";
#if (DEBUG)
        url = "http://localhost:6868" + url;
#endif
        await jsRuntime.InvokeVoidAsync("ff.downloadFile", new object[] { url, items.Count() == 1 ? items[0].Name + ".json" : "Flows.zip" });
    }

    private async Task Import()
    {
        var idResult = await ImportDialog.Show();
        string json = idResult.content;
        if (string.IsNullOrEmpty(json))
            return;

        Blocker.Show();
        try
        {
            var newFlow = await HttpHelper.Post<ffFlow>("/api/flow/import", json);
            if (newFlow != null && newFlow.Success)
            {
                await this.Refresh();
                Toast.ShowSuccess(Translater.Instant("Pages.Flows.Messages.FlowImported", new { name = newFlow.Data.Name }));
            }
        }
        finally
        {
            Blocker.Hide();
        }
    }

    private async Task Duplicate()
    {
        Blocker.Show();
        try
        {
            var item = Table.GetSelected()?.FirstOrDefault();
            if (item == null)
                return;
            string url = $"/api/flow/duplicate/{item.Uid}";
#if (DEBUG)
            url = "http://localhost:6868" + url;
#endif
            var newItem = await HttpHelper.Get<Script>(url);
            if (newItem != null && newItem.Success)
            {
                await this.Refresh();
                Toast.ShowSuccess(Translater.Instant("Pages.Flows.Messages.Duplicated",
                    new { name = newItem.Data.Name }));
            }
            else
            {
                Toast.ShowError(newItem.Body?.EmptyAsNull() ?? "Failed to duplicate");
            }
        }
        finally
        {
            Blocker.Hide();
        }
    }

    protected override Task PostDelete() => Refresh();

    public override Task PostLoad()
    {
        UpdateTypeData();
        return Task.CompletedTask;
    }
    
    private void UpdateTypeData()
    {
        this.DataFailure = this.Data.Where(x => x.Type == FlowType.Failure).ToList();
        this.DataStandard = this.Data.Where(x => x.Type == FlowType.Standard).ToList();
        this.DataSubFlows = this.Data.Where(x => x.Type == FlowType.SubFlow).ToList();
        this.Skybox.SetItems(new List<FlowSkyBoxItem<FlowType>>()
        {
            new ()
            {
                Name = "Standard Flows",
                Icon = "fas fa-sitemap",
                Count = this.DataStandard.Count,
                Value = FlowType.Standard
            },
            App.Instance.FileFlowsSystem?.LicenseWebhooks == true ? new ()
            {
                Name = "Sub Flows",
                Icon = "fas fa-subway",
                Count = this.DataSubFlows.Count,
                Value = FlowType.SubFlow
            } : null,
            new ()
            {
                Name = "Failure Flows",
                Icon = "fas fa-exclamation-circle",
                Count = this.DataFailure.Count,
                Value = FlowType.Failure
            }
        }.Where(x => x != null).ToList(), this.SelectedType);
    }

    private void SetSelected(FlowSkyBoxItem<FlowType> item)
    {
        SelectedType = item.Value;
        // need to tell table to update so the "Default" column is shown correctly
        Table.TriggerStateHasChanged();
        this.StateHasChanged();
    }

    private async Task SetDefault()
    {
        var item = Table.GetSelected()?.FirstOrDefault();
        if (item == null)
            return;
        
        Blocker.Show();
        try
        {
            await HttpHelper.Put($"/api/flow/set-default/{item.Uid}?default={(!item.Default)}");
            await this.Refresh();
        }
        finally
        {
            Blocker.Hide();
        }
    }

    public override async Task Delete()
    {
        var used = Table.GetSelected()?.Any(x => x.UsedBy?.Any() == true) == true;
        if (used)
        {
            Toast.ShowError("Pages.Flows.Messages.DeleteUsed");
            return;
        }
        await base.Delete();
    }

    private async Task UsedBy()
    {
        var item = Table.GetSelected()?.FirstOrDefault();
        if (item?.UsedBy?.Any() != true)
            return;
        await UsedByDialog.Show(item.UsedBy);
    }
}
