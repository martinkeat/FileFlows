namespace FileFlows.Client.Pages;

using FileFlows.Client.Components;

/// <summary>
/// Page for processing nodes
/// </summary>
public partial class Nodes : ListPage<Guid, ProcessingNode>
{
    public override string ApiUrl => "/api/node";
    const string FileFlowsServer = "FileFlowsServer";

    private ProcessingNode EditingItem = null;

    private string lblInternal, lblAddress, lblRunners, lblVersion, lblDownloadNode, lblUpgradeRequired, lblUpgradeRequiredHint;
     
#if(DEBUG)
    string DownlaodUrl = "http://localhost:6868/download";
#else
    string DownlaodUrl = "/download";
#endif
    protected override void OnInitialized()
    {
        base.OnInitialized();
        lblInternal= Translater.Instant("Pages.Nodes.Labels.Internal");
        lblAddress = Translater.Instant("Pages.Nodes.Labels.Address");
        lblRunners = Translater.Instant("Pages.Nodes.Labels.Runners");
        lblVersion = Translater.Instant("Pages.Nodes.Labels.Version");
        lblDownloadNode = Translater.Instant("Pages.Nodes.Labels.DownloadNode");
        lblUpgradeRequired = Translater.Instant("Pages.Nodes.Labels.UpgradeRequired");
        lblUpgradeRequiredHint = Translater.Instant("Pages.Nodes.Labels.UpgradeRequiredHint");
    }


    private Task Add()
        => Edit(new ProcessingNode());

    public override Task PostLoad()
    {
        var serverNode = this.Data?.Where(x => x.Address == FileFlowsServer).FirstOrDefault();
        if(serverNode != null)
        {
            serverNode.Name = Translater.Instant("Pages.Nodes.Labels.FileFlowsServer");                
        }
        return base.PostLoad();
    }


    async Task Enable(bool enabled, ProcessingNode node)
    {
        Blocker.Show();
        try
        {
            await HttpHelper.Put<ProcessingNode>($"{ApiUrl}/state/{node.Uid}?enable={enabled}");
            await Refresh();
        }
        finally
        {
            Blocker.Hide();
        }
    }

    async Task<bool> Save(ExpandoObject model)
    {
        Blocker.Show();
        this.StateHasChanged();

        try
        {
            var saveResult = await HttpHelper.Post<ProcessingNode>($"{ApiUrl}", model);
            if (saveResult.Success == false)
            {
                Toast.ShowError( saveResult.Body?.EmptyAsNull() ?? Translater.Instant("ErrorMessages.SaveFailed"));
                return false;
            }

            int index = this.Data.FindIndex(x => x.Uid == saveResult.Data.Uid);
            if (index < 0)
                this.Data.Add(saveResult.Data);
            else
                this.Data[index] = saveResult.Data;
            await this.Load(saveResult.Data.Uid);

            return true;
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
    }

}