namespace FileFlows.Shared.Widgets;

/// <summary>
/// Widget for Decoders
/// </summary>
public class Decoders:WidgetDefinition
{
    /// <summary>
    /// The Widget Definition UID
    /// </summary>
    public static readonly Guid WD_UID = new ("162390b6-92f6-4bd5-ac70-1288e09ed49d");
    
    /// <summary>
    /// Gets the UID 
    /// </summary>
    public override Guid Uid => WD_UID;

    /// <summary>
    /// Gets the URL
    /// </summary>
    public override string Url => "/api/statistics/running-totals/Decoder";
    
    /// <summary>
    /// Gets the Icon
    /// </summary>
    public override string Icon => "fas fa-atom";

    /// <summary>
    /// Gets the Name
    /// </summary>
    public override string Name => "Decoders";

    /// <summary>
    /// Gets the type of Widget
    /// </summary>
    public override WidgetType Type => WidgetType.TreeMap;

    /// <summary>
    /// Gets any flags 
    /// </summary>
    public override int Flags => 0;
}