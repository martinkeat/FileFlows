using FileFlows.Shared.Models;

namespace FileFlows.Shared.Widgets;

/// <summary>
/// Widget for Video Resolution
/// </summary>
public class VideoResolution:WidgetDefinition
{
    /// <summary>
    /// The Widget Definition UID
    /// </summary>
    public static readonly Guid WD_UID = new ("af4a5687-18f5-406b-aa96-abb45912f289");
    
    /// <summary>
    /// Gets the UID 
    /// </summary>
    public override Guid Uid => WD_UID;

    /// <summary>
    /// Gets the URL
    /// </summary>
    public override string Url => "/api/statistics/running-totals/VIDEO_RESOLUTION";

    /// <summary>
    /// Gets the Name
    /// </summary>
    public override string Name => "Video Resolutions";
    
    /// <summary>
    /// Gets the Icon
    /// </summary>
    public override string Icon => "fas fa-tv";

    /// <summary>
    /// Gets the type of Widget
    /// </summary>
    public override WidgetType Type => WidgetType.PieChart;

    /// <summary>
    /// Gets any flags 
    /// </summary>
    public override int Flags => 0;
}