namespace FileFlows.Shared.Widgets;

/// <summary>
/// Widget for processing files
/// </summary>
public class Processing:WidgetDefinition
{
    /// <summary>
    /// The Widget Definition UID
    /// </summary>
    public static readonly Guid WD_UID = new ("9a74f25b-85b0-4838-a994-479728a243df");
    
    /// <summary>
    /// Gets the UID 
    /// </summary>
    public override Guid Uid => WD_UID;

    /// <summary>
    /// Gets the URL
    /// </summary>
    public override string Url => "/api/worker";

    /// <summary>
    /// Gets the Name
    /// </summary>
    public override string Name => "Processing";
    
    /// <summary>
    /// Gets the Icon
    /// </summary>
    public override string Icon => "fas fa-running";

    /// <summary>
    /// Gets the type of Widget
    /// </summary>
    public override WidgetType Type => WidgetType.Processing;

    /// <summary>
    /// Gets any flags 
    /// </summary>
    public override int Flags => 0;
}