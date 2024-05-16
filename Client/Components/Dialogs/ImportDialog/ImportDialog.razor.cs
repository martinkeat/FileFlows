using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using FileFlows.Shared;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Forms;
using System.IO;

namespace FileFlows.Client.Components.Dialogs;

public partial class ImportDialog : ComponentBase, IDisposable
{
    private string lblImport, lblCancel, lblBrowse;
    private string Message, Title;
    TaskCompletionSource<(string filename, string content)> ShowTask;

    private string FileName { get; set; }
    private bool HasFile { get; set; }
    private static ImportDialog Instance { get; set; }

    private bool Visible { get; set; }

    private string Value { get; set; }

    private string[] Extensions = new[] { "json" };
    private string AcceptedTypes;

    private string Uid = System.Guid.NewGuid().ToString();


    [Inject] private IJSRuntime jsRuntime { get; set; }

    protected override void OnInitialized()
    {
        this.lblImport = Translater.Instant("Labels.Import");
        this.lblCancel = Translater.Instant("Labels.Cancel");
        this.lblBrowse = Translater.Instant("Labels.Browse");
        this.Title = Translater.Instant("Dialogs.Import.Title");
        this.Message = Translater.Instant("Dialogs.Import.Message");
        Instance = this;
        App.Instance.OnEscapePushed += InstanceOnOnEscapePushed;
    }

    private void InstanceOnOnEscapePushed(OnEscapeArgs args)
    {
        if (Visible)
        {
            Cancel();
            this.StateHasChanged();
        }
    }


    public static Task<(string filename, string content)> Show(params string[] extensions)
    {
        if (Instance == null)
            return Task.FromResult<(string, string)>((string.Empty, string.Empty));

        return Instance.ShowInstance(extensions);
    }

    private Task<(string filename, string content)> ShowInstance(string[] extensions = null)
    {
        this.Value = string.Empty;
        this.FileName = string.Empty;
        this.Visible = true;
        this.Extensions = extensions?.Any() == true ? extensions : new[] { "json" };
        this.AcceptedTypes = string.Join(", ", this.Extensions.Select(x => "." + x));
        this.StateHasChanged();

        Instance.ShowTask = new TaskCompletionSource<(string filename, string content)>();
        return Instance.ShowTask.Task;
    }

    private async void Accept()
    {
        this.Visible = false;
        Instance.ShowTask.TrySetResult((FileName, Value));
        await Task.CompletedTask;
    }

    private async void Cancel()
    {
        this.Visible = false;
        Instance.ShowTask.TrySetResult((string.Empty, string.Empty));
        await Task.CompletedTask;
    }

    private async Task LoadFile(InputFileChangeEventArgs e)
    {
        if (e.FileCount == 0)
        {
            FileName = string.Empty;
            Value = string.Empty;
            HasFile = false;
            return;
        }
        FileName = e.File.Name;
        using var reader = new StreamReader(e.File.OpenReadStream());
        this.Value = await reader.ReadToEndAsync();
        this.HasFile = string.IsNullOrWhiteSpace(this.Value) == false;
        this.StateHasChanged();
    }

    public void Dispose()
    {
        App.Instance.OnEscapePushed -= InstanceOnOnEscapePushed;
    }
}