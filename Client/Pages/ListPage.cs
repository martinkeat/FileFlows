using System.Threading;
using FileFlows.Client.Components;
using FileFlows.Client.Components.Dialogs;
using Microsoft.AspNetCore.Components;
using FileFlows.Client.Components.Common;
using Humanizer;

namespace FileFlows.Client.Pages;

public abstract class ListPage<U, T> : ComponentBase where T : IUniqueObject<U>
{
    protected FlowTable<T> Table { get; set; }
    [CascadingParameter] public Blocker Blocker { get; set; }
    [CascadingParameter] public Editor Editor { get; set; }
    public string lblAdd, lblEdit, lblDelete, lblDeleting, lblRefresh;
    

    public abstract string ApiUrl { get; }
    private bool _needsRendering = false;

    protected bool Loaded { get; set; }
    protected bool HasData { get; set; }

    public List<T> _Data = new List<T>();
    public List<T> Data
    {
        get => _Data;
        set
        {
            _Data = value ?? new List<T>();
        }
    }

    protected override void OnInitialized() => OnInitialized(true);

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

    public virtual async Task Refresh() => await Load(default);

    public virtual string FetchUrl => ApiUrl;

    public async virtual Task PostLoad()
    {
        await Task.CompletedTask;
    }
    protected async Task WaitForRender()
    {
        _needsRendering = true;
        StateHasChanged();
        while (_needsRendering)
        {
            await Task.Delay(50);
        }
    }
    protected override void OnAfterRender(bool firstRender)
    {
        _needsRendering = false;
    }

    private SemaphoreSlim fetching = new(1);

    /// <summary>
    /// Sets the table data, virtual so a filter can be set if needed
    /// </summary>
    /// <param name="data">the data to set</param>
    protected virtual void SetTableData(List<T> data) => Table.SetData(data);

    public virtual async Task Load(U selectedUid)
    {
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
                    var item = this.Data.Where(x => x.Uid.Equals(selectedUid)).FirstOrDefault();
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
        var items = Table.GetSelected();
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


    public async Task Enable(bool enabled, T item)
    {
#if (DEMO)
        return;
#else
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
#endif
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
#if (!DEMO)
            var deleteResult = await HttpHelper.Delete(DeleteUrl, new ReferenceModel<U> { Uids = uids });
            if (deleteResult.Success == false)
            {
                if(Translater.NeedsTranslating(deleteResult.Body))
                    Toast.ShowError( Translater.Instant(deleteResult.Body));
                else
                    Toast.ShowError( Translater.Instant("ErrorMessages.DeleteFailed"));
                return;
            }
#endif

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
    /// Humanizes a date, eg 11 hours ago
    /// </summary>
    /// <param name="date">the date</param>
    /// <returns>the humanized date</returns>
    protected string DateString(DateTime? date)
    {
        if (date == null) return string.Empty;
        if (date.Value.Year < 2020) return string.Empty; // fixes 0000-01-01 issue
        var localDate = new DateTime(date.Value.Year, date.Value.Month, date.Value.Day, date.Value.Hour,
            date.Value.Minute, date.Value.Second);
        return localDate.ToUniversalTime().Humanize();
    }

}