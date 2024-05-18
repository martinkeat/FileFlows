using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Components.Common;

/// <summary>
/// Help button in the FlowTable
/// </summary>
public class FlowTableHelpButton : FlowTableButton
{
    /// <summary>
    /// Gets or sets the JavaScript Runtime
    /// </summary>
    [Inject] IJSRuntime jsRuntime { get; set; }

    /// <summary>
    /// Gets or sets the Help URL to open
    /// </summary>
    [Parameter] public string HelpUrl { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();

        this.Icon = "fas fa-question-circle";
        this.Label = Translater.Instant("Labels.Help");
    }

    /// <summary>
    /// When the button is clicked
    /// </summary>
    /// <returns>a task to await</returns>
    public override async Task OnClick()
    {
        string url = this.HelpUrl;            
        if (string.IsNullOrEmpty(HelpUrl))
            url = "https://fileflows.com/docs";
        else if (url.ToLower().StartsWith("http") == false)
            url = "https://fileflows.com/docs/webconsole/" + url;

        await App.Instance.OpenHelp(url.ToLowerInvariant());
    }
}
