namespace FileFlows.Shared.Widgets;

/// <summary>
/// Widget for processing nodes
/// </summary>
public class ProcessingNodes:WidgetDefinition
{
    /// <summary>
    /// The Widget Definition UID
    /// </summary>
    public static readonly Guid WD_UID = new ("36859522-e2c3-4648-8466-7d7ad519cc8e");
    
    /// <summary>
    /// Gets the UID 
    /// </summary>
    public override Guid Uid => WD_UID;

    /// <summary>
    /// Gets the URL
    /// </summary>
    public override string Url => "/api/node/overview";

    /// <summary>
    /// Gets the Icon
    /// </summary>
    public override string Icon => "fas fa-server";

    /// <summary>
    /// Gets the Name
    /// </summary>
    public override string Name => "Processing Nodes";

    /// <summary>
    /// Gets the type of Widget
    /// </summary>
    public override WidgetType Type => WidgetType.ProcessingNodes;

    /// <summary>
    /// Gets the flags
    /// </summary>
    public override int Flags => 0;
}