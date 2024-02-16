using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace FileFlows.Client.Components.Common;

/// <summary>
/// Represents a collection of tabs.
/// </summary>
public partial class FlowTabs : ComponentBase
{
    /// <summary>
    /// Gets or sets the content of the tabs.
    /// </summary>
    [Parameter] public RenderFragment ChildContent { get; set; }
    public FlowTab ActiveTab { get; internal set; }
    
    /// <summary>
    /// Gets or sets if the title should only be shown on the active tab
    /// </summary>
    [Parameter] public bool TitleOnlyOnActive { get; set; }
    
    /// <summary>
    /// Gets or sets if the tabs should be contained with contain-tabs css
    /// Used by the flow editor to contain the flow parts
    /// </summary>
    [Parameter] public bool ContainTabs { get; set; }
    
    /// <summary>
    /// Gets or sets an event when a tab changes
    /// </summary>
    [Parameter] public EventCallback<int> OnTabChanged { get; set; }
    
    /// <summary>
    /// Gets or sets if the tabs cannot be changed
    /// </summary>
    [Parameter] public bool DisableChanging { get; set; }

    /// <summary>
    /// Represents a collection of tabs.
    /// </summary>
    private List<FlowTab> Tabs = new();

    /// <summary>
    /// Adds a tab to the collection.
    /// </summary>
    /// <param name="tab">The tab to add.</param>
    internal void AddTab(FlowTab tab)
    {
        if (Tabs.Contains(tab) == false)
        {
            Tabs.Add(tab);
            if (ActiveTab == null)
                ActiveTab = tab;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Selects a tab.
    /// </summary>
    /// <param name="tab">The tab to select.</param>
    private void SelectTab(FlowTab tab)
    {
        if (DisableChanging) return;
        ActiveTab = tab;
        OnTabChanged.InvokeAsync(Tabs.IndexOf(tab));
    }

    /// <summary>
    /// Called when the visibility of a tab has changed
    /// </summary>
    internal void TabVisibilityChanged()
        => StateHasChanged();

    /// <inheritdoc/>
    protected override Task OnParametersSetAsync()
    {
        SelectFirstTab();
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Sets the first visible tab as the active tab.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public void SelectFirstTab()
    {
        if (DisableChanging) return;
        
        if (ActiveTab == null)
        {
            ActiveTab = Tabs.FirstOrDefault(x => x.Visible);
            OnTabChanged.InvokeAsync(Tabs.IndexOf(ActiveTab));
            StateHasChanged();
        }
    }

}
