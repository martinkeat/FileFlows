using System.Text;
using System.Text.Json;
using System.Web;

namespace FileFlows.DataLayer.Reports.Charts;

/// <summary>
/// Bar chart
/// </summary>
public class BarChart : Chart
{
    /// <summary>
    /// Generates a bar chart
    /// </summary>
    /// <param name="data">The data for the chart.</param>
    /// <param name="emailing">If the image generated should be an emailed</param>
    /// <returns>The chart content</returns>
    public static string Generate(BarChartData data, bool emailing = false)
    {
        if (emailing == false)
            return
                $"<div class=\"chart\"><h2 class=\"title\">{HttpUtility.HtmlEncode(data.Title)}</h2>{JavaScript(data)}</div>";

        return @$"
<div>
    <span class=""chart-title"" style=""{ReportBuilder.EmailTitleStyling}"">{HttpUtility.HtmlEncode(data.Title)}</span>
    {FileFlows.DataLayer.Helpers.DelegateHelpers.GenerateBarChart?.Invoke(data)}
</div>";
    }

    /// <summary>
    /// Generates an bar chart using javascript
    /// </summary>
    /// <param name="data">The data for the chart.</param>
    /// <returns>An HTML input to be parsed by javascript</returns>
    private static string JavaScript(BarChartData data)
        => "<input type=\"hidden\" class=\"report-bar-chart-data\" value=\"" +
           HttpUtility.HtmlEncode(JsonSerializer.Serialize(new
           {
               data = data.Data,
               yAxisFormatter = data.YAxisFormatter
           }))  + "\" />";


}

/// <summary>
/// Bar Chart Data
/// </summary>
public class BarChartData
{
    /// <summary>
    /// Gets or sets the title of the bar chart
    /// </summary>
    public string Title { get; set; } = null!;
    /// <summary>
    /// The data
    /// </summary>
    public Dictionary<string, double> Data { get; set; } = null!;

    /// <summary>
    /// Gets or sets the y-axis formatter
    /// </summary>
    public string? YAxisFormatter { get; set; }
}