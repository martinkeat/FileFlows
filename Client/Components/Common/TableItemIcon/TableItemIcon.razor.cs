using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Common;

/// <summary>
/// Icon that displayed in a table-item row
/// </summary>
public partial class TableItemIcon : ComponentBase
{
    /// <summary>
    /// Gets or sets the icon
    /// </summary> 
    [Parameter] public string Icon { get; set; }
    
    /// <summary>
    /// Gets or sets the default icon to show if there is no icon
    /// </summary>
    [Parameter] public string DefaultIcon { get; set; }
}