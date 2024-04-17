using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Common;

/// <summary>
/// A page show in a flow wizard
/// </summary>
public partial class FlowWizardPage : ComponentBase
{
    /// <summary>
    /// Gets or sets the <see cref="FlowWizard"/> component containing this page.
    /// </summary>
    [CascadingParameter]
    FlowWizard Wizard { get; set; }

    private bool _Visible = true;

    /// <summary>
    /// Gets or sets a value indicating whether the page tab is visible.
    /// </summary>
    [Parameter]
    public bool Visible
    {
        get => _Visible;
        set
        {
            if (_Visible == value)
                return;
            _Visible = value;
            Wizard?.PageVisibilityChanged();
            this.StateHasChanged();
        }
    }

    private string _Title;

    /// <summary>
    /// Gets or sets the title of the page.
    /// </summary>
    [Parameter]
    public string Title
    {
        get => _Title;
        set { _Title = Translater.TranslateIfNeeded(value); }
    }

    /// <summary>
    /// Gets or sets the icon associated with the page.
    /// </summary>
    [Parameter]
    public string Icon { get; set; }

    /// <summary>
    /// Gets or sets the content of the page.
    /// </summary>
    [Parameter]
    public RenderFragment ChildContent { get; set; }


    /// <summary>
    /// Initializes the page when it is first rendered.
    /// </summary>
    protected override void OnInitialized()
    {
        Wizard.AddPage(this);
    }

    /// <summary>
    /// Determines whether the current tab page active.
    /// </summary>
    /// <returns><c>true</c> if the current page is active; otherwise, <c>false</c>.</returns>
    private bool IsActive() => this.Wizard.ActivePage == this;
}
