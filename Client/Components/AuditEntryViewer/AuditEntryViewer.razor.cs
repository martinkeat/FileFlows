using FileFlows.Client.Components.Common;
using Humanizer;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components;

/// <summary>
/// Audit entry viewer
/// </summary>
public partial class AuditEntryViewer
{
    /// <summary>
    /// Gets the static instance of the audit entry viewrt
    /// </summary>
    public static AuditEntryViewer Instance { get;private set; }
    
    /// <summary>
    /// Gets or sets the blocker tho show
    /// </summary>
    [CascadingParameter] public Blocker Blocker { get; set; }
    
    TaskCompletionSource ShowTask;
    /// <summary>
    /// The entry to render
    /// </summary>
    private AuditEntry Entry;
    /// <summary>
    /// The title of the viewer
    /// </summary>
    private string Title;
    /// <summary>
    /// If the viewer is visible or not
    /// </summary>
    private bool Visible;
    /// <summary>
    /// The close label
    /// </summary>
    private string lblClose;
    /// <summary>
    /// If the component is waiting a render
    /// </summary>
    private bool AwaitingRender = false;
    /// <summary>
    /// The table instance
    /// </summary>
    private FlowTable<EntryViewerData> Table { get; set; }
    /// <summary>
    /// The table data
    /// </summary>
    private List<EntryViewerData> Data = new();
    
    /// <summary>
    /// The label to show in the value column
    /// </summary>
    private string lblValue;

    /// <summary>
    /// Constructs a new instance of the Audit entry viewer component
    /// </summary>
    public AuditEntryViewer()
    {
        Instance = this;
    }

    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        lblClose = Translater.Instant("Labels.Close");
        lblValue = Translater.Instant("Label.Value");
    }
    
    /// <summary>
    /// Closes the viewer
    /// </summary>
    private void Close()
    {
        this.Visible = false;
        Entry = null;
        this.ShowTask.SetResult();
    }

    /// <summary>
    /// Shows the audit entry changes
    /// </summary>
    /// <param name="entry">the entry to view</param>
    /// <returns>a task to await</returns>
    public Task Show(AuditEntry entry)
    {
        if (entry.Changes?.Any() != true)
        {
            Toast.ShowWarning("Labels.NoChangedDetected");
            return Task.CompletedTask;
        }
        
        this.Entry = entry;
        Title = Translater.Instant("Labels.Audit");
        Instance.ShowTask = new ();
        Visible = true;
        _ = ShowActual(entry);
        
        return Instance.ShowTask.Task;
    }

    /// <summary>
    /// Performs the actual show
    /// </summary>
    /// <param name="entry">the entry to view</param>
    private async Task ShowActual(AuditEntry entry)
    {
        Data = entry.Changes.Select(x => new EntryViewerData()
        {
            Name = x.Key,
            Value = x.Value
        }).ToList();

        await AwaitRender();
        
        Table?.SetData(Data);
        await AwaitRender();
    }


    /// <summary>
    /// Waits for the component to re-render
    /// </summary>
    private async Task AwaitRender()
    {
        AwaitingRender = true;
        this.StateHasChanged();
        await Task.Delay(10);
        while (AwaitingRender)
            await Task.Delay(10);
    }

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        if (AwaitingRender)
            AwaitingRender = false;
    }
    
    /// <summary>
    /// Gets or sets if this is maximised
    /// </summary>
    protected bool Maximised { get; set; }
    /// <summary>
    /// Maximises the viewer
    /// </summary>
    /// <param name="maximised">true to maximise otherwise false to return to normal</param>
    protected void OnMaximised(bool maximised)
    {
        this.Maximised = maximised;
    }

    /// <summary>
    /// Entry viewer data
    /// </summary>
    private class EntryViewerData
    {
        /// <summary>
        /// Gets the name of the value
        /// </summary>
        public string Name { get; init; }
        /// <summary>
        /// Gets the value
        /// </summary>
        public object Value { get; init; }
    }
}
