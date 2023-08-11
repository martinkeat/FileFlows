using System.Net.Http;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components;
using ffElement = FileFlows.Shared.Models.FlowElement;
using FileFlows.Client.Components.Inputs;
using FileFlows.Plugin;
using FileFlows.Client.Components.Common;


namespace FileFlows.Client.Components;

public partial class ScriptBrowser: ComponentBase
{
    const string ApiUrl = "/api/repository";
    [CascadingParameter] public Blocker Blocker { get; set; }
    [CascadingParameter] public Editor Editor { get; set; }

    public FlowTable<RepositoryObject> Table { get; set; }

    public bool Visible { get; set; }

    private bool Updated;

    private string lblTitle, lblClose;

    TaskCompletionSource<bool> OpenTask;

    private bool _needsRendering = false;

    private bool Loading = false;

    private string Icon = "fas fa-code";

    private ScriptType ScriptType;

    protected override void OnInitialized()
    {
        lblClose = Translater.Instant("Labels.Close");
        lblTitle = Translater.Instant("Pages.Scripts.Labels.ScriptBrowser");
    }

    internal Task<bool> Open(ScriptType type)
    {
        this.ScriptType = type;
        lblTitle = Translater.Instant("Pages.Scripts.Labels.ScriptBrowser") + " - " + type + " Scripts";
        Icon = "fas fa-code";

        this.Visible = true;
        this.Loading = true;
        this.Table.Data = new List<RepositoryObject>();
        OpenTask = new TaskCompletionSource<bool>();
        App.Instance.OnEscapePushed += InstanceOnOnEscapePushed;
        _ = LoadData();
        this.StateHasChanged();
        return OpenTask.Task;
    }

    private void InstanceOnOnEscapePushed(OnEscapeArgs args)
    {
        if (args.HasModal || Editor.Visible)
            return;
        
        this.Close();
    }

    private async Task LoadData()
    {
        this.Loading = true;
        Blocker.Show();
        this.StateHasChanged();
        try
        {
            var result = await HttpHelper.Get<List<RepositoryObject>>(ApiUrl + "/scripts?missing=true&type=" + ScriptType);
            if (result.Success == false)
            {
                // close this and show message
                this.Close();
                return;
            }
            this.Table.Data = result.Data.OrderBy(x => x.Name).ToList();
            this.Loading = false;
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
    }

    private async Task WaitForRender()
    {
        _needsRendering = true;
        StateHasChanged();
        while (_needsRendering)
        {
            await Task.Delay(50);
        }
    }

    private void Close()
    {
        App.Instance.OnEscapePushed -= InstanceOnOnEscapePushed;
        OpenTask.TrySetResult(Updated);
        this.Visible = false;
        this.StateHasChanged();
    }

    private async Task Download()
    {
        var selected = Table.GetSelected().ToArray();
        var items = selected.Select(x => x.Path).ToList();
        if (items.Any() == false)
            return;
        this.Blocker.Show();
        this.StateHasChanged();
        try
        {
            this.Updated = true;
            var result = await HttpHelper.Post(ApiUrl + "/download", new { Scripts = items });
            if (result.Success == false)
            {
                // close this and show message
                return;
            }
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Error: " + ex.Message);
        }
        finally
        {
            this.Blocker.Hide();
            this.StateHasChanged();
        }
        this.Close();
        //await LoadData();
    }

    private async Task ViewAction()
    {
        var item = Table.GetSelected().FirstOrDefault();
        if (item != null)
            await View(item);
    }

    private async Task View(RepositoryObject @object)
    {
        Blocker.Show();
        string code;
        try
        {
            var response =
                await HttpHelper.Get<string>(ApiUrl + "/content?path=" + UrlEncoder.Create().Encode(@object.Path));
            if (response.Success == false)
                return;
            code = response.Data;
        }
        finally
        {
            Blocker.Hide();
        }

        await Editor.Open(new () { TypeName = "Pages.Scripts", Title = @object.Name, Fields = new List<ElementField>
        {
            new()
            {
                Name = "Code",
                InputType = FormInputType.Code
            },
        }, Model = new { Code = code }, ReadOnly = true});
    }
}