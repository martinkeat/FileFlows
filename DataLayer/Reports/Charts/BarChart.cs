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

        int longestYLabel = 0;
        const int yAxisLabelFrequency = 4; // Change as needed to control the number of labels

        // Convert values to double for processing
        double maxValue = chartData.Data.Max(x => x.Value);
        
        for (int i = 0; i <= yAxisLabelFrequency; i++)
        {
            double value = (maxValue / yAxisLabelFrequency) * i;
            object yValue = string.IsNullOrWhiteSpace(chartData.YAxisFormatter) ? (object)Convert.ToInt64(value) : (object)value;
            string yLabel = ChartFormatter.Format(yValue, chartData.YAxisFormatter, axis: true);
            longestYLabel = Math.Max(longestYLabel, yLabel.Length);
        }

        // Constants and initial setup
        int chartStartX = string.IsNullOrWhiteSpace(yAxisLabel) ? 12 : 100; // Adjusted based on yAxisLabel presence
        chartStartX += (longestYLabel * 10);
        int chartEndX = 20;
        const int chartStartY = 20; // The top of the y-axis where it starts, from the top
        int xAxisLabelOffset =
            string.IsNullOrWhiteSpace(yAxisLabel) ? 40 : 80; // Adjusted based on yAxisLabel presence
        const int yAxisLabelOffset = 10; // Fixed offset when yAxisLabel is present

        
        const int barWidth = 40;
        const int barSpacing = 80;
        const string backgroundColor = "#e4e4e4"; 
        const string foregroundColor = "#000";
        const string lineColor = "#afafaf";


        // Calculate bar width based on available space and number of bars
        int totalBars = chartData.Data.Count;
        int availableWidth = EmailChartWidth - 2 * chartStartX;
        int actualBarWidth = Math.Min(barWidth, (availableWidth - (totalBars - 1) * barSpacing) / totalBars);

        // Calculate the total width needed by the bars and spacing
        int totalWidthNeeded = totalBars * (actualBarWidth + barSpacing) - barSpacing;
        int startX = (availableWidth - totalWidthNeeded) / 2 + chartStartX; // Starting point for the first bar

        StringBuilder builder = new StringBuilder();
        builder.AppendLine(
            $"<svg class=\"bar-chart\" width=\"100%\" height=\"100%\" viewBox=\"0 0 {EmailChartWidth} {EmailChartHeight}\" xmlns=\"http://www.w3.org/2000/svg\">");

        // Draw background
        builder.AppendLine(
            $"<rect x=\"{chartStartX}\" y=\"{chartStartY}\" width=\"{EmailChartWidth - chartStartX - chartEndX}\" height=\"{EmailChartHeight - chartStartY - xAxisLabelOffset}\" fill=\"{backgroundColor}\" />");

        // Draw y-axis labels and grid lines
        int yAxisHeight = EmailChartHeight - xAxisLabelOffset - chartStartY;
        for (int i = 0; i <= yAxisLabelFrequency; i++)
        {
            double value = (maxValue / yAxisLabelFrequency) * i;
            int y = EmailChartHeight - xAxisLabelOffset - (int)((value / maxValue) * yAxisHeight);

            // Draw grid line
            builder.AppendLine(
                $"<line x1=\"{chartStartX}\" y1=\"{y}\" x2=\"{EmailChartWidth - chartEndX}\" y2=\"{y}\" stroke=\"{lineColor}\" />");

            // Format y-axis label
            object yValue = string.IsNullOrWhiteSpace(chartData.YAxisFormatter) ? Convert.ToInt64(value) : value;
            string yLabel = ChartFormatter.Format(yValue, chartData.YAxisFormatter, axis: true);

            // Draw y-axis label
            builder.AppendLine(
                $"<text x=\"{chartStartX - yAxisLabelOffset}\" y=\"{y + 5}\" text-anchor=\"end\" fill=\"{foregroundColor}\">{yLabel}</text>");

            // Draw y-axis tick
            builder.AppendLine(
                $"<line x1=\"{chartStartX - 5}\" y1=\"{y}\" x2=\"{chartStartX}\" y2=\"{y}\" stroke=\"{lineColor}\" />");
        }

        // Draw y-axis main label if provided, rotated 90 degrees
        if (!string.IsNullOrEmpty(yAxisLabel))
        {
            builder.AppendLine(
                $"<text x=\"{chartStartX - yAxisLabelOffset - 30}\" y=\"{EmailChartHeight / 2}\" text-anchor=\"middle\" fill=\"#fff\" transform=\"rotate(-90, {chartStartX - yAxisLabelOffset - 30}, {EmailChartHeight / 2})\">{yAxisLabel}</text>");
        }

        // Draw bars
        // Assuming a rough average width of a character based on font size
        const double averageCharWidth = 12 * 0.6; 
        const int maxChars = (int)((averageCharWidth * 16) / averageCharWidth);
        
        int x = startX;
        int labelInterval = Math.Max(1, totalBars / 20); // Show approximately 20 labels on the x-axis
        int count = 0;
        foreach(var item in chartData.Data)
        {
            string label = item.Key;
            double value = item.Value;
            int barHeight = (int)((value / maxValue) * yAxisHeight);

            // Bar coordinates
            int y = EmailChartHeight - xAxisLabelOffset - barHeight;

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

                string truncatedText = label;
                if (label.Length > maxChars)
                {
                    truncatedText = label[..(maxChars - 3)] + "...";
                }
                
                // Draw x-axis label
                builder.AppendLine(
                    $"<text x=\"{x + actualBarWidth / 2}\" y=\"{EmailChartHeight - xAxisLabelOffset + 25}\" text-anchor=\"middle\" " +
                    $"fill=\"{foregroundColor}\" clip-path=\"url(#clipPath)\">{truncatedText}</text>");
                // builder.AppendLine(
                //     $"<g transform=\"translate({x + actualBarWidth / 2}, {EmailChartHeight - xAxisLabelOffset + 10})\">" +
                //     $"<text fill=\"{foregroundColor}\">{label}</text>" +
                //     "</g>");

                // Draw x-axis tick
                builder.AppendLine(
                    $"<line x1=\"{x + actualBarWidth / 2}\" y1=\"{EmailChartHeight - xAxisLabelOffset}\" x2=\"{x + actualBarWidth / 2}\" y2=\"{EmailChartHeight - xAxisLabelOffset + 5}\" stroke=\"{lineColor}\" />");
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