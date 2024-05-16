using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Common;

/// <summary>
/// Icon Value 
/// </summary>
public partial class IconValue : ComponentBase
{
    /// <summary>
    /// Gets or sets the icon
    /// </summary>
    [Parameter] public string Icon { get; set; }
    /// <summary>
    /// Gets or sets the value
    /// </summary>
    [Parameter] public string Value { get; set; }

    private string _Tooltip;
#pragma warning disable BL0007
    /// <summary>
    /// Gets or sets the tootlip
    /// </summary>
    [Parameter]
    public string Tooltip
    {
        get => _Tooltip;
        set => _Tooltip = Translater.TranslateIfNeeded(value);
    }
#pragma warning restore BL0007

    /// <summary>
    /// Gets or sets the color 
    /// </summary>
    [Parameter] public IconValueColor Color { get; set; }

    /// <summary>
    /// Gets or sets the click event
    /// </summary>
    [Parameter] public Action OnClick { get; set; }

    /// <summary>
    /// User clicked the icon label
    /// </summary>
    private void ClickPerformed()
        => OnClick?.Invoke();
}

/// <summary>
/// A color for an icon value
/// </summary>
public enum IconValueColor
{
    /// <summary>
    /// Blue
    /// </summary>
    Blue,
    /// <summary>
    /// Green
    /// </summary>
    Green,
    /// <summary>
    /// Purple
    /// </summary>
    Purple,
    /// <summary>
    /// Pink
    /// </summary>
    Pink,
    /// <summary>
    /// Orange
    /// </summary>
    Orange,
    /// <summary>
    /// Red
    /// </summary>
    Red
}