using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Connections.Features;
using ffElement = FileFlows.Shared.Models.FlowElement;

namespace FileFlows.Client.Components;

/// <summary>
/// Represents a base class for a component that displays a list of elements.
/// </summary>
public partial class FlowElementList : ComponentBase
{
    private string txtFilter;
    private ElementReference eleFilter;
    private string lblObsoleteMessage, lblFilter, lblAdd, lblClose;
    /// <summary>
    /// The selected item, used for mobile view so can add an element
    /// </summary>
    private string SelectedElement;

    /// <summary>
    /// The selected group for the accordion view
    /// </summary>
    private string SelectedGroup;
    
    /// <summary>
    /// Gets or sets the default selected group
    /// </summary>
    [Parameter] public string DefaultGroup { get; set; }


    /// <inheritdoc />
    protected override void OnInitialized()
    {
        lblFilter = Translater.Instant("Labels.FilterPlaceholder");
        lblObsoleteMessage = Translater.Instant("Labels.ObsoleteConfirm.Message");
        lblAdd = Translater.Instant("Labels.Add");
        lblClose = Translater.Instant("Labels.Close");

        SetItems(Items);
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
    /// Gets or sets the event to open the script browser
    /// </summary>
    [Parameter] public Action OpenScriptBrowser { get; set; }

    
    /// <summary>
    /// Gets or sets the event to close the element list when viewed on mobile
    /// </summary>
    [Parameter] public Action Close { get; set; }
    
    /// <summary>
    /// Gets or sets the event that handles adding a selected item when viewed on mobile
    /// </summary>
    [Parameter] public Action<string> AddSelectedElement { get; set; }
    
    /// <summary>
    /// Gets or sets the event callback for the drag start event.
    /// </summary>
    [Parameter] public EventCallback<(DragEventArgs, FlowElement)> OnDragStart { get; set; }

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

    protected void DragStart(DragEventArgs e, FlowElement element)
        => _ = OnDragStart.InvokeAsync((e, element));

    private void SelectGroup(string group)
        => SelectedGroup = SelectedGroup == group ? null : group;

    private void FixItem(FlowElement x)
    {
        if (x.Type != FlowElementType.Script)
            return;
        
        // get the group name from the script name, eg 'File - Older Than', becomes 'File' Group and name 'Older Than'
        int index = x.Name.IndexOf(" - ", StringComparison.InvariantCulture);
        if (index < 0)
            return;
        x.Group = x.Name[..(index)];
    }

    private object FormatName(ffElement ele)
    {
        if (ele.Name.StartsWith(ele.Group + " - "))
            return ele.Name[(ele.Group.Length + 3)..];
        return ele.Name;
    }

    public void SetItems(IEnumerable<ffElement> items, string newDefaultGroup = "notset")
    {
        if (newDefaultGroup != "notset")
            DefaultGroup = newDefaultGroup;
        
        SelectedGroup = DefaultGroup;
        this.Items = Items;
        if (Items?.Any() == true)
        {
            foreach (var item in Items)
            {
                FixItem(item);
            }
        }

        ApplyFilter();
        StateHasChanged();
    }
}