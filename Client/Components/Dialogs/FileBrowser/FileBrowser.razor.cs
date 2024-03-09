using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using FileFlows.Shared;
using FileFlows.Shared.Helpers;
using System.Linq;
using FileFlows.Shared.Models;

namespace FileFlows.Client.Components.Dialogs;

public partial class FileBrowser : ComponentBase, IDisposable
{
    private string lblSelect, lblCancel;
    private string Title;

    private bool DirectoryMode = false;
    private string[] Extensions = new string[] { };
    TaskCompletionSource<string> ShowTask;

    private static FileBrowser Instance { get; set; }

    private FileBrowserItem Selected;
    List<FileBrowserItem> Items = new List<FileBrowserItem>();

    private bool Visible { get; set; }

    private const string API_URL = "/api/file-browser";

    protected override void OnInitialized()
    {
        this.lblSelect = Translater.Instant("Labels.Select");
        this.lblCancel = Translater.Instant("Labels.Cancel");
        App.Instance.OnEscapePushed += InstanceOnOnEscapePushed;
        Instance = this;
    }

    private void InstanceOnOnEscapePushed(OnEscapeArgs args)
    {
        if (Visible)
        {
            Cancel();
            this.StateHasChanged();
        }
    }

    public static Task<string> Show(string start, bool directory = false, string[] extensions = null)
    {
        if (Instance == null)
            return Task.FromResult<string>("");

        return Instance.ShowInstance(start, directory, extensions);
    }

    private Task<string> ShowInstance(string start, bool directory = false, string[] extensions = null)
    {
        this.Extensions = extensions ?? new string[] { };
        this.DirectoryMode = directory;

        this.Title = Translater.TranslateIfNeeded("Dialogs.FileBrowser.FileTitle");
        _ = this.LoadPath(start);
        this.Visible = true;
        this.StateHasChanged();

        Instance.ShowTask = new TaskCompletionSource<string>();
        return Instance.ShowTask.Task;
    }

    private async void Select()
    {
        if (Selected == null)
            return;
        this.Visible = false;
        Instance.ShowTask.TrySetResult(Selected.IsParent ? Selected.Name : Selected.FullName);
        await Task.CompletedTask;
    }

    private async void Cancel()
    {
        this.Visible = false;
        Instance.ShowTask.TrySetResult("");
        await Task.CompletedTask;
    }

    private async Task SetSelected(FileBrowserItem item)
    {
        if (DirectoryMode == false && (item.IsPath || item.IsDrive || item.IsParent))
            return;
        if (this.Selected == item)
            this.Selected = null;
        else
            this.Selected = item;
        await Task.CompletedTask;
    }

    private async Task DblClick(FileBrowserItem item)
    {
        if (item.IsParent || item.IsPath || item.IsDrive)
            await LoadPath(item.FullName);
        else
        {
            this.Selected = item;
            this.Select();
        }
    }

    private async Task LoadPath(string path)
    {
        var result = await GetPathData(path);
        if (result.Success)
        {
            this.Items = result.Data;
            var parent = this.Items.Where(x => x.IsParent).FirstOrDefault();
            if (parent != null)
                this.Title = parent.Name;
            else
                this.Title = "Root";
            this.StateHasChanged();
        }
    }

    private async Task<RequestResult<List<FileBrowserItem>>> GetPathData(string path)
    {
#if (DEMO)
        List<FileBrowserItem> items = new List<FileBrowserItem>();
        items.AddRange(Enumerable.Range(1, 5).Select(x => new FileBrowserItem { IsPath = true, Name = "Demo Folder " + x, FullName = "DemoFolder" + x }));

        if (DirectoryMode == false)
        {
            var random = new Random(DateTime.UtcNow.Millisecond);
            items.AddRange(Enumerable.Range(1, 5).Select(x =>
            {
                string extension = "." + (Extensions?.Any() != true ? "mkv" : Extensions[random.Next(0, Extensions.Length)]);
                return new FileBrowserItem { IsPath = true, Name = "DemoFile" + x + extension, FullName = "DemoFile" + x + extension };
            }));
        }
        return new RequestResult<List<FileBrowserItem>> { Success = true, Data = items };
#else
        return await HttpHelper.Get<List<FileBrowserItem>>($"{API_URL}?includeFiles={DirectoryMode == false}" +
        $"&start={Uri.EscapeDataString(path)}" +
        string.Join("", Extensions?.Select(x => "&extensions=" + Uri.EscapeDataString(x))?.ToArray() ?? new string[] { }));
#endif
    }

    public void Dispose()
    {
        App.Instance.OnEscapePushed -= InstanceOnOnEscapePushed;
    }
}