namespace FileFlows.Shared.Widgets;

/// <summary>
/// Widget for storage saved
/// </summary>
public class StorageSaved:WidgetDefinition
{
    /// <summary>
    /// The Widget Definition UID
    /// </summary>
    public static readonly Guid WD_UID = new ("5560fbeb-be8a-4dcd-bff2-a7acd141b7e6");
    
    /// <summary>
    /// Gets the UID 
    /// </summary>
    public override Guid Uid => WD_UID;

    /// <summary>
    /// Gets the URL
    /// </summary>
    public override string Url => "/api/statistics/storage-saved";
    
    /// <summary>
    /// Gets the Icon
    /// </summary>
    public override string Icon => "fas fa-hdd";

    /// <summary>
    /// Gets the Name
    /// </summary>
    public override string Name => "Storage Saved";

    /// <summary>
    /// Gets the type of Widget
    /// </summary>
    public override WidgetType Type => WidgetType.Bar;

    /// <summary>
    /// Gets any flags 
    /// </summary>
    public override int Flags => 0;
    
}