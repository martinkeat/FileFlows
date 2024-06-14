using FileFlows.Client.Components;
using FileFlows.Client.Components.Dialogs;
using FileFlows.Client.Shared;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Pages;

public partial class Libraries : ListPage<Guid, Library>
{
    public override string ApiUrl => "/api/library";

    private Library EditingItem = null;

    private string lblLastScanned, lblFlow, lblSavings;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        lblLastScanned = Translater.Instant("Labels.LastScanned");
        lblFlow = Translater.Instant("Labels.Flow");
        lblSavings = Translater.Instant("Labels.Savings");
    }

    private async Task Add()
    {
        await Edit(new ()
        {  
            Enabled = true, 
            ScanInterval = 60, 
            FileSizeDetectionInterval = 5,
            UseFingerprinting = false,
            UpdateMovedFiles = true,
            Schedule = new string('1', 672)
        });
    }

    private Task<RequestResult<Dictionary<Guid, string>>> GetFlows()
        => HttpHelper.Get<Dictionary<Guid, string>>("/api/flow/basic-list?type=Standard");

    private Dictionary<string, StorageSavedData> StorageSaved = new ();

    /// <inheritdoc />
    protected override async Task<RequestResult<List<Library>>> FetchData()
    {
        StorageSaved =
            (await HttpHelper.Get<List<StorageSavedData>>("/api/statistics/storage-saved-raw"))
            .Data?.ToDictionary(x => x.Library, x => x) ?? new ();
        return await base.FetchData();
    }

    public override async Task<bool> Edit(Library library)
    {
        this.EditingItem = library;
        return await OpenEditor(library);
    }

    private void TemplateValueChanged(object sender, object value) 
    {
        if (value == null)
            return;
        var template = value as Library;
        if (template == null)
            return;
        var editor = sender as Editor;
        if (editor == null)
            return;
        if (editor.Model == null)
            editor.Model = new ExpandoObject();
        IDictionary<string, object> model = editor.Model!;
        
        SetModelProperty(nameof(template.Name), template.Name);
        SetModelProperty(nameof(template.Template), template.Name);
        SetModelProperty(nameof(template.FileSizeDetectionInterval), template.FileSizeDetectionInterval);
        SetModelProperty(nameof(template.Filter), template.Filter);
        SetModelProperty(nameof(template.Extensions), template.Extensions?.ToArray() ?? new string[] { });
        SetModelProperty(nameof(template.UseFingerprinting), template.UseFingerprinting);
        SetModelProperty(nameof(template.ExclusionFilter), template.ExclusionFilter);
        SetModelProperty(nameof(template.Path), template.Path);
        SetModelProperty(nameof(template.Priority), template.Priority);
        SetModelProperty(nameof(template.ScanInterval), template.ScanInterval);
        SetModelProperty(nameof(template.ReprocessRecreatedFiles), template.ReprocessRecreatedFiles);
        SetModelProperty(nameof(Library.Folders), false);

        editor.TriggerStateHasChanged();
        void SetModelProperty(string property, object value)
        {
            model[property] = value;
        }
    }

    async Task<bool> Save(ExpandoObject model)
    {
        Blocker.Show();
        this.StateHasChanged();

        try
        {
            var saveResult = await HttpHelper.Post<Library>($"{ApiUrl}", model);
            if (saveResult.Success == false)
            {
                Toast.ShowEditorError( Translater.TranslateIfNeeded(saveResult.Body?.EmptyAsNull() ?? "ErrorMessages.SaveFailed"));
                return false;
            }
            if ((Profile.ConfigurationStatus & ConfigurationStatus.Libraries) !=
                ConfigurationStatus.Libraries)
            {
                // refresh the app configuration status
                await ProfileService.Refresh();
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
    }


    private string TimeSpanToString(Library lib)
    {
        if (lib.LastScanned.Year < 2001)
            return Translater.Instant("Times.Never");

        if (lib.LastScannedAgo.TotalMinutes < 1)
            return Translater.Instant("Times.SecondsAgo", new { num = (int)lib.LastScannedAgo.TotalSeconds });
        if (lib.LastScannedAgo.TotalHours < 1 && lib.LastScannedAgo.TotalMinutes < 120)
            return Translater.Instant("Times.MinutesAgo", new { num = (int)lib.LastScannedAgo.TotalMinutes });
        if (lib.LastScannedAgo.TotalDays < 1)
            return Translater.Instant("Times.HoursAgo", new { num = (int)Math.Round(lib.LastScannedAgo.TotalHours) });
        else
            return Translater.Instant("Times.DaysAgo", new { num = (int)lib.LastScannedAgo.TotalDays });
    }

    private async Task Rescan()
    {
        var uids = Table.GetSelected()?.Select(x => x.Uid)?.ToArray() ?? new System.Guid[] { };
        if (uids.Length == 0)
            return; // nothing to rescan

        Blocker.Show();
        this.StateHasChanged();

        try
        {
            var result = await HttpHelper.Put($"{ApiUrl}/rescan", new ReferenceModel<Guid> { Uids = uids });
            if (result.Success == false)
                return;
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
    }

    /// <summary>
    /// Reprocess all files in a library
    /// </summary>
    private async Task Reprocess()
    {
        var uids = Table.GetSelected()?.Select(x => x.Uid)?.ToArray() ?? new System.Guid[] { };
        if (uids.Length == 0)
            return; // nothing to rescan

        if (await Confirm.Show("Pages.Libraries.Messages.Reprocess.Title",
                "Pages.Libraries.Messages.Reprocess.Message", defaultValue: false) == false)
            return;

        Blocker.Show();
        this.StateHasChanged();

        try
        {
            var result = await HttpHelper.Put($"{ApiUrl}/reprocess", new ReferenceModel<Guid> { Uids = uids });
            if (result.Success == false)
                return;
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
    }

    public override async Task Delete()
    {
        var uids = Table.GetSelected()?.Select(x => x.Uid)?.ToArray() ?? new System.Guid[] { };
        if (uids.Length == 0)
            return; // nothing to delete
        var confirmResult = await Confirm.Show("Labels.Delete",
            Translater.Instant("Pages.Libraries.Messages.DeleteConfirm", new { count = uids.Length })
        );
        if (confirmResult == false)
            return; // rejected the confirm

        Blocker.Show();
        this.StateHasChanged();

        try
        {
            var deleteResult = await HttpHelper.Delete(ApiUrl, new ReferenceModel<Guid> { Uids = uids });
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

    /// <summary>
    /// Get priority icon
    /// </summary>
    /// <param name="library">the library</param>
    /// <returns>the priority icon class</returns>
    private string GetPriorityIcon(Library library)
    {
        switch (library.Priority)
        {
            case ProcessingPriority.Highest:
                return "fas fa-angle-double-up";
            case ProcessingPriority.High:
                return "fas fa-angle-up";
            case ProcessingPriority.Low:
                return "fas fa-angle-down";
            case ProcessingPriority.Lowest:
                return "fas fa-angle-double-down";
            default:
                return "fas fa-folder";
        }
    }

    /// <summary>
    /// Gets the storage saved
    /// </summary>
    /// <param name="libraryName">the name of the library</param>
    /// <param name="storageSavedData">the savings if found</param>
    /// <returns>if the storage savings were in the dictionary</returns>
    private bool GetStorageSaved(string libraryName, out StorageSavedData storageSavedData)
        => StorageSaved.TryGetValue(libraryName, out storageSavedData);
    
    
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
    /// Opens the flow in the editor
    /// </summary>
    /// <param name="flowUid">the UID of the flow</param>
    private void OpenFlow(Guid? flowUid)
    {
        if (flowUid == null || Profile.HasRole(UserRole.Flows) == false)
            return;

        NavigationManager.NavigateTo($"/flows/{flowUid}");
    }
}

