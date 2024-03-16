using FileFlows.Shared.Models;

namespace FileFlows.Shared.Widgets;

/// <summary>
/// Widget for Image Formats
/// </summary>
public class ImageFormats:WidgetDefinition
{
    /// <summary>
    /// The Widget Definition UID
    /// </summary>
    public static readonly Guid WD_UID = new ("c84c39d9-a954-4d88-8de9-22f3aa825e91");
    
    /// <summary>
    /// Gets the UID 
    /// </summary>
    public override Guid Uid => WD_UID;

    /// <summary>
    /// Gets the URL
    /// </summary>
    public override string Url => "/api/statistics/running-totals/IMAGE_FORMAT";
    
    /// <summary>
    /// Gets the Icon
    /// </summary>
    public override string Icon => "fas fa-book";

    /// <summary>
    /// Gets the Name
    /// </summary>
    public override string Name => "Image Formats";

    /// <summary>
    /// Gets the type of Widget
    /// </summary>
    public override WidgetType Type => WidgetType.PieChart;

    /// <summary>
    /// Gets any flags 
    /// </summary>
    public override int Flags => 0;
}