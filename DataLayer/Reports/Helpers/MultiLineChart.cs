using System.Text.Json;
using System.Web;

namespace FileFlows.DataLayer.Reports.Helpers;

/// <summary>
/// Multiline chart
/// </summary>
public class MultiLineChart
{

    /// <summary>
    /// Generates an SVG line chart from a collection of data with a single color for all lines.
    /// </summary>
    /// <typeparam name="T">The type of the numeric values in the data dictionary.</typeparam>
    /// <param name="data">The collection of data to generate the SVG line chart from.</param>
    /// <param name="yAxisFormatter">Optional formatter for the y-axis labels</param>
    /// <returns>An SVG string representing the line chart.</returns>
    public static string Generate(object data, string? yAxisFormatter = null)
    {
        return "<input type=\"hidden\" class=\"report-line-chart-data\" value=\"" + HttpUtility.HtmlEncode(
            JsonSerializer.Serialize(
                new
                {
                    data,
                    yAxisFormatter
                })) + "\" />";
    }
}