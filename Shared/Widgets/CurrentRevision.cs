namespace FileFlows.Shared.Widgets;

/// <summary>
/// Widget for current revision
/// </summary>
public class CurrentRevision:WidgetDefinition
{
    /// <summary>
    /// The Widget Definition UID
    /// </summary>
    public static readonly Guid WD_UID = new ("37c762ca-69ba-42c3-a14e-95b0b254d916");
    
    /// <summary>
    /// Gets the UID 
    /// </summary>
    public override Guid Uid => WD_UID;

    /// <summary>
    /// Gets the URL
    /// </summary>
    public override string Url => "/api/settings/current-config/revision";
    
    /// <summary>
    /// Gets the Icon
    /// </summary>
    public override string Icon => "fas fa-cogs";

    /// <summary>
    /// Gets the Name
    /// </summary>
    public override string Name => "Current Revision";

    /// <summary>
    /// Gets the type of Widget
    /// </summary>
    public override WidgetType Type => WidgetType.Counter;

    /// <summary>
    /// Gets the flags
    /// </summary>
    public override int Flags => 0;
}