using System.Text.RegularExpressions;
using FileFlows.Client.Components.Inputs;
using FileFlows.Plugin;
using FileFlows.ScriptExecution;
using FileFlows.Shared.Validators;
using Logger = FileFlows.Shared.Logger;

namespace FileFlows.Client.Components.ScriptEditor;

/// <summary>
/// An editor used to edit scripts 
/// </summary>
public class ScriptEditor
{
    /// <summary>
    /// The editor instance
    /// </summary>
    private readonly Editor Editor;
    
    /// <summary>
    /// The script importer
    /// </summary>
    private readonly Dialogs.ImportScript ScriptImporter;
    
    /// <summary>
    /// The callback to call when the script editor is saved
    /// </summary>
    private readonly Editor.SaveDelegate SaveCallback;

    /// <summary>
    /// The blocker
    /// </summary>
    private readonly Blocker Blocker;

    public ScriptEditor(Editor editor, Dialogs.ImportScript scriptImporter, Editor.SaveDelegate saveCallback = null, Blocker blocker = null)
    {
        this.Editor = editor;
        this.ScriptImporter = scriptImporter;
        this.SaveCallback = saveCallback;
        this.Blocker = blocker;
    }
    
    
    /// <summary>
    /// Opens the script editor to edit a specific script
    /// </summary>
    /// <param name="item">the script to edit</param>
    /// <returns>true if the script was saved, otherwise false</returns>
    public async Task<bool> Open(Script item)
    {
        List<ElementField> fields = new List<ElementField>();
        bool flowScript = item.Type == ScriptType.Flow;

        if (string.IsNullOrEmpty(item.Code))
        {
            item.Code = flowScript ? @"
/**
 * @description The description of this script
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
        else
        {
            item.Code = ScriptParser.GetCodeWithCommentBlock(item, true);
        }

        item.Code = item.Code.Replace("\r\n", "\n").Trim();

        bool readOnly = item.Repository;
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
            Validators = item.Type == ScriptType.Flow ? new List<Validator>
            {
                new ScriptValidator()
            } : new List<Validator>()
        });

        var result = await Editor.Open(new()
        {
            TypeName = "Pages.Script", Title = title, Fields = fields, Model = item, Large = true, ReadOnly = readOnly,
            SaveCallback = SaveCallback ?? Save, HelpUrl = "https://fileflows.com/docs/webconsole/extensions/scripts",
            AdditionalButtons = readOnly ? null : new ActionButton[]
            {
                new ()
                {
                    Label = "Labels.Import", 
                    Clicked = (sender, e) => _ = OpenImport(sender, e)
                }
            }
        });

        return result.Success;
    }
    
    
    private List<Script> _Shared;

    /// <summary>
    /// Gets the shared scripts
    /// </summary>
    /// <returns>the shared scripts</returns>
    private async Task<List<Script>> GetShared()
    {
        if (_Shared == null)
        {
            var result = await HttpHelper.Get<List<Script>>("/api/script/list/Shared");
            if (result.Success)
                this._Shared = result.Data;
        }

        return _Shared ?? new ();
    }
    
    /// <summary>
    /// Opens the import script dialog
    /// </summary>
    /// <param name="sender">the sender</param>
    /// <param name="e">the event arguments</param>
    private async Task OpenImport(object sender, EventArgs e)
    {
        if (sender is Editor editor == false)
            return;

        var codeInput = editor.FindInput<InputCode>("Code");
        if (codeInput == null)
            return;

        string code = await codeInput.GetCode() ?? string.Empty;
        var shared = await GetShared();
        var available = shared.Where(x => code.IndexOf("Shared/" + x.Name, StringComparison.Ordinal) < 0).Select(x => x.Name).ToList();
        if (available.Any() == false)
        {
            Toast.ShowWarning("Dialogs.ImportScript.Messages.NoMoreImports");
            return;
        }

        List<string> import = await ScriptImporter.Show(available);
        Logger.Instance.ILog("Import", import);
        await codeInput.AddImports(import);
    }
    
    

    async Task<bool> Save(ExpandoObject model)
    {
        Blocker?.Show();

        try
        {
            var saveResult = await HttpHelper.Post<Script>($"/api/script", model);
            if (saveResult.Success == false)
            {
                Toast.ShowEditorError(saveResult.Body?.EmptyAsNull() ?? Translater.Instant("ErrorMessages.SaveFailed"));
                return false;
            }

            return true;
        }
        finally
        {
            Blocker?.Hide();
        }
    }
}