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
    /// <param name="generateSvg">If the image generated should be an SVG image, else the image will require javascript to render</param>
    /// <returns>The chart content</returns>
    public static string Generate(BarChartData data, bool generateSvg = false)
        => $"<div class=\"chart\"><h2 class=\"title\">{data.Title}</h2>" +
           (generateSvg ? Svg(data) : JavaScript(data))
           + "</div>";

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


    /// <summary>
    /// Generates an SVG bar chart
    /// </summary>
    /// <param name="chartData">The data for the chart.</param>
    /// <returns>An SVG string representing the line chart.</returns>
    private static string Svg(BarChartData chartData)
    {
        if (chartData.Data?.Any() != true)
            return string.Empty;

        string? yAxisLabel = null;
        
        const int chartWidth = 600;
        const int chartHeight = 500;
        const int barWidth = 40;
        const int barSpacing = 20;
        int chartStartX = string.IsNullOrWhiteSpace(yAxisLabel) ? 60 : 100; // Adjusted based on yAxisLabel presence
        const int chartStartY = 20; // The top of the y-axis where it starts, from the top
        int xAxisLabelOffset =
            string.IsNullOrWhiteSpace(yAxisLabel) ? 110 : 150; // Adjusted based on yAxisLabel presence
        const int yAxisLabelOffset = 40; // Fixed offset when yAxisLabel is present
        const int yAxisLabelFrequency = 10; // Change as needed to control the number of labels
        const string backgroundColor = "#161616"; // Dark background color

        // Convert values to double for processing
        //double ConvertToDouble(T value) => Convert.ToDouble(value);
        double maxValue = chartData.Data.Max(x => x.Value);

        // Calculate bar width based on available space and number of bars
        int totalBars = chartData.Data.Count;
        int availableWidth = chartWidth - 2 * chartStartX;
        int actualBarWidth = Math.Min(barWidth, (availableWidth - (totalBars - 1) * barSpacing) / totalBars);

        // Calculate the total width needed by the bars and spacing
        int totalWidthNeeded = totalBars * (actualBarWidth + barSpacing) - barSpacing;
        int startX = (availableWidth - totalWidthNeeded) / 2 + chartStartX; // Starting point for the first bar

        StringBuilder builder = new StringBuilder();
        builder.AppendLine(
            $"<svg class=\"bar-chart\" width=\"{chartWidth}\" height=\"{chartHeight}\" viewBox=\"0 0 {chartWidth} {chartHeight}\" xmlns=\"http://www.w3.org/2000/svg\">");

        // Draw background
        builder.AppendLine(
            $"<rect x=\"{chartStartX}\" y=\"{chartStartY}\" width=\"{chartWidth - 2 * chartStartX}\" height=\"{chartHeight - chartStartY - xAxisLabelOffset}\" fill=\"{backgroundColor}\" />");

        // Draw y-axis labels and grid lines
        int yAxisHeight = chartHeight - xAxisLabelOffset - chartStartY;
        for (int i = 0; i <= yAxisLabelFrequency; i++)
        {
            double value = (maxValue / yAxisLabelFrequency) * i;
            int y = chartHeight - xAxisLabelOffset - (int)((value / maxValue) * yAxisHeight);

            // Draw grid line
            builder.AppendLine(
                $"<line x1=\"{chartStartX}\" y1=\"{y}\" x2=\"{chartWidth - chartStartX}\" y2=\"{y}\" stroke=\"#555\" />");

            //string yLabel = yAxisFormatter == null ? $"{value:F0}" : yAxisFormatter(value);
            string yLabel = $"{value:F0}";
            
            // Draw y-axis label
            builder.AppendLine(
                $"<text x=\"{chartStartX - yAxisLabelOffset + 30}\" y=\"{y + 5}\" text-anchor=\"end\" fill=\"#fff\">{yLabel}</text>");

            // Draw y-axis tick
            builder.AppendLine(
                $"<line x1=\"{chartStartX - 5}\" y1=\"{y}\" x2=\"{chartStartX}\" y2=\"{y}\" stroke=\"#fff\" />");
        }

        // Draw y-axis main label if provided, rotated 90 degrees
        if (!string.IsNullOrEmpty(yAxisLabel))
        {
            builder.AppendLine(
                $"<text x=\"{chartStartX - yAxisLabelOffset - 30}\" y=\"{chartHeight / 2}\" text-anchor=\"middle\" fill=\"#fff\" transform=\"rotate(-90, {chartStartX - yAxisLabelOffset - 30}, {chartHeight / 2})\">{yAxisLabel}</text>");
        }

        // Draw bars
        int x = startX;
        int labelInterval = Math.Max(1, totalBars / 20); // Show approximately 20 labels on the x-axis
        int count = 0;
        foreach(var item in chartData.Data)
        {
            string label = item.Key;
            double value = item.Value;
            int barHeight = (int)((value / maxValue) * yAxisHeight);

            // Bar coordinates
            int y = chartHeight - xAxisLabelOffset - barHeight;

            // Bar color
            string color = "#007bff"; // Blue color

            // Tooltip
            string tooltip = $"{label}: {value}";

            // Draw bar
            builder.AppendLine(
                $"<rect class=\"bar-chart-bar\" x=\"{x}\" y=\"{y}\" width=\"{actualBarWidth}\" height=\"{barHeight}\" fill=\"{color}\" data-title=\"{System.Net.WebUtility.HtmlEncode(tooltip)}\" />");

            // Label below the bar (rotated 90 degrees)
            if (count % labelInterval == 0)
            {
                builder.AppendLine(
                    $"<g transform=\"translate({x + actualBarWidth / 2}, {chartHeight - xAxisLabelOffset + 10})\">" +
                    $"<text transform=\"rotate(90)\" dy=\"0.4em\" fill=\"#fff\">{label}</text>" +
                    "</g>");

                // Draw x-axis tick
                builder.AppendLine(
                    $"<line x1=\"{x + actualBarWidth / 2}\" y1=\"{chartHeight - xAxisLabelOffset}\" x2=\"{x + actualBarWidth / 2}\" y2=\"{chartHeight - xAxisLabelOffset + 5}\" stroke=\"#fff\" />");
            }

            x += actualBarWidth + barSpacing;
            count++;
        }

        builder.AppendLine("</svg>");
        return builder.ToString();
    }
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