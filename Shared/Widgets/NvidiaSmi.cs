namespace FileFlows.Shared.Widgets;

/// <summary>
/// Widget for NVIDIA smi
/// </summary>
public class NvidiaSmi:WidgetDefinition
{
    /// <summary>
    /// The Widget Definition UID
    /// </summary>
    public static readonly Guid WD_UID = new ("4d5c8a97-0b19-4d09-8893-106192bfb540");
    
    /// <summary>
    /// Gets the UID 
    /// </summary>
    public override Guid Uid => WD_UID;

    /// <summary>
    /// Gets the URL
    /// </summary>
    public override string Url => "/api/nvidia/smi";

    /// <summary>
    /// Gets the Name
    /// </summary>
    public override string Name => "NVIDIA SMI";
    
    /// <summary>
    /// Gets the Icon
    /// </summary>
    public override string Icon => "fas fa-rocket";

    /// <summary>
    /// Gets the type of Widget
    /// </summary>
    public override WidgetType Type => WidgetType.Nvidia;

    /// <summary>
    /// Gets any flags 
    /// </summary>
    public override int Flags => 0;
}