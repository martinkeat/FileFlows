using FileFlows.Shared.Models;

namespace FileFlows.Shared.Widgets;

/// <summary>
/// Widget for Codecs
/// </summary>
public class Codecs:WidgetDefinition
{
    /// <summary>
    /// The Widget Definition UID
    /// </summary>
    public static readonly Guid WD_UID = new ("d7f212b6-29eb-4912-b7b5-0efe1d377164");
    
    /// <summary>
    /// Gets the UID 
    /// </summary>
    public override Guid Uid => WD_UID;

    /// <summary>
    /// Gets the URL
    /// </summary>
    public override string Url => "/api/statistics/running-totals/CODEC";
    
    /// <summary>
    /// Gets the Icon
    /// </summary>
    public override string Icon => "fas fa-photo-video";

    /// <summary>
    /// Gets the Name
    /// </summary>
    public override string Name => "Codecs";

    /// <summary>
    /// Gets the type of Widget
    /// </summary>
    public override WidgetType Type => WidgetType.TreeMap;

    /// <summary>
    /// Gets any flags 
    /// </summary>
    public override int Flags => 0;
}