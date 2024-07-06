using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;
using System.Xml.Schema;
using Humanizer.DateTimeHumanizeStrategy;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace FileFlows.Client.Components.Common;

/// <summary>
/// A skybox
/// </summary>
public partial class FlowSkyBox<TItem>
{
    private readonly List<FlowSkyBoxItem<TItem>> _Items = new ();

#pragma warning disable BL0007
    /// <summary>
    /// Gets or set the items to display in the skybox
    /// </summary>
    [Parameter]
    public List<FlowSkyBoxItem<TItem>> Items
    {
        get => _Items;
        set
        {
            _Items.Clear();
            if (value?.Any() == true)
                _Items.AddRange(value);
            this.StateHasChanged();
        }
    }
#pragma warning restore BL0007

    /// <summary>
    /// Gets or sets event that is called when a skybox item is selected
    /// </summary>
    [Parameter] public EventCallback<FlowSkyBoxItem<TItem>> OnSelected { get; set; }
    
    /// <summary>
    /// Gets or sets if this sky box does not show a count
    /// </summary>
    [Parameter] public bool NoCount { get; set; }

    /// <summary>
    /// Gets or sets the selected item
    /// </summary>
    public FlowSkyBoxItem<TItem> SelectedItem { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if(Items?.Any() == true)
            SelectedItem = Items.First();
    }

    /// <summary>
    /// Sets the selected item
    /// </summary>
    /// <param name="item">the item to select</param>
    void SetSelected(FlowSkyBoxItem<TItem> item)
    {
        this.SelectedItem = item;
        OnSelected.InvokeAsync(item);
    }

    /// <summary>
    /// Sets the items
    /// </summary>
    /// <param name="items">the items</param>
    /// <param name="selected">the selected value</param>
    public void SetItems(List<FlowSkyBoxItem<TItem>> items, TItem selected)
    {
        this._Items.Clear();
        items = items?.Where(x => x != null)?.ToList() ?? new();
        if(items.Any() == true)
            this._Items.AddRange(items.Where(x => x != null));
        if(selected != null)
            this.SelectedItem = items.FirstOrDefault(x => x.Value.Equals(selected));
        else
            this.SelectedItem = items.FirstOrDefault();
        this.StateHasChanged();
    }

    /// <summary>
    /// Sets a certain value as selected
    /// </summary>
    /// <param name="value">the value to set active</param>
    public void SetSelectedValue(TItem value)
    {
        this.SelectedItem = this.Items.FirstOrDefault(x => x.Value.Equals(value));
        this.StateHasChanged();
    }
}

/// <summary>
/// An item displayed in a skybox
/// </summary>
public class FlowSkyBoxItem<TItem>
{
    /// <summary>
    /// Gets or sets the icon
    /// </summary>
    public string Icon { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the class name
    /// </summary>
    public string ClassName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the name
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the count 
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Gets or sets the value
    /// </summary>
    public TItem Value { get; init; }
}