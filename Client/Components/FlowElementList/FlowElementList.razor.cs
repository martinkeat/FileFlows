using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using ffElement = FileFlows.Shared.Models.FlowElement;

namespace FileFlows.Client.Components;

/// <summary>
/// Represents a base class for a component that displays a list of elements.
/// </summary>
public partial class FlowElementList : ComponentBase
{
    private string txtFilter;
    private ElementReference eleFilter;
    private string lblObsoleteMessage, lblFilter;
    /// <summary>
    /// The selected item, used for mobile view so can add an element
    /// </summary>
    private string SelectedElement;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        lblFilter = Translater.Instant("Labels.FilterPlaceholder");
        lblObsoleteMessage = Translater.Instant("Labels.ObsoleteConfirm.Message");
        ApplyFilter();
    }

    /// <summary>
    /// Gets or sets the items to display in the list.
    /// </summary>
    [Parameter]
    public IEnumerable<ffElement> Items { get; set; }

    /// <summary>
    /// Gets or sets the filtered items to display in the list.
    /// </summary>
    private IEnumerable<ffElement> Filtered { get; set; }


    /// <summary>
    /// Handles the key down event for filtering.
    /// </summary>
    /// <param name="e">The keyboard event arguments.</param>
    protected void FilterKeyDown(KeyboardEventArgs e)
    {
        ApplyFilter();
    }

    /// <summary>
    /// Applies the filter to the items list.
    /// </summary>
    protected void ApplyFilter()
    {
        if (Items == null)
            return;

        if (string.IsNullOrWhiteSpace(txtFilter))
        {
            Filtered = Items;
        }
        else
        {
            Filtered = Items
                .Where(x => x.Name.ToLower().Replace(" ", "").Contains(txtFilter)
                            || x.Group.ToLower().Replace(" ", "").Contains(txtFilter)
                )
                .ToArray();
        }
    }

    /// <summary>
    /// Invokes the <see cref="OnSelectPart"/> event callback asynchronously.
    /// </summary>
    /// <param name="uid">The unique identifier of the selected element.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected void SelectPart(string uid)
    {
        if (App.Instance.IsMobile)
            SelectedElement = uid;
    }
}