namespace FileFlows.Shared.Widgets;

/// <summary>
/// Widget for Encoders
/// </summary>
public class Encoders:WidgetDefinition
{
    /// <summary>
    /// The Widget Definition UID
    /// </summary>
    public static readonly Guid WD_UID = new ("6c09bd5f-70be-43ad-ac75-40df33820b29");
    
    /// <summary>
    /// Gets the UID 
    /// </summary>
    public override Guid Uid => WD_UID;

    /// <summary>
    /// Gets the URL
    /// </summary>
    public override string Url => "/api/statistics/running-totals/Encoder";
    
    /// <summary>
    /// Gets the Icon
    /// </summary>
    public override string Icon => "fas fa-file-video";

    /// <summary>
    /// Gets the Name
    /// </summary>
    public override string Name => "Encoders";

    /// <summary>
    /// Gets the type of Widget
    /// </summary>
    public override WidgetType Type => WidgetType.TreeMap;

    /// <summary>
    /// Gets any flags 
    /// </summary>
    public override int Flags => 0;
}