using System.Text;
using System.Text.Json;
using FileFlows.Client.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Pages;

/// <summary>
/// Page the shows the system's DockerMods
/// </summary>
public partial class DockerMods : ListPage<Guid, DockerMod>
{
    /// <summary>
    /// The API URL
    /// </summary>
    public override string ApiUrl => "/api/dockermod";

    /// <summary>
    /// Gets or sets the DockerMod Browser isntance
    /// </summary>
    private RepositoryBrowser Browser { get; set; }

    /// <summary>
    /// Gets or sets the JavaScript runtime
    /// </summary>
    [Inject] protected IJSRuntime jsRuntime { get; set; }

    /// <summary>
    /// Translated strings
    /// </summary>
    private string lblUpdateAvailable, lblRevision;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        lblUpdateAvailable = Translater.Instant("Pages.DockerMod.Labels.UpdateAvailable");
        lblRevision = Translater.Instant("Pages.DockerMod.Labels.Revision");
    }


    Task Add()
        => OpenEditor(new ()
        {
            Code = "#!/bin/bash\n\n",
            Enabled = true
        });
    public override Task<bool> Edit(DockerMod item)
        => OpenEditor(item);

    private Task DoubleClick(DockerMod item)
        => OpenEditor(item);

    
    async Task OpenBrowser()
    {
        bool result = await Browser.Open();
        if (result)
            await Refresh();
    }


    async Task Update()
    {
        var items = Table.GetSelected()?.Select(x => x.Uid)?.ToArray() ?? new System.Guid[] { };
        if (items?.Any() != true)
            return;
        await Update(items);
    }
    
    async Task Update(params Guid[] items)
    {
        Blocker.Show("Pages.DockerMod.Messages.Updating");
        this.StateHasChanged();
        Data.Clear();
        try
        {
            await HttpHelper.Post($"/api/repository/DockerMod/update", new ReferenceModel<Guid> { Uids = items });
            await Refresh();
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
    }
    
    /// <summary>
    /// we only want to do the sort the first time, otherwise the list will jump around for the user
    /// </summary>
    private List<Guid> initialSortOrder;
    
    /// <inheritdoc />
    public override Task PostLoad()
    {
        if (initialSortOrder == null)
        {
            Data = Data?.OrderByDescending(x => x.Enabled)?.ThenBy(x => x.Name)
                ?.ToList();
            initialSortOrder = Data?.Select(x => x.Uid)?.ToList();
        }
        else
        {
            Data = Data?.OrderBy(x => initialSortOrder.Contains(x.Uid) ? initialSortOrder.IndexOf(x.Uid) : 1000000)
                .ThenBy(x => x.Name)
                ?.ToList();
        }
        return base.PostLoad();
    }

    /// <summary>
    /// Exports a DockerHub command
    /// </summary>
    private async Task Export()
    {
        var item = Table?.GetSelected()?.FirstOrDefault();
        if (item == null)
            return;
        string url = $"{ApiUrl}/export/{item.Uid}";
#if (DEBUG)
        url = "http://localhost:6868" + url;
#endif
        
        var result = await HttpHelper.Get<string>(url);
        if (result.Success == false)
        {
            Toast.ShowError(Translater.Instant("Pages.DockerMod.Messages.FailedToExport"));
            return;
        }

        await jsRuntime.InvokeVoidAsync("ff.saveTextAsFile", item.Name  + ".sh", result.Body);
    }
}