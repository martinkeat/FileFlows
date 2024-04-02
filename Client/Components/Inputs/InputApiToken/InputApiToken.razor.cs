using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Inputs;

/// <summary>
/// API Token input
/// </summary>
public partial class InputApiToken
{
    /// <summary>
    /// Gets or sets the clipboard service
    /// </summary>
    [Inject] private IClipboardService ClipboardService { get; set; }
    
    /// <summary>
    /// Refreshes a new API token
    /// </summary>
    private void Refresh()
    {
        Value = Guid.NewGuid().ToString("N");
    }

    /// <summary>
    /// Copies to the clipboard
    /// </summary>
    private async Task Copy()
    {
        if (string.IsNullOrWhiteSpace(Value) == false)
            await ClipboardService.CopyToClipboard(Value);
    }
}