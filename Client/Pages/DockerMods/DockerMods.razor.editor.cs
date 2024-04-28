using FileFlows.Client.Components;
using FileFlows.Client.Components.Inputs;
using FileFlows.Plugin;

namespace FileFlows.Client.Pages;

/// <summary>
/// Editor for the DockerMod
/// </summary>
public partial class DockerMods
{
    
    private async Task<bool> OpenEditor(DockerMod item)
    {
        var fields = new List<ElementField>();
        fields.Add(new ()
        {
            Name = nameof(item.Name),
            InputType = FormInputType.Text
        });
        fields.Add(new ()
        {
            Name = nameof(item.Description),
            InputType = FormInputType.TextArea,
            Parameters = new ()
            {
                { nameof(InputTextArea.Rows), 3}
            }
        });
        fields.Add(new ()
        {
            Name = nameof(item.Icon),
            InputType = FormInputType.IconPicker
        });
        fields.Add(new ()
        {
            InputType = FormInputType.HorizontalRule
        });
        fields.Add(new ()
        {
            Name = nameof(item.Code),
            InputType = FormInputType.Code,
            Parameters = new ()
            {
                { nameof(InputCode.Language), "shell" }
            }
        });
        
        var result = await Editor.Open(new()
        {
            TypeName = "Pages.DockerMod", Title = "Pages.DockerMod.Title", Model = item, SaveCallback = Save, Fields = fields,
            HelpUrl = "https://fileflows.com/docs/webconsole/dockermods"
        });
        return true;
    }

    async Task<bool> Save(ExpandoObject model)
    {
        Blocker.Show();
        this.StateHasChanged();

        try
        {
            var saveResult = await HttpHelper.Post<DockerMod>($"{ApiUrl}", model);
            if (saveResult.Success == false)
            {
                Toast.ShowError( Translater.TranslateIfNeeded(saveResult.Body?.EmptyAsNull() ?? "ErrorMessages.SaveFailed"));
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
    }
}