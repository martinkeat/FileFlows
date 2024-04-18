using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Common;

/// <summary>
/// Flow Wizard component
/// </summary>
public partial class FlowWizard : ComponentBase
{
    /// <summary>
    /// Gets or sets the content of the wizard
    /// </summary>
    [Parameter] public RenderFragment ChildContent { get; set; }
    /// <summary>
    /// Gets or sets the active page
    /// </summary>
    public FlowWizardPage ActivePage { get; internal set; }
    
    /// <summary>
    /// Gets or sets an event when a page changes
    /// </summary>
    [Parameter] public EventCallback<int> OnPageChanged { get; set; }
    
    /// <summary>
    /// Gets or sets an event when a finish is clicked
    /// </summary>
    [Parameter] public EventCallback OnFinish { get; set; }
    
    /// <summary>
    /// Gets or sets if the pages cannot be changed
    /// </summary>
    [Parameter] public bool DisableChanging { get; set; }
    
    /// <summary>
    /// Gets or sets if the finish button is disabled
    /// </summary>
    [Parameter] public bool FinishDisabled { get; set; }

    /// <summary>
    /// Represents a collection of pages.
    /// </summary>
    private List<FlowWizardPage> Pages = new();

    /// <summary>
    /// Adds a page to the collection.
    /// </summary>
    /// <param name="page">The page to add.</param>
    internal void AddPage(FlowWizardPage page)
    {
        if (Pages.Contains(page) == false)
        {
            Pages.Add(page);
            if (ActivePage == null)
                ActivePage = page;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Selects a page
    /// </summary>
    /// <param name="page">The page to select.</param>
    private void SelectPage(FlowWizardPage page)
    {
        if (DisableChanging || page.Disabled) return;
        ActivePage = page;
        OnPageChanged.InvokeAsync(Pages.IndexOf(page));
    }

    /// <summary>
    /// Called when the visibility of a page has changed
    /// </summary>
    internal void PageVisibilityChanged()
        => StateHasChanged();

    /// <inheritdoc/>
    protected override Task OnParametersSetAsync()
    {
        SelectFirstPage();
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Sets the first visible page as the active page.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public void SelectFirstPage()
    {
        if (DisableChanging) return;
        
        if (ActivePage == null)
        {
            ActivePage = Pages.FirstOrDefault(x => x.Visible);
            OnPageChanged.InvokeAsync(Pages.IndexOf(ActivePage));
            StateHasChanged();
        }
    }


    /// <summary>
    /// Selects the previous page
    /// </summary>
    private void Previous()
    {
        int index = Pages.IndexOf(ActivePage);
        while (true)
        {
            index--;
            if (index < 0)
                return;
            if (Pages[index].Visible)
                break;
        }
        
        SelectPage(Pages[index]);
    }
    
    /// <summary>
    /// Selects the next page
    /// </summary>
    private void Next()
    {
        if (ActivePage?.NextDisabled == true)
            return;
        
        int index = Pages.IndexOf(ActivePage);
        while (true)
        {
            index++;
            if (index >= Pages.Count)
                return;
            if (Pages[index].Visible)
                break;
        }
        SelectPage(Pages[index]);
    }

    /// <summary>
    /// Completes the wizard
    /// </summary>
    private void Finish()
    {
        OnFinish.InvokeAsync();
    }

    /// <summary>
    /// Triggers a state has change event
    /// </summary>
    public void TriggerStateHasChanged()
        => StateHasChanged();
}