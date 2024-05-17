using FileFlows.Client.Components;

namespace FileFlows.Client.Pages;

public partial class Variables : ListPage<Guid, Variable>
{
    public override string ApiUrl => "/api/variable";

    private Variable EditingItem = null;

    private async Task Add()
    {
#if (!DEMO)
        await Edit(new Variable());
#endif
    }


    public override async Task<bool> Edit(Variable variable)
    {
#if (!DEMO)
        this.EditingItem = variable;
        List<ElementField> fields = new List<ElementField>();
        fields.Add(new ElementField
        {
            InputType = FileFlows.Plugin.FormInputType.Text,
            Name = nameof(variable.Name),
            HideLabel = true,
            Validators = new List<FileFlows.Shared.Validators.Validator> {
                new FileFlows.Shared.Validators.Required()
            },
            
        });
        fields.Add(new ElementField
        {
            InputType = FileFlows.Plugin.FormInputType.TextArea,
            FlexGrow = true,
            HideLabel = true,
            Name = nameof(variable.Value),
            Validators = new List<FileFlows.Shared.Validators.Validator> {
                new FileFlows.Shared.Validators.Required()
            }
        });
        var result = await Editor.Open(new () { TypeName = "Pages.Variable", Title = "Pages.Variable.Title", 
            Fields = fields, Model = variable, SaveCallback = Save,
            FullWidth = true
        });
#endif
        return false;
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
            var saveResult = await HttpHelper.Post<Variable>($"{ApiUrl}", model);
            if (saveResult.Success == false)
            {
                Toast.ShowEditorError( saveResult.Body?.EmptyAsNull() ?? Translater.Instant("ErrorMessages.SaveFailed"));
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
#endif
    }

}