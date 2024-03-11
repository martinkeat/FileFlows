using FileFlows.Shared.Models;

namespace FileFlows.Shared.Widgets;

/// <summary>
/// Widget for Comic Pages
/// </summary>
public class ComicPages:WidgetDefinition
{
    /// <summary>
    /// The Widget Definition UID
    /// </summary>
    public static readonly Guid WD_UID = new ("81a4ac22-b71c-49fe-9124-9ddc68466ab5");
    
    /// <summary>
    /// Gets the UID 
    /// </summary>
    public override Guid Uid => WD_UID;

    /// <summary>
    /// Gets the URL
    /// </summary>
    public override string Url => "/api/statistics/average/COMIC_PAGES";
    
    /// <summary>
    /// Gets the Icon
    /// </summary>
    public override string Icon => "fas fa-book";

    /// <summary>
    /// Gets the Name
    /// </summary>
    public override string Name => "Comic Pages";

    /// <summary>
    /// Gets the type of Widget
    /// </summary>
    public override WidgetType Type => WidgetType.BellCurve;

    /// <summary>
    /// Gets any flags 
    /// </summary>
    public override int Flags => 0;
}