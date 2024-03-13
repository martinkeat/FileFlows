using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Common;

/// <summary>
/// Component that shows a bar progress
/// </summary>
public partial class FlowSavings
{
    [Parameter]
    public long FinalSize { get; set; }
    
    [Parameter]
    public long OriginalSize { get; set; }
}