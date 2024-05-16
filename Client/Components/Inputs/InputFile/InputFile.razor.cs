using System.Collections.Generic;
using System.Threading.Tasks;
using FileFlows.Client.Components.Dialogs;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Inputs;

public partial class InputFile : Input<string>
{
    [Parameter]
    public string[] Extensions { get; set; }

    [Parameter]
    public bool Directory { get; set; }

    [Parameter] public Dictionary<string, object> Variables { get; set; } = new();
    public override bool Focus() => FocusUid();
    async Task Browse()
    {
        string result = await FileBrowser.Show(this.Value, directory: Directory, extensions: Extensions);
        if (string.IsNullOrEmpty(result))
            return;
        this.ClearError();
        this.Value = result;
    }
}
