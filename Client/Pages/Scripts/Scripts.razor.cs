using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using FileFlows.Client.Components.Common;
using FileFlows.Client.Components.Dialogs;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Pages;

using FileFlows.Client.Components;

/// <summary>
/// Page for processing nodes
/// </summary>
public partial class Scripts : ListPage<string, Script>
{
    public override string ApiUrl => "/api/script";

    const string FileFlowsServer = "FileFlowsServer";

    private string TableIdentifier => "Scripts-" + this.SelectedType;
    
    private FlowSkyBox<ScriptType> Skybox;

    private Script EditingItem = null;
    [Inject] public IJSRuntime jsRuntime { get; set; }
    
    private List<Script> DataFlow = new();
    private List<Script> DataSystem = new();
    private List<Script> DataShared = new();
    private ScriptType SelectedType = ScriptType.Flow;

    private string lblUpdateScripts, lblUpdatingScripts, lblInUse, lblReadOnly, lblUpdateAvailable;

    /// <summary>
    /// Gets or sets the instance of the ScriptBrowser
    /// </summary>
    private ScriptBrowser ScriptBrowser { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        this.lblUpdateScripts = Translater.Instant("Pages.Scripts.Buttons.UpdateAllScripts");
        this.lblUpdatingScripts = Translater.Instant("Pages.Scripts.Labels.UpdatingScripts");
        lblInUse = Translater.Instant("Labels.InUse");
        lblReadOnly = Translater.Instant("Labels.ReadOnly");
        lblUpdateAvailable = Translater.Instant("Pages.Scripts.Labels.UpdateAvailable");
    }


    private async Task Add()
    {
        await Edit(new Script()
        {
            Type = SelectedType
        });
    }


