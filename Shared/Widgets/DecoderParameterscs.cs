namespace FileFlows.Shared.Widgets;

/// <summary>
/// Widget for Decoder Parametesr
/// </summary>
public class DecoderParameters:WidgetDefinition
{
    /// <summary>
    /// The Widget Definition UID
    /// </summary>
    public static readonly Guid WD_UID = new ("a3fbcfaa-799c-4ecd-8f84-59c054c15a79");
    
    /// <summary>
    /// Gets the UID 
    /// </summary>
    public override Guid Uid => WD_UID;

    /// <summary>
    /// Gets the URL
    /// </summary>
    public override string Url => "/api/statistics/running-totals/DecoderParameters";
    
    /// <summary>
    /// Gets the Icon
    /// </summary>
    public override string Icon => "fas fa-atom";

    /// <summary>
    /// Gets the Name
    /// </summary>
    public override string Name => "Decoder Parameters";

    /// <summary>
    /// Gets the type of Widget
    /// </summary>
    public override WidgetType Type => WidgetType.TotalsTable;

    /// <summary>
    /// Gets any flags 
    /// </summary>
    public override int Flags => 0;
}