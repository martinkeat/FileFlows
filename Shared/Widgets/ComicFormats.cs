using FileFlows.Shared.Models;

namespace FileFlows.Shared.Widgets;

/// <summary>
/// Widget for Comic Formats
/// </summary>
public class ComicFormats:WidgetDefinition
{
    /// <summary>
    /// The Widget Definition UID
    /// </summary>
    public static readonly Guid WD_UID = new ("b2bfbacc-3f62-4e15-9ace-e96a059079c6");
    
    /// <summary>
    /// Gets the UID 
    /// </summary>
    public override Guid Uid => WD_UID;

    /// <summary>
    /// Gets the URL
    /// </summary>
    public override string Url => "/api/statistics/running-totals/COMIC_FORMAT";
    
    /// <summary>
    /// Gets the Icon
    /// </summary>
    public override string Icon => "fas fa-book";

    /// <summary>
    /// Gets the Name
    /// </summary>
    public override string Name => "Comic Formats";

    /// <summary>
    /// Gets the type of Widget
    /// </summary>
    public override WidgetType Type => WidgetType.PieChart;

    /// <summary>
    /// Gets any flags 
    /// </summary>
    public override int Flags => 0;
}