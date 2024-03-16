using FileFlows.Shared.Models;

namespace FileFlows.Shared.Widgets;

/// <summary>
/// Widget for Video Containers
/// </summary>
public class VideoContainers:WidgetDefinition
{
    /// <summary>
    /// The Widget Definition UID
    /// </summary>
    public static readonly Guid WD_UID = new ("eca4b7ed-97bd-4947-bd30-9307abba7900");
    
    /// <summary>
    /// Gets the UID 
    /// </summary>
    public override Guid Uid => WD_UID;

    /// <summary>
    /// Gets the URL
    /// </summary>
    public override string Url => "/api/statistics/running-totals/VIDEO_CONTAINER";
    
    /// <summary>
    /// Gets the Icon
    /// </summary>
    public override string Icon => "fas fa-file-video";

    /// <summary>
    /// Gets the Name
    /// </summary>
    public override string Name => "Video Containers";

    /// <summary>
    /// Gets the type of Widget
    /// </summary>
    public override WidgetType Type => WidgetType.PieChart;

    /// <summary>
    /// Gets any flags 
    /// </summary>
    public override int Flags => 0;
}