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
    /// Gets or sets if this is expanded
    /// </summary>
    private bool Expanded { get; set; }
    
    /// <summary>
    /// Gets or sets the plugins used by this flow
    /// </summary>
    private List<string> Plugins { get; set; }
    /// <summary>
    /// Gets or sets the scripts used by this flow
    /// </summary>
    private List<string> Scripts { get; set; }

    /// <summary>
    /// Handles the action when the user clicks the main element
    /// </summary>
    /// <param name="args">the mouse client event args</param>
    private void HandleOnClick(MouseEventArgs args)
    {
        OnClick?.Invoke(this, args);
    }

    /// <summary>
    /// Handles the action when the user clicks the expand/collapse button
    /// </summary>
    /// <param name="args">the mouse client event args</param>
    private void HandleExpandCollapse(MouseEventArgs args)
        => Expanded = !Expanded;

    protected override void OnInitialized()
    {
        Plugins = Template.Flow.Parts.Where(x => x.FlowElementUid.StartsWith("Script") == false)
            .Select(x => x.FlowElementUid.Split('.')[x.FlowElementUid.StartsWith("FileFlows.") ? 1 : 0].Humanize(LetterCasing.Title))
            .Distinct()
            .ToList();
        Scripts = Template.Flow.Parts.Where(x => x.FlowElementUid.StartsWith("Script"))
            .Select(x => x.FlowElementUid).Distinct().Select(x =>
                x[8..].Trim()
            ).ToList();
    }
}