    async Task<bool> Save(ExpandoObject model)
    {
#if (DEMO)
        return true;
#else
        Blocker.Show();
        this.StateHasChanged();

        try
        {
            var saveResult = await HttpHelper.Post<Script>($"{ApiUrl}", model);
            if (saveResult.Success == false)
            {
                Toast.ShowEditorError(saveResult.Body?.EmptyAsNull() ?? Translater.Instant("ErrorMessages.SaveFailed"));
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
#endif
    }

    private async Task Export()
    {
        var item = Table.GetSelected()?.FirstOrDefault();
        if (item == null)
            return;
        string url = $"/api/script/export/{item.Uid}";
#if (DEBUG)
        url = "http://localhost:6868" + url;
#endif
        await jsRuntime.InvokeVoidAsync("ff.downloadFile", new object[] { url, item.Name + ".js" });
    }

    private async Task Import()
    {
        var idResult = await ImportDialog.Show("js");
        string js = idResult.content;
        if (string.IsNullOrEmpty(js))
            return;

        Blocker.Show();
        try
        {
            var newItem = await HttpHelper.Post<Script>("/api/script/import?filename=" + UrlEncoder.Create().Encode(idResult.filename), js);
            if (newItem != null && newItem.Success)
            {
                await this.Refresh();
                Toast.ShowSuccess(Translater.Instant("Pages.Scripts.Messages.Imported",
                    new { name = newItem.Data.Name }));
            }
            else
            {
                Toast.ShowError(newItem.Body?.EmptyAsNull() ?? "Invalid script");
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
            string url = $"/api/script/duplicate/{item.Uid}?type={SelectedType}";
#if (DEBUG)
            url = "http://localhost:6868" + url;
#endif
            var newItem = await HttpHelper.Get<Script>(url);
            if (newItem != null && newItem.Success)
            {
                await this.Refresh();
                Toast.ShowSuccess(Translater.Instant("Pages.Script.Messages.Duplicated",
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

    protected override string DeleteUrl => $"{ApiUrl}?type={SelectedType}";

    public override async Task Delete()
    {
        var used = Table.GetSelected()?.Any(x => x.UsedBy?.Any() == true) == true;
        if (used)
        {
            Toast.ShowError("Pages.Scripts.Messages.DeleteUsed");
            return;
        }

        await base.Delete();
        await Refresh();
    }


    private async Task UsedBy()
    {
        var item = Table.GetSelected()?.FirstOrDefault();
        if (item?.UsedBy?.Any() != true)
            return;
        await UsedByDialog.Show(item.UsedBy);
    }
    /// <summary>
    /// Opens the used by dialog
    /// </summary>
    /// <param name="item">the item to open used by for</param>
    /// <returns>a task to await</returns>
    private Task OpenUsedBy(Script item)
        => UsedByDialog.Show(item.UsedBy);

    
    
    public override Task PostLoad()
    {
        UpdateTypeData();
        return Task.CompletedTask;
    }
    
    private void UpdateTypeData()
    {
        this.DataFlow = this.Data.Where(x => x.Type == ScriptType.Flow).ToList();
        this.DataSystem = this.Data.Where(x => x.Type == ScriptType.System).ToList();
        this.DataShared = this.Data.Where(x => x.Type == ScriptType.Shared).ToList();
        foreach (var script in this.Data)
        {
            if (script.Code?.StartsWith("// path: ") == true)
                script.Code = Regex.Replace(script.Code, @"^\/\/ path:(.*?)$", string.Empty, RegexOptions.Multiline).Trim();
        }
        this.Skybox.SetItems(new List<FlowSkyBoxItem<ScriptType>>()
        {
            new ()
            {
                Name = "Flow Scripts",
                Icon = "fas fa-sitemap",
                Count = this.DataFlow.Count,
                Value = ScriptType.Flow
            },
            Profile.LicensedFor(LicenseFlags.Tasks) ? new ()
            {
                Name = "System Scripts",
                Icon = "fas fa-laptop-code",
                Count = this.DataSystem.Count,
                Value = ScriptType.System
            } : null,
            new ()
            {
                Name = "Shared Scripts",
                Icon = "fas fa-handshake",
                Count = this.DataShared.Count,
                Value = ScriptType.Shared
            }
        }, this.SelectedType);
    }
    
    async Task Browser()
    {
        bool result = await ScriptBrowser.Open(this.SelectedType);
        if (result)
            await this.Refresh();
    }

    async Task Update()
    {
        var scripts = Table.GetSelected()?.Where(x => string.IsNullOrEmpty(x.Path) == false)?.Select(x => x.Path)?.ToArray() ?? new string[] { };
        if (scripts?.Any() != true)
        {
            Toast.ShowWarning("Pages.Scripts.Messages.NoRepositoryScriptsToUpdate");
            return;
        }

        Blocker.Show("Pages.Scripts.Labels.UpdatingScripts");
        this.StateHasChanged();
        Data.Clear();
        try
        {
            var result = await HttpHelper.Post($"/api/repository/update-specific-scripts", new ReferenceModel<string> { Uids = scripts });
            if (result.Success)
                await Refresh();
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
    }

    
    private void SetSelected(FlowSkyBoxItem<ScriptType> item)
    {
        SelectedType = item.Value;
        // need to tell table to update so the "Default" column is shown correctly
        Table.TriggerStateHasChanged();
        this.StateHasChanged();
    }

    private async Task UpdateScripts()
    {
        this.Blocker.Show(lblUpdatingScripts);
        try
        {
            await HttpHelper.Post("/api/repository/update-scripts");
            await Refresh();
        }
        finally
        {
            this.Blocker.Hide();
        }
    }

    private string GetIcon(Script item)
    {
        string url = "";
#if (DEBUG)
        url = "http://localhost:6868" + url;
#endif
        string nameLower = item.Name.ToLowerInvariant();
        if (nameLower.StartsWith("video"))
            return "/icons/video.svg";
        if (item.Name == "FILE_DISPLAY_NAME")
            return "fas fa-signature";
        if (nameLower.StartsWith("fileflows"))
            return "/favicon.svg";
        if (nameLower.StartsWith("image"))
            return "/icons/image.svg";
        if (nameLower.StartsWith("folder"))
            return "/icons/filetypes/folder.svg";
        if (nameLower.StartsWith("file"))
            return url + "/icon/filetype/file.svg";
        if (nameLower.StartsWith("7zip"))
            return url + "/icon/filetype/7z.svg";
        if (nameLower.StartsWith("language"))
            return "fas fa-comments";
        var icons = new[]
        {
            "apple", "apprise", "audio", "basic", "comic", "database", "docker", "emby", "folder", "gotify", "gz",
            "image", "intel", "linux", "nvidia", "plex", "pushbullet", "pushover", "radarr", "sabnzbd", "sonarr", "video", "windows"
        };
        foreach (var icon in icons)
        {
            if (nameLower.StartsWith(icon))
                return $"/icons/{icon}.svg";
        }

        return "fas fa-scroll";

    }
}