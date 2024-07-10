using System.Text.Json;
using System.Web;

namespace FileFlows.DataLayer.Reports.Charts;

/// <summary>
/// Multiline chart
/// </summary>
public class MultiLineChart : Chart
{
    /// <summary>
    /// Generates a multi-line chart
    /// </summary>
    /// <param name="data">The data for the chart.</param>
    /// <param name="yAxisFormatter">Optional formatter for the y-axis labels</param>
    /// <param name="emailing">If the image generated is being emailed</param>
    /// <returns>The chart content</returns>
    public static string Generate(LineChartData data, string? yAxisFormatter = null, bool emailing = false)
    {
        if (emailing == false)
            return $"<div class=\"chart\"><h2 class=\"title\">{HttpUtility.HtmlEncode(data.Title)}</h2>{JavaScript(data, yAxisFormatter)}</div>";
        
        return @$"
<div>
    <span class=""chart-title"" style=""{ReportBuilder.EmailTitleStyling}"">{HttpUtility.HtmlEncode(data.Title)}</span>
    {FileFlows.DataLayer.Helpers.DelegateHelpers.GenerateLineChart?.Invoke(data)}
</div>";
    }

    /// <summary>
    /// Generates an multi line chart using javascript
    /// </summary>
    /// <param name="data">The data for the chart.</param>
    /// <param name="yAxisFormatter">Optional formatter for the y-axis labels</param>
    /// <returns>An HTML input to be parsed by javascript</returns>
    private static string JavaScript(LineChartData data, string? yAxisFormatter)
        => "<input type=\"hidden\" class=\"report-line-chart-data\" value=\"" + HttpUtility.HtmlEncode(
            JsonSerializer.Serialize(
                new
                {
                    data,
                    yAxisFormatter
                }, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                })) + "\" />";
}


/// <summary>
/// Multi line chart data
/// </summary>
public class LineChartData
{
    /// <summary>
    /// Gets or sets the title
    /// </summary>
    public string Title { get; set; } = null!;
    /// <summary>
    /// Gets or sets the labels
    /// </summary>
    public DateTime[] Labels { get; set; } = null!;
    /// <summary>
    /// Gets or sets the y-axis formatter
    /// </summary>
    public string? YAxisFormatter { get; set; }
    /// <summary>
    /// Gets or sets the series 
    /// </summary>
    public ChartSeries[] Series { get; set; } = null!;
}

/// <summary>
/// Chart series
/// </summary>
public class ChartSeries
{
    /// <summary>
    /// Gets or sets the name of the series
    /// </summary>
    public string Name { get; set; } = null!;
    /// <summary>
    /// Gets or sets the data for the series
    /// </summary>
    public double[] Data { get; set; } = null!;
}