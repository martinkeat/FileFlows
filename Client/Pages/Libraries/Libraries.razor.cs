using FileFlows.Client.Components;
using FileFlows.Client.Components.Dialogs;

namespace FileFlows.Client.Pages;

public partial class Libraries : ListPage<Guid, Library>
{
    public override string ApiUrl => "/api/library";

    private Library EditingItem = null;

    private async Task Add()
    {
        await Edit(new ()
        {
            Enabled = true, 
            ScanInterval = 60, 
            FileSizeDetectionInterval = 5,
            UseFingerprinting = true,
            UpdateMovedFiles = true,
            Schedule = new String('1', 672)
        });
    }
#if (DEMO)
    public override async Task Load(Guid? selectedUid = null)
    {
        this.Data = Enumerable.Range(1, 5).Select(x => new Library
        {
            Enabled = true,
            Flow = new ObjectReference
            {
                Name = "Flow",
                Uid = Guid.NewGuid()
            },
            Path = "/media/library" + x,                
            Name = "Demo Library " + x,
            Uid = Guid.NewGuid(),
            Filter = "\\.(mkv|mp4|avi|mov|divx)$"
        }).ToList();
    }
#endif

    private async Task<RequestResult<FileFlows.Shared.Models.Flow[]>> GetFlows()
    {
#if (DEMO)
        var results = Enumerable.Range(1, 5).Select(x => new FileFlows.Shared.Models.Flow { Name = "Flow " + x, Uid = System.Guid.NewGuid() }).ToArray();
        return new RequestResult<FileFlows.Shared.Models.Flow[]> { Success = true, Data = results };
#endif
        return await HttpHelper.Get<FileFlows.Shared.Models.Flow[]>("/api/flow");
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
        Library template = value as Library;
        if (template == null)
            return;
        Editor editor = sender as Editor;
        if (editor == null)
            return;
        if (editor.Model == null)
            editor.Model = new ExpandoObject();
        IDictionary<string, object> model = editor.Model;
        
        SetModelProperty(nameof(template.Name), template.Name);
        SetModelProperty(nameof(template.Template), template.Name);
        SetModelProperty(nameof(template.FileSizeDetectionInterval), template.FileSizeDetectionInterval);
        SetModelProperty(nameof(template.Filter), template.Filter);
        SetModelProperty(nameof(template.ExclusionFilter), template.ExclusionFilter);
        SetModelProperty(nameof(template.Path), template.Path);
        SetModelProperty(nameof(template.Priority), template.Priority);
        SetModelProperty(nameof(template.ScanInterval), template.ScanInterval);
        SetModelProperty(nameof(template.ReprocessRecreatedFiles), template.ReprocessRecreatedFiles);
        SetModelProperty(nameof(Library.Folders), false);

        void SetModelProperty(string property, object value)
        {
            if(model.ContainsKey(property))
                model[property] = value;
            else
                model.Add(property, value);
        }
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
            var saveResult = await HttpHelper.Post<Library>($"{ApiUrl}", model);
            if (saveResult.Success == false)
            {
                Toast.ShowError( Translater.TranslateIfNeeded(saveResult.Body?.EmptyAsNull() ?? "ErrorMessages.SaveFailed"));
                return false;
            }
            if ((App.Instance.FileFlowsSystem.ConfigurationStatus & ConfigurationStatus.Libraries) !=
                ConfigurationStatus.Libraries)
            {
                // refresh the app configuration status
                await App.Instance.LoadAppInfo();
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
#if (!DEMO)
            var deleteResult = await HttpHelper.Put($"{ApiUrl}/rescan", new ReferenceModel<Guid> { Uids = uids });
            if (deleteResult.Success == false)
                return;
#endif
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
    }

    public override async Task Delete()
    {
#if (!DEMO)
        var uids = Table.GetSelected()?.Select(x => x.Uid)?.ToArray() ?? new System.Guid[] { };
        if (uids.Length == 0)
            return; // nothing to delete
        var confirmResult = await Confirm.Show("Labels.Delete",
            Translater.Instant("Pages.Libraries.Messages.DeleteConfirm", new { count = uids.Length }),
            "Pages.Libraries.Messages.KeepLibraryFiles",
            false
        );
        if (confirmResult.Confirmed == false)
            return; // rejected the confirm

        Blocker.Show();
        this.StateHasChanged();

        try
        {
            var deleteResult = await HttpHelper.Delete($"{ApiUrl}?deleteLibraryFiles={(confirmResult.SwitchState == false)}", new ReferenceModel<Guid> { Uids = uids });
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
#endif
    }
}

