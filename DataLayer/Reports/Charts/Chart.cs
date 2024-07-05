namespace FileFlows.DataLayer.Reports.Charts;

/// <summary>
/// A chart 
/// </summary>
public class Chart
{
    /// <summary>
    /// The width for emailed charts
    /// </summary>
    protected const int EmailChartWidth = 590;
    /// <summary>
    /// The height for emailed charts
    /// </summary>
    protected const int EmailChartHeight = 210;
    
    /// <summary>
    /// The colors to show on the chart
    /// </summary>
    protected static readonly string[] COLORS =
    [
        // same colors as JS
        "#33b2df","#33DF55A6", "#84004bd9", "#007bff", "#6610f2",
        "#17a2b8", "#fd7e14", "#28a745", "#20c997", "#ffc107",  "#ff4d76",
        // different colors
        "#007bff", "#28a745", "#ffc107", "#dc3545", "#17a2b8",
        "#fd7e14", "#6f42c1", "#20c997", "#6610f2", "#e83e8c",
        "#ff6347", "#4682b4", "#9acd32", "#8a2be2", "#ff4500",
        "#2e8b57", "#d2691e", "#ff1493", "#00ced1", "#b22222",
        "#daa520", "#5f9ea0", "#7f007f", "#808000", "#3cb371"
    ];

}