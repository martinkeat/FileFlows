using System.Text.Json;
using System.Web;

namespace FileFlows.DataLayer.Reports.Charts;

/// <summary>
/// Pie chart
/// </summary>
public class PieChart : Chart
{
    /// <summary>
    /// Generates a pie chart
    /// </summary>
    /// <param name="data">The data for the chart.</param>
    /// <param name="emailing">If the image generated should be an emailed</param>
    /// <returns>The chart content</returns>
    public static string Generate(PieChartData data, bool emailing = false)
    {
        if (emailing == false)
            return
                $"<div class=\"chart\"><h2 class=\"title\">{HttpUtility.HtmlEncode(data.Title)}</h2>{JavaScript(data)}</div>";

        return @$"
<div>
    <span class=""chart-title"" style=""{ReportBuilder.EmailTitleStyling}"">{HttpUtility.HtmlEncode(data.Title)}</span>
    {FileFlows.DataLayer.Helpers.DelegateHelpers.GeneratePieChart?.Invoke(data)}
</div>";
    }

    /// <summary>
    /// Generates an pie chart using javascript
    /// </summary>
    /// <param name="data">The data for the chart.</param>
    /// <returns>An HTML input to be parsed by javascript</returns>
    private static string JavaScript(PieChartData data)
        => "<input type=\"hidden\" class=\"report-pie-chart-data\" value=\"" + HttpUtility.HtmlEncode(JsonSerializer.Serialize(data.Data)) + "\" />";


}

/// <summary>
/// Pie Chart Data
/// </summary>
public class PieChartData
{
    /// <summary>
    /// Gets or sets the title of the pie chart
    /// </summary>
    public string Title { get; set; } = null!;
    /// <summary>
    /// The data
    /// </summary>
    public Dictionary<string, int> Data { get; set; } = null!;
}