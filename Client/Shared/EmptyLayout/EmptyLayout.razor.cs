using FileFlows.Client.Components;

namespace FileFlows.Client.Shared;

/// <summary>
/// An empty layout
/// </summary>
public partial class EmptyLayout
{
    /// <summary>
    /// Gets or sets the blocker
    /// </summary>
    public Blocker Blocker { get; set; }
    
    /// <inheritdoc />
    protected override void OnInitialized()
    {
        HttpHelper.On401 = null;
    }
}