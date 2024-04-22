using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Common;

/// <summary>
/// Progress bar that shows the progress of a file through a flow
/// </summary>
public partial class FlowProgress : ComponentBase
{
    /// <summary>
    /// Gets or sets the executor info
    /// </summary>
    [Parameter] public FlowExecutorInfo Info { get; set; }
}