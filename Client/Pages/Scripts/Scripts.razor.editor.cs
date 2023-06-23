using FileFlows.Client.Components;
using FileFlows.Client.Components.Inputs;
using FileFlows.Plugin;
using FileFlows.Shared.Validators;

namespace FileFlows.Client.Pages;

public partial class Scripts
{
    private FileFlows.Client.Components.Dialogs.ImportScript ScriptImporter;

    /// <summary>
    /// Editor for a Script
    /// </summary>
    /// <param name="item">the script to edit</param>
    /// <returns>the result of the edit</returns>
    public override async Task<bool> Edit(Script item)
    {
        this.EditingItem = item;

        List<ElementField> fields = new List<ElementField>();
        bool flowScript = item.Type == ScriptType.Flow;

        if (string.IsNullOrEmpty(item.Code))
        {
            item.Code = flowScript ? @"
/**
 * Description of this script
 * @param {int} NumberParameter Description of this input
 * @output Description of output 1
 * @output Description of output 2
 */
function Script(NumberParameter)
{
    return 1;
}
" : @"
import { FileFlowsApi } from 'Shared/FileFlowsApi';

let ffApi = new FileFlowsApi();
";
        }

        item.Code = item.Code.Replace("\r\n", "\n").Trim();

        bool readOnly = item.Repository || item.Type == ScriptType.Shared;
        string title = "Pages.Script.Title";

        if (readOnly)
        {
            title = Translater.Instant("Pages.Script.Title") + ": " + item.Name;
        }
        else
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.Text,
                Name = nameof(item.Name),
                Validators = flowScript ? new List<FileFlows.Shared.Validators.Validator>
                {
                    new FileFlows.Shared.Validators.Required()
                } : new ()
            });
        }


        fields.Add(new ElementField
        {
            InputType = FormInputType.Code,
            Name = "Code",
            Validators = item.Type == ScriptType.Flow ? new List<FileFlows.Shared.Validators.Validator>
            {
                new FileFlows.Shared.Validators.ScriptValidator()
            } : new List<Validator>()
        });

        var result = await Editor.Open(new()
        {
            TypeName = "Pages.Script", Title = title, Fields = fields, Model = item, Large = true, ReadOnly = readOnly,
            SaveCallback = Save, HelpUrl = "https://fileflows.com/docs/pages/scripts",
            AdditionalButtons = new ActionButton[]
            {
                new ()
                {
                    Label = "Labels.Import", 
                    Clicked = (sender, e) => OpenImport(sender, e)
                }
            }
        });

        return false;
    }

    private async Task OpenImport(object sender, EventArgs e)
    {
        if (sender is Editor editor == false)
            return;

        var codeInput = editor.FindInput<InputCode>("Code");
        if (codeInput == null)
            return;

        string code = await codeInput.GetCode() ?? string.Empty;
        var available = DataShared.Where(x => code.IndexOf("Shared/" + x.Name) < 0).Select(x => x.Name).ToList();
        if (available.Any() == false)
        {
            Toast.ShowWarning("Dialogs.ImportScript.Messages.NoMoreImports");
            return;
        }

        Logger.Instance.ILog("open import!");
        List<string> import = await ScriptImporter.Show(available);
        Logger.Instance.ILog("Import", import);
        codeInput.AddImports(import);
    }
}