using FileFlows.Client.Components;
using FileFlows.Client.Components.Inputs;
using FileFlows.Client.Components.ScriptEditor;
using FileFlows.Plugin;
using FileFlows.Shared.Validators;

namespace FileFlows.Client.Pages;

public partial class Scripts
{
    /// <summary>
    /// The script importer
    /// </summary>
    private Components.Dialogs.ImportScript ScriptImporter;

    /// <summary>
    /// Editor for a Script
    /// </summary>
    /// <param name="item">the script to edit</param>
    /// <returns>the result of the edit</returns>
    public override async Task<bool> Edit(Script item)
    {
        this.EditingItem = item;
        
        var editor = new ScriptEditor(Editor, ScriptImporter, saveCallback: Save);
        await editor.Open(item);

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