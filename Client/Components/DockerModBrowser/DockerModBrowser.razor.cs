using Microsoft.AspNetCore.Components;
using FileFlows.Client.Components.Inputs;
using FileFlows.Plugin;
using FileFlows.Client.Components.Common;

namespace FileFlows.Client.Components;

/// <summary>
/// Browser for DockerMods
/// </summary>
public partial class DockerModBrowser : ComponentBase
{
    /// <summary>
    /// The URL to get the DockerMods
    /// </summary>
    const string ApiUrl = "/api/docker";
    /// <summary>
    /// Gets or sets the blocker
    /// </summary>
    [CascadingParameter] public Blocker Blocker { get; set; }
    /// <summary>
    /// Gets or sets the editor
    /// </summary>
    [CascadingParameter] public Editor Editor { get; set; }
    /// <summary>
    /// Gets or sets the table
    /// </summary>
    public FlowTable<DockerMod> Table { get; set; }
    /// <summary>
    /// Gets or sets if this is visible
    /// </summary>
    public bool Visible { get; set; }
    /// <summary>
    /// If this has been updated
    /// </summary>
    private bool Updated;
    /// <summary>
    /// The translated labels
    /// </summary>
    private string lblTitle, lblClose;

    /// <summary>
    /// The open task to complete when closing
    /// </summary>
    TaskCompletionSource<bool> OpenTask;
    /// <summary>
    /// If the components needs rendering
    /// </summary>
    private bool _needsRendering = false;
    /// <summary>
    /// If this component is loading
    /// </summary>
    private bool Loading = false;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        lblClose = Translater.Instant("Labels.Close");
        lblTitle = Translater.Instant("Pages.Plugins.Labels.PluginBrowser");
    }

    /// <summary>
    /// Opens the plugin browser
    /// </summary>
    /// <returns>returns true if one or more plugins were downloaded</returns>
    internal Task<bool> Open()
    {
        this.Visible = true;
        this.Loading = true;
        this.Table.Data = new List<DockerMod>();
        OpenTask = new TaskCompletionSource<bool>();
        _ = LoadData();
        this.StateHasChanged();
        return OpenTask.Task;
    }

    /// <summary>
    /// Loads the plugins
    /// </summary>
    private async Task LoadData()
    {
        this.Loading = true;
        Blocker.Show();
        this.StateHasChanged();
        try
        {
            var result = await HttpHelper.Get<List<DockerMod>>(ApiUrl + "/plugin-packages?missing=true");
            if (result.Success == false)
            {
                Toast.ShowError(result.Body, duration: 15_000);
                // close this and show message
                this.Close();
                return;
            }
            this.Table.Data = result.Data;
            this.Loading = false;
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
    }

    /// <summary>
    /// Waits for the component to render
    /// </summary>
    private async Task WaitForRender()
    {
        _needsRendering = true;
        StateHasChanged();
        while (_needsRendering)
        {
            await Task.Delay(50);
        }
    }

    /// <summary>
    /// Closes the component
    /// </summary>
    private void Close()
    {
        OpenTask.TrySetResult(Updated);
        this.Visible = false;
    }

    /// <summary>
    /// Downloads the selected plugins
    /// </summary>
    private async Task Download()
    {
        var selected = Table.GetSelected().ToArray();
        var items = selected;
        if (items.Any() == false)
            return;
        this.Blocker.Show();
        this.StateHasChanged();
        try
        {
            this.Updated = true;
            var result = await HttpHelper.Post(ApiUrl + "/download", new { Items = items });
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
        await LoadData();
    }

    /// <summary>
    /// When the view button is clicked
    /// </summary>
    private async Task ViewAction()
    {
        var item = Table.GetSelected().FirstOrDefault();
        if (item != null)
            await View(item);
    }

    /// <summary>
    /// Views the item
    /// </summary>
    /// <param name="item">the item</param>
    private async Task View(DockerMod item)
    {
        await Editor.Open(new () { TypeName = "Pages.DockerMods", Title = item.Name, Fields = new List<ElementField>
        {
            new ()
            {
                Name = nameof(item.Name),
                InputType = FormInputType.TextLabel
            },
            new ()
            {
                Name = nameof(item.Author),
                InputType = FormInputType.TextLabel
            },
            new ()
            {
                Name = nameof(item.Revision),
                InputType = FormInputType.TextLabel
            },
            new ()
            {
                Name = nameof(item.Description),                    
                InputType = FormInputType.TextLabel,
                Parameters = new Dictionary<string, object>
                {
                    { nameof(InputTextLabel.Pre), true }
                }
            }
        }, Model = item, ReadOnly= true});
    }

}