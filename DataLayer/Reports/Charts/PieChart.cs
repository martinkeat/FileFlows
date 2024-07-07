using System.Text;
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
    /// <param name="generateSvg">If the image generated should be an SVG image, else the image will require javascript to render</param>
    /// <returns>The chart content</returns>
    public static string Generate(PieChartData data, bool generateSvg = false)
        => $"<div class=\"chart\"><h2 class=\"title\">{data.Title}</h2>" +
           (generateSvg ? Svg(data) : JavaScript(data))
           + "</div>";

    /// <summary>
    /// Generates an pie chart using javascript
    /// </summary>
    /// <param name="data">The data for the chart.</param>
    /// <returns>An HTML input to be parsed by javascript</returns>
    private static string JavaScript(PieChartData data)
        => "<input type=\"hidden\" class=\"report-pie-chart-data\" value=\"" + HttpUtility.HtmlEncode(JsonSerializer.Serialize(data.Data)) + "\" />";


    /// <summary>
    /// Generates an SVG pie chart
    /// </summary>
    /// <param name="chartData">The data for the chart.</param>
    /// <returns>An SVG string representing the line chart.</returns>
    private static string Svg(PieChartData chartData)
    {
        if (chartData.Data?.Any() != true)
            return string.Empty;
        
        int radius = Math.Min(EmailChartWidth, EmailChartHeight) / 2;
        const int centerX = EmailChartWidth / 2;
        const int centerY = EmailChartHeight / 2;

        double total = chartData.Data.Sum(item => (double)item.Value);
        double startAngle = 270;

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("<div class=\"chart pie-chart\">");
        builder.AppendLine(
            $"<svg class=\"pie-chart\" width=\"100%\" height=\"100%\" viewBox=\"0 0 {EmailChartWidth} {EmailChartHeight}\" xmlns=\"http://www.w3.org/2000/svg\">");


        var legendEntries = new List<(string Label, string Color, double Percentage)>();

        int count = 0;
        foreach (var item in chartData.Data)
        {
            string label = item.Key;
            double value = (double)item.Value;
            double sliceAngle = (value / total) * 360;
            double endAngle = startAngle + sliceAngle;
            double percentage = (value / total) * 100;

            double startAngleRadians = (Math.PI / 180) * startAngle;
            double endAngleRadians = (Math.PI / 180) * endAngle;

            double x1 = centerX + radius * Math.Cos(startAngleRadians);
            double y1 = centerY + radius * Math.Sin(startAngleRadians);

            double x2 = centerX + radius * Math.Cos(endAngleRadians);
            double y2 = centerY + radius * Math.Sin(endAngleRadians);

            var largeArcFlag = sliceAngle > 180 ? 1 : 0;

            string color = COLORS[count % COLORS.Length];
            string tooltip = $"{label}: {value} ({percentage:F2}%)";

            if (chartData.Data.Count == 1)
            {
                // Special case for a single item
                builder.AppendLine(
                    $"<circle data-title=\"{System.Net.WebUtility.HtmlEncode(tooltip)}\" class=\"slice\" cx=\"{centerX}\" cy=\"{centerY}\" r=\"{radius}\" fill=\"{color}\">" +
                    "</circle>");
            }
            else
            {
                builder.AppendLine(
                    $"<path data-title=\"{System.Net.WebUtility.HtmlEncode(tooltip)}\" class=\"slice\" d=\"M{centerX},{centerY} L{x1},{y1} A{radius},{radius} 0 {largeArcFlag},1 {x2},{y2} Z\" fill=\"{color}\">" +
                    "</path>");
            }

            legendEntries.Add((label, color, percentage));

            startAngle = endAngle;
            ++count;
        }

        builder.AppendLine("</svg>");
        // Add legend
        builder.AppendLine("<div class=\"legend\">");
        foreach (var entry in legendEntries)
        {
            builder.AppendLine("<span class=\"series\">");
            builder.AppendLine($"<svg width=\"12\" height=\"12\">" +
                               $"<circle cx=\"6\" cy=\"6\" r=\"5\" fill=\"{entry.Color}\" />" +
                               $"</svg>");
            //builder.AppendLine($"<span class=\"legend-label\">{HttpUtility.HtmlEncode(entry.Label)} ({entry.Percentage:F2}%)</span>");
            builder.AppendLine($"<span class=\"legend-label\">{HttpUtility.HtmlEncode(entry.Label)}</span>");
            builder.AppendLine("</span>");
        }

        builder.AppendLine("</div>");

        builder.AppendLine("</div>");
        return builder.ToString();
    }
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