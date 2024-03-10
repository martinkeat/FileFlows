using FileFlows.Shared.Models;

namespace FileFlows.Shared.Widgets;

/// <summary>
/// Widget for Audio Containers
/// </summary>
public class AudioContainers:WidgetDefinition
{
    /// <summary>
    /// The Widget Definition UID
    /// </summary>
    public static readonly Guid WD_UID = new ("1f57bad7-5e7a-452e-8382-4552642505d0");
    
    /// <summary>
    /// Gets the UID 
    /// </summary>
    public override Guid Uid => WD_UID;

    /// <summary>
    /// Gets the URL
    /// </summary>
    public override string Url => "/api/statistics/running-totals/AUDIO_CONTAINER";
    
    /// <summary>
    /// Gets the Icon
    /// </summary>
    public override string Icon => "fas fa-file-audio";

    /// <summary>
    /// Gets the Name
    /// </summary>
    public override string Name => "Audio Containers";

    /// <summary>
    /// Gets the type of Widget
    /// </summary>
    public override WidgetType Type => WidgetType.PieChart;

    /// <summary>
    /// Gets any flags 
    /// </summary>
    public override int Flags => 0;
}