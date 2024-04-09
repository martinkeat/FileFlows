using FileFlows.Client.Components.Common;
using Humanizer;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components;

/// <summary>
/// Audit history popup
/// </summary>
public partial class AuditHistory
{
    /// <summary>
    /// Gets the static instance of the audit history
    /// </summary>
    public static AuditHistory Instance { get;private set; }
    
    /// <summary>
    /// Gets or sets the blocker tho show
    /// </summary>
    [CascadingParameter] public Blocker Blocker { get; set; }
    
    TaskCompletionSource ShowTask;
    private Guid Uid;
    private string Type;
    private string Title;
    private bool Visible;
    private string lblClose;
    private List<AuditEntry> Data = new ();
    private bool AwaitingRender = false;
    public FlowTable<AuditEntry> Table { get; set; }

    /// <summary>
    /// Constructs a new instance of the Audit history component
    /// </summary>
    public AuditHistory()
    {
        Instance = this;
    }

    protected override void OnInitialized()
    {
        lblClose = Translater.Instant("Labels.Close");
    }
    
    private void Close()
    {
        this.Visible = false;
        this.Data.Clear();
        this.ShowTask.SetResult();
    }

    private async Task AwaitRender()
    {
        AwaitingRender = true;
        this.StateHasChanged();
        await Task.Delay(10);
        while (AwaitingRender)
            await Task.Delay(10);
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (AwaitingRender)
            AwaitingRender = false;
    }

    public Task Show(Guid uid, string type)
    {
        this.Uid = uid;
        this.Type = type;
        this.Title = Translater.Instant("Labels.Audit");
        this.Blocker.Show();
        Instance.ShowTask = new ();
        _ = ShowActual(uid, type);
        return Instance.ShowTask.Task;
    }
    
    private async Task ShowActual(Guid uid, string type)
    {
        try
        {  
            var response = await HttpHelper.Get<AuditEntry[]>($"/api/audit/{type}/{uid}");
            if (response.Success == false)
            {
                ShowTask.SetResult();
                return;
            }

            if (response.Data?.Any() != true)
            {
                ShowTask.SetResult();
                Toast.ShowWarning(Translater.Instant("Labels.NoAuditHistoryAvailable"));
                return;
            }

            if (response.Data.First().Parameters.TryGetValue("Name", out object oName))
                this.Title = oName.ToString();

            foreach (var d in response.Data)
            {
                d.Parameters ??= new();
                if(string.IsNullOrEmpty(d.ObjectType) == false)
                    d.Parameters["Type"] = d.ObjectType[(d.ObjectType.LastIndexOf(".", StringComparison.Ordinal) + 1)..].Humanize();
                d.Parameters["User"] = d.OperatorName;
                d.Summary = Translater.Instant($"AuditActions.{d.Action}", d.Parameters);
            }
            
            Data = response.Data.ToList();
            this.Visible = true;
            this.StateHasChanged();
            await AwaitRender();
            this.StateHasChanged();
        }
        finally
        {
            Blocker.Hide();
        }
    }
    protected bool Maximised { get; set; }
    protected void OnMaximised(bool maximised)
    {
        this.Maximised = maximised;
    }
    
}
