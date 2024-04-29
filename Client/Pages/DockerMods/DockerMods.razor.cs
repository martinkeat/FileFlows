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
    private DockerModBrowser Browser { get; set; }

    /// <summary>
    /// Gets or sets the JavaScript runtime
    /// </summary>
    [Inject] protected IJSRuntime jsRuntime { get; set; }



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
            var result = await HttpHelper.Post($"{ApiUrl}/update", new ReferenceModel<Guid> { Uids = items });
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
        var mod = Table?.GetSelected()?.FirstOrDefault();
        if (mod == null)
            return;

        var sb = new StringBuilder();
        sb.AppendLine("# --------------------------------------------------------------------------------------------------------------------------------------------");
        sb.AppendLine("# Name: " + mod.Name);
        sb.AppendLine("# Description: " + mod.Description.Replace("\n", "\n# "));
        sb.AppendLine("# Author: Enter Your Name");
        sb.AppendLine("# Revision: " + mod.Revision);
        sb.AppendLine("# Icon: " + mod.Icon);
        sb.AppendLine("# --------------------------------------------------------------------------------------------------------------------------------------------");
        sb.AppendLine();
        sb.Append(mod.Code);

        await jsRuntime.InvokeVoidAsync("ff.saveTextAsFile", $"{mod.Name}.sh", sb.ToString());       
    }
}