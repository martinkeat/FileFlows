using System.Threading;
using FileFlows.Client.Components;
using FileFlows.Client.Components.Common;
using Humanizer;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Pages;

/// <summary>
/// Page for access control 
/// </summary>
public partial class Audit : ComponentBase
{
    private SemaphoreSlim fetching = new(1);
    /// <summary>
    /// Gets or sets the table instance
    /// </summary>
    protected FlowTable<AuditEntry> Table { get; set; }
    /// <summary>
    /// Gets or sets the navigation manager
    /// </summary>
    [Inject] public NavigationManager NavigationManager { get; set; }
    /// <summary>
    /// Gets or sets the blocker
    /// </summary>
    [CascadingParameter] public Blocker Blocker { get; set; }

    /// <summary>
    /// Gets or sets the profile service
    /// </summary>
    [Inject] protected ProfileService ProfileService { get; set; }
    
    /// <summary>
    /// Gets the profile
    /// </summary>
    protected Profile Profile { get; private set; }

    /// <summary>
    /// The data shown
    /// </summary>
    List<AuditEntry> Data = new List<AuditEntry>();
    private bool _needsRendering = false;

    /// <summary>
    /// The search filter
    /// </summary>
    private AuditSearchFilter Filter = new();
    
    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        Profile = await ProfileService.Get();
        if (Profile.LicensedFor(LicenseFlags.Auditing) == false)
        {
            NavigationManager.NavigateTo("/");
            return;
        }
        _ = Load();
    }
    
    /// <summary>
    /// Waits for a render to occur
    /// </summary>
    async Task WaitForRender()
    {
        _needsRendering = true;
        StateHasChanged();
        while (_needsRendering)
        {
            await Task.Delay(50);
        }
    }
    
    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        _needsRendering = false;
    }


    public virtual async Task Load()
    {
        Blocker.Show("Loading Data");
        await this.WaitForRender();
        try
        {
            await fetching.WaitAsync();
            var result = await HttpHelper.Post<List<AuditEntry>>("/api/audit", Filter);
            if (result.Success)
            {
                foreach (var d in result.Data)
                {
                    d.Parameters ??= new();
                    if(string.IsNullOrEmpty(d.ObjectType) == false)
                        d.Parameters["Type"] = d.ObjectType[(d.ObjectType.LastIndexOf(".", StringComparison.Ordinal) + 1)..].Humanize();
                    d.Parameters["User"] = d.OperatorName;
                    d.Summary = Translater.Instant($"AuditActions.{d.Action}", d.Parameters);
                }
                this.Data = result.Data;
                if (Table != null)
                    SetTableData(this.Data);
            }
        }
        finally
        {
            fetching.Release();
            Blocker.Hide();
            await this.WaitForRender();
        }
    }
    
    /// <summary>
    /// Sets the table data, virtual so a filter can be set if needed
    /// </summary>
    /// <param name="data">the data to set</param>
    protected virtual void SetTableData(List<AuditEntry> data) => Table?.SetData(data, clearSelected: false);

}