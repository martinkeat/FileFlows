using System.Threading;
using FileFlows.Client.Components;
using FileFlows.Client.Components.Dialogs;
using Microsoft.AspNetCore.Components;
using FileFlows.Client.Components.Common;
using FileFlows.Client.Helpers;

namespace FileFlows.Client.Pages;

public abstract class ListPage<U, T> : ComponentBase where T : IUniqueObject<U>
{
    /// <summary>
    /// Gets or sets the navigation manager
    /// </summary>
    [Inject] public NavigationManager NavigationManager { get; set; }
    /// <summary>
    /// Gets or sets the table instance
    /// </summary>
    protected FlowTable<T> Table { get; set; }
    [CascadingParameter] public Blocker Blocker { get; set; }
    [CascadingParameter] public Editor Editor { get; set; }
    public string lblAdd, lblEdit, lblDelete, lblDeleting, lblRefresh;
    

    public abstract string ApiUrl { get; }
    private bool _needsRendering = false;

    protected bool Loaded { get; set; }
    protected bool HasData { get; set; }

    /// <summary>
    /// Gets or sets the profile service
    /// </summary>
    [Inject] protected ProfileService ProfileService { get; set; }
    
    /// <summary>
    /// Gets the profile
    /// </summary>
    protected Profile Profile { get; private set; }

    public List<T> _Data = new List<T>();
    public List<T> Data
    {
        get => _Data;
        set
        {
            _Data = value ?? new List<T>();
        }
    }

    protected override async Task OnInitializedAsync()
    {
        Profile = await ProfileService.Get();
        if (Licensed() == false)
        {
            NavigationManager.NavigateTo("/");
            return;
        }
        OnInitialized(true);
    }

    protected void OnInitialized(bool load)
    {
        lblAdd = Translater.Instant("Labels.Add");
        lblEdit = Translater.Instant("Labels.Edit");
        lblDelete = Translater.Instant("Labels.Delete");
        lblDeleting = Translater.Instant("Labels.Deleting");
        lblRefresh = Translater.Instant("Labels.Refresh");

        if(load)
            _ = Load(default);
    }

    public virtual async Task Refresh(bool showBlocker = true) => await Load(default, showBlocker);

    public virtual string FetchUrl => ApiUrl;

    public async virtual Task PostLoad()
    {
        await Task.CompletedTask;
    }
    /// <summary>
    /// Waits for a render to occur
    /// </summary>
    protected async Task WaitForRender()
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

    private SemaphoreSlim fetching = new(1);

    /// <summary>
    /// Sets the table data, virtual so a filter can be set if needed
    /// </summary>
    /// <param name="data">the data to set</param>
    protected virtual void SetTableData(List<T> data) => Table?.SetData(data, clearSelected: false);

    public virtual async Task Load(U selectedUid, bool showBlocker = true)
    {
        if(showBlocker)
            Blocker.Show("Loading Data");
        await this.WaitForRender();
        try
        {
            await fetching.WaitAsync();
            var result = await FetchData();
            if (result.Success)
            {
                this.Data = result.Data;
                if (Table != null)
                {
                    SetTableData(this.Data);
                    var item = this.Data.FirstOrDefault(x => x.Uid.Equals(selectedUid));
                    if (item != null)
                    {
                        _ = Task.Run(async () =>
                        {
                            // need a delay here since setdata and the inner works of FlowTable will clear this without it
                            await Task.Delay(50);
                            Table.SelectItem(item);
                        });
                    }
                }
            }
            await PostLoad();
        }
        finally
        {
            fetching.Release();
            HasData = this.Data?.Any() == true;
            this.Loaded = true;
            if(showBlocker)
                Blocker.Hide();
            await this.WaitForRender();
        }
    }

    protected virtual Task<RequestResult<List<T>>> FetchData()
    {
        return HttpHelper.Get<List<T>>(FetchUrl);
    }


    protected async Task OnDoubleClick(T item)
    {
        await Edit(item);
    }


