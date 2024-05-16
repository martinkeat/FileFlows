using System.Threading;
using BlazorMonaco;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using NPoco.Expressions;

namespace FileFlows.Client.Components.Common;

/// <summary>
/// Context menu
/// </summary>
public partial class FlowContextMenu : IDisposable
{
    /// <summary>
    /// Gets or sets the items in the context menu
    /// </summary>
    [Parameter]
    public List<FlowContextMenuItem> Items { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the child content
    /// </summary>
    [Parameter] public RenderFragment ChildContent { get; set; }
    
    /// <summary>
    /// Gets or sets a css classes to add to the wrapper div 
    /// </summary>
    [Parameter] public string CssClass { get; set; }
    
    /// <summary>
    /// Gets or sets if the context menu is visible
    /// </summary>
    public bool Visible { get; set; }

    /// <summary>
    /// Gets or sets an event that is shown just prior to opening a context menu
    /// </summary>
    [Parameter] public EventCallback PreShow { get; set; }

    private double xPos, yPos;

    protected override void OnInitialized()
    {
        App.Instance.OnDocumentClick += OnDocumentClick;
        App.Instance.OnWindowBlur += OnWindowBlur;
    }

    public void SetItems(List<FlowContextMenuItem> items)
    {
        Items = items ?? new();
    }

    public void ShowAt(int xPos, int yPos)
    {
        this.xPos = xPos;
        this.yPos = yPos;
        this.Visible = true;
    }

    void Clicked(FlowContextMenuItem item)
    {
        this.Visible = false;
        item.OnClick?.Invoke();
    }

    async Task ShowContextMenu(MouseEventArgs e)
    {
        Logger.Instance.ILog("About to call preshow");
        await this.PreShow.InvokeAsync();
        Logger.Instance.ILog("called preshow!");
        if (this.Items?.Any() != true)
            return;
        
        this.xPos = e.ClientX;
        this.yPos = e.ClientY;
        this.Visible = true;
    }

    public void Dispose()
    {
        App.Instance.OnDocumentClick -= OnDocumentClick;
        App.Instance.OnWindowBlur -= OnWindowBlur;
    }

    void OnDocumentClick()
    {
        Visible = false;
        StateHasChanged();
    }
    void OnWindowBlur()
    {
        Visible = false;
        StateHasChanged();
    }
}

/// <summary>
/// An item in a context menu
/// </summary>
public class FlowContextMenuItem
{
    /// <summary>
    /// Gets or sets if this is a separator
    /// </summary>
    public bool Separator { get; set; }
    /// <summary>
    /// Gets or sets the label
    /// </summary>
    public string Label { get; set; }
    /// <summary>
    /// Gets or sets the icon
    /// </summary>
    public string Icon { get; set; }
    /// <summary>
    /// Gets or sets the onclick action
    /// </summary>
    public Action OnClick { get; set; }
    
    /// <summary>
    /// Gets or sets if this ia help button
    /// </summary>
    public bool IsHelpButton { get; set; }
}