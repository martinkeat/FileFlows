using FileFlows.Shared.Models;

namespace FileFlows.Shared.Widgets;

/// <summary>
/// Widget for Video Codecs
/// </summary>
public class VideoCodecs:WidgetDefinition
{
    /// <summary>
    /// The Widget Definition UID
    /// </summary>
    public static readonly Guid WD_UID = new ("e4ea40ed-f52b-4f81-bded-9eb402192b14");
    
    /// <summary>
    /// Gets the UID 
    /// </summary>
    public override Guid Uid => WD_UID;

    /// <summary>
    /// Gets the URL
    /// </summary>
    public override string Url => "/api/statistics/running-totals/VIDEO_CODEC";
    
    /// <summary>
    /// Gets the Icon
    /// </summary>
    public override string Icon => "fas fa-video";

    /// <summary>
    /// Gets the Name
    /// </summary>
    public override string Name => "Video Codecs";

    /// <summary>
    /// Gets the type of Widget
    /// </summary>
    public override WidgetType Type => WidgetType.TreeMap;

    /// <summary>
    /// Gets any flags 
    /// </summary>
    public override int Flags => 0;
}