using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using NPoco.Expressions;

namespace FileFlows.Client.Components.Dialogs;

/// <summary>
/// An flow item that appears in the template picker
/// </summary>
public partial class FlowTemplateItem
{
    /// <summary>
    /// Gets or sets the template
    /// </summary>
    [Parameter] public FlowTemplateModel Template { get; set; }
    
    /// <summary>
    /// Gets or sets if this template is selected
    /// </summary>
    [Parameter] public bool Selected { get; set; }
    
    /// <summary>
    /// Gets or sets the onclick event handler
    /// </summary>
    [Parameter] public EventHandler OnClick { get; set; }
    
    /// <summary>
    /// Gets or sets the on double click event handler
    /// </summary>
    [Parameter] public EventHandler OnDoubleClick { get; set; }
    
    /// <summary>
    /// Gets or sets if this is expanded
    /// </summary>
    private bool Expanded { get; set; }

    /// <summary>
    /// Gets or sets the plugins used by this flow
    /// </summary>
    private List<string> Plugins => Template.Plugins ?? new ();

    /// <summary>
    /// Gets or sets the scripts used by this flow
    /// </summary>
    private List<string> Scripts => Template.Scripts ?? new();

    /// <summary>
    /// Handles the action when the user clicks the main element
    /// </summary>
    /// <param name="args">the mouse client event args</param>
    private void HandleOnClick(MouseEventArgs args)
        => OnClick?.Invoke(this, args);

    /// <summary>
    /// Handles the action when the user double clicks the main element
    /// </summary>
    /// <param name="args">the mouse client event args</param>
    private void HandleOnDoubleClick(MouseEventArgs args)
        => OnDoubleClick?.Invoke(this, args);
    
    /// <summary>
    /// Handles the action when the user clicks the expand/collapse button
    /// </summary>
    /// <param name="args">the mouse client event args</param>
    private void HandleExpandCollapse(MouseEventArgs args)
        => Expanded = !Expanded;

    private static string lblMissingDependencies;
    
    protected override void OnInitialized()
    {
        if(string.IsNullOrWhiteSpace(lblMissingDependencies))
            lblMissingDependencies = Translater.Instant("Labels.MissingDependencies");
    }
}