    public async Task Edit()
    {
        var items = Table?.GetSelected();
        if (items?.Any() != true)
            return;
        var selected = items.First();
        if (selected == null)
            return;
        var changed = await Edit(selected);
        if (changed)
            await this.Load(selected.Uid);
    }

    public abstract Task<bool> Edit(T item);

    public void ShowEditHttpError<U>(RequestResult<U> result, string defaultMessage = "ErrorMessage.NotFound")
    {
        Toast.ShowError(
            result.Success || string.IsNullOrEmpty(result.Body) ? Translater.Instant(defaultMessage) : Translater.TranslateIfNeeded(result.Body),
            duration: 60_000
        );
    }
    
    /// <summary>
    /// Tests if the user is licensed for this page
    /// </summary>
    /// <returns>true if they are licensed</returns>
    protected virtual bool Licensed() => true;


    public async Task Enable(bool enabled, T item)
    {
        Blocker.Show();
        this.StateHasChanged();
        Data.Clear();
        try
        {
            await HttpHelper.Put<T>($"{ApiUrl}/state/{item.Uid}?enable={enabled}");
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
    }

    protected virtual string DeleteMessage => "Labels.DeleteItems";
    
    protected virtual string DeleteUrl => ApiUrl;

    public virtual async Task Delete()
    {
        var uids = Table.GetSelected()?.Select(x => x.Uid)?.ToArray() ?? new U[] { };
        if (uids.Length == 0)
            return; // nothing to delete
        if (await Confirm.Show("Labels.Remove",
            Translater.Instant(DeleteMessage, new { count = uids.Length })) == false)
            return; // rejected the confirm

        Blocker.Show();
        this.StateHasChanged();

        try
        {
            var deleteResult = await HttpHelper.Delete(DeleteUrl, new ReferenceModel<U> { Uids = uids });
            if (deleteResult.Success == false)
            {
                if(Translater.NeedsTranslating(deleteResult.Body))
                    Toast.ShowError( Translater.Instant(deleteResult.Body));
                else
                    Toast.ShowError( Translater.Instant("ErrorMessages.DeleteFailed"));
                return;
            }

            this.Data = this.Data.Where(x => uids.Contains(x.Uid) == false).ToList();

            await PostDelete();
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
    }

    protected async virtual Task PostDelete()
    {
        await Task.CompletedTask;
    }

    protected async Task Revisions()
    {
        var items = Table.GetSelected();
        if (items?.Any() != true)
            return;
        var selected = items.First();
        if (selected == null)
            return;
        Guid guid = Guid.Empty;
        if (selected is RevisionedObject ro)
            guid = ro.RevisionUid;
        else if (selected.Uid is Guid sGuid)
            guid = sGuid;
        else
            return;
        
        bool changed = await RevisionExplorer.Instance.Show(guid, "Revisions");
        if (changed)
            await Load(selected.Uid);
    }
    
    /// <summary>
    /// Shows the audit log
    /// </summary>
    protected async Task AuditLog()
    {
        if (Profile.LicensedFor(LicenseFlags.Auditing) == false)
            return;
        
        var selected = Table.GetSelected().FirstOrDefault();
        if (selected == null)
            return;
        if(selected.Uid is Guid uid)
            await AuditHistory.Instance.Show(uid, GetAuditTypeName());
    }

    /// <summary>
    /// Gets the audit type name
    /// </summary>
    /// <returns>the audit type name</returns>
    protected virtual string GetAuditTypeName()
        => typeof(T).FullName;
    
    /// <summary>
    /// Humanizes a date, eg 11 hours ago
    /// </summary>
    /// <param name="dateUtc">the date</param>
    /// <returns>the humanized date</returns>
    protected string DateString(DateTime? dateUtc)
    {
        if (dateUtc == null) return string.Empty;
        if (dateUtc.Value.Year < 2020) return string.Empty; // fixes 0000-01-01 issue
        // var localDate = new DateTime(date.Value.Year, date.Value.Month, date.Value.Day, date.Value.Hour,
        //     date.Value.Minute, date.Value.Second);

        return FormatHelper.HumanizeDate(dateUtc.Value);
    }

}