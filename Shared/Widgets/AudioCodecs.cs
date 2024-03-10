using FileFlows.Shared.Models;

namespace FileFlows.Shared.Widgets;

/// <summary>
/// Widget for Audio Codecs
/// </summary>
public class AudioCodecs:WidgetDefinition
{
    /// <summary>
    /// The Widget Definition UID
    /// </summary>
    public static readonly Guid WD_UID = new("51dd33b4-7dcd-4123-8d8b-ecead198ba36");
    
    /// <summary>
    /// Gets the UID 
    /// </summary>
    public override Guid Uid => WD_UID;

    /// <summary>
    /// Gets the URL
    /// </summary>
    public override string Url => "/api/statistics/running-totals/AUDIO_CODEC";
    
    /// <summary>
    /// Gets the Icon
    /// </summary>
    public override string Icon => "fas fa-headphones";

    /// <summary>
    /// Gets the Name
    /// </summary>
    public override string Name => "Audio Codecs";

    /// <summary>
    /// Gets the type of Widget
    /// </summary>
    public override WidgetType Type => WidgetType.TreeMap;

    /// <summary>
    /// Gets any flags 
    /// </summary>
    public override int Flags => 0;
}