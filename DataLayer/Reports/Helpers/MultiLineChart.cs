using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;

namespace FileFlows.DataLayer.Reports.Helpers;

/// <summary>
/// Multiline chart
/// </summary>
public class MultiLineChart
{
    /// <summary>
    /// Generates a multi-line chart
    /// </summary>
    /// <param name="data">The data for the chart.</param>
    /// <param name="yAxisFormatter">Optional formatter for the y-axis labels</param>
    /// <param name="generateSvg">If the image generated should be an SVG image, else the image will require javascript to render</param>
    /// <returns>The chart content</returns>
    public static string Generate(MultilineChartData data, string? yAxisFormatter = null, bool generateSvg = false)
    {
        if (generateSvg)
            return Svg(data);
        return JavaScript(data, yAxisFormatter);
    }

    /// <summary>
    /// Generates an multi line chart using javascript
    /// </summary>
    /// <param name="data">The data for the chart.</param>
    /// <param name="yAxisFormatter">Optional formatter for the y-axis labels</param>
    /// <returns>An HTML input to be parsed by javascript</returns>
    private static string JavaScript(MultilineChartData data, string? yAxisFormatter)
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

    private static readonly string[] COLORS =
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


    /// <summary>
    /// Generates an SVG line chart
    /// </summary>
    /// <param name="chartData">The data for the chart.</param>
    /// <returns>An SVG string representing the line chart.</returns>
    private static string Svg(MultilineChartData chartData)
    {
        string? yAxisLabel = null;

        // Constants and initial setup
        const int chartWidth = 800; // Increased chart width
        const int chartHeight = 400; // Increased chart height
        const int lineThickness = 2;
        int chartStartX = string.IsNullOrWhiteSpace(yAxisLabel) ? 100 : 140; // Adjusted based on yAxisLabel presence
        const int chartStartY = 20; // The top of the y-axis where it starts, from the top
        int xAxisLabelOffset =
            string.IsNullOrWhiteSpace(yAxisLabel) ? 40 : 80; // Adjusted based on yAxisLabel presence
        const int yAxisLabelOffset = 10; // Fixed offset when yAxisLabel is present
        const int yAxisLabelFrequency = 10; // Change as needed to control the number of labels
        
        //if dark
        // const string backgroundColor = "#161616"; // Dark background color
        // const string foregroundColor = "#fff";
        // if light
        const string backgroundColor = "#fafafa"; // Dark background color
        const string foregroundColor = "#000";

        // Convert values to double for processing
        double maxValue = chartData.Series.SelectMany(x => x.Data).Max(item => item);

        // Start building SVG content
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("<div class=\"chart line-chart\">");
        builder.AppendLine(
            $"<svg class=\"line-chart\" width=\"{chartWidth}\" height=\"{chartHeight}\" viewBox=\"0 0 {chartWidth} {chartHeight}\" xmlns=\"http://www.w3.org/2000/svg\">");

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

            // Format y-axis label
            object yValue = string.IsNullOrWhiteSpace(chartData.YAxisFormatter) ? (object)Convert.ToInt64(value) : (object)value;
            string yLabel = ChartFormatter.Format(yValue, chartData.YAxisFormatter);

            // Draw y-axis label
            builder.AppendLine(
                $"<text x=\"{chartStartX - yAxisLabelOffset}\" y=\"{y + 5}\" text-anchor=\"end\" fill=\"{foregroundColor}\">{yLabel}</text>");

            // Draw y-axis tick
            builder.AppendLine(
                $"<line x1=\"{chartStartX - 5}\" y1=\"{y}\" x2=\"{chartStartX}\" y2=\"{y}\" stroke=\"{foregroundColor}\" />");
        }

        // Draw y-axis main label if provided
        if (!string.IsNullOrEmpty(yAxisLabel))
        {
            builder.AppendLine(
                $"<text x=\"{chartStartX - yAxisLabelOffset - 30}\" y=\"{chartHeight / 2}\" text-anchor=\"middle\" fill=\"{foregroundColor}\">{yAxisLabel}</text>");
        }

        // Calculate the total width needed by the lines and spacing
        int totalLines = chartData.Series.Max(x => x.Data.Length);
        int availableWidth = chartWidth - 2 * chartStartX;
        double xStep = (double)availableWidth / (totalLines - 1);

        // Draw series lines
        for (int seriesIndex = 0; seriesIndex < chartData.Series.Length; seriesIndex++)
        {
            var series = chartData.Series[seriesIndex];
            string color = COLORS[seriesIndex % COLORS.Length]; // Cycling through COLORS array

            // Build polyline points
            StringBuilder pointsBuilder = new StringBuilder();
            for (int i = 0; i < series.Data.Length; i++)
            {
                int x = chartStartX + (int)(i * xStep);
                int y = chartHeight - xAxisLabelOffset - (int)((series.Data[i] / maxValue) * yAxisHeight);
                pointsBuilder.Append($"{x},{y} ");
            }

            // Draw polyline for the series
            builder.AppendLine(
                $"<polyline points=\"{pointsBuilder}\" fill=\"none\" stroke=\"{color}\" stroke-width=\"{lineThickness}\" />");
        }

        // Ensure we don't exceed the maximum number of labels
        int maxLabels = 10; // Maximum number of x-axis labels
        int step = Math.Max(totalLines / maxLabels, 1);


        var minDateUtc = chartData.Labels.Min();
        var maxDateUtc = chartData.Labels.Max();
        var totalDays = (maxDateUtc - minDateUtc).Days;

        string yAxisFormat = "{0:MMM} '{0:yy}";
        if (totalDays <= 1)
            yAxisFormat = "{0:HH}:00";
        else if (totalDays <= 180)
            yAxisFormat = "{0:%d} {0:MMM}";

        // Draw x-axis labels and ticks
        for (int i = 0; i < totalLines; i += step)
        {
            string label = string.Format(yAxisFormat, chartData.Labels[i].ToLocalTime());
            int x = chartStartX + (int)(i * xStep);

            // Draw x-axis label
            builder.AppendLine(
                $"<text x=\"{x}\" y=\"{chartHeight - xAxisLabelOffset + 25}\" text-anchor=\"middle\" fill=\"{foregroundColor}\">{label}</text>");

            // Draw x-axis tick
            builder.AppendLine(
                $"<line x1=\"{x}\" y1=\"{chartHeight - xAxisLabelOffset}\" x2=\"{x}\" y2=\"{chartHeight - xAxisLabelOffset + 5}\" stroke=\"{foregroundColor}\" />");
        }

        // Close SVG tag
        builder.AppendLine("</svg>");

        if (chartData.Series.Length > 1) // only show legend if more than one series
        {
            builder.AppendLine("<div class=\"legend\">");
            for (int seriesIndex = 0; seriesIndex < chartData.Series.Length; seriesIndex++)
            {
                var series = chartData.Series[seriesIndex];
                string color = COLORS[seriesIndex % COLORS.Length]; // Cycling through COLORS array

                builder.AppendLine(
                    $"<span style=\"display: inline-block; margin: 0 10px;\">" +
                    $"<svg width=\"12\" height=\"12\">" +
                    $"<circle cx=\"6\" cy=\"6\" r=\"5\" fill=\"{color}\" />" +
                    $"</svg>" +
                    $"<span style=\"color: {foregroundColor}; margin-left: 5px;\">{series.Name}</span>" +
                    $"</span>");
            }

            builder.AppendLine("</div>");
        }

        builder.AppendLine("</div>");
        

        return builder.ToString();
    }
}


public class MultilineChartData
{
    public DateTime[] Labels { get; set; } = null!;
    public string? YAxisFormatter { get; set; }
    public ChartSeries[] Series { get; set; } = null!;
}

public class ChartSeries
{
    public string Name { get; set; } = null!;
    public double[] Data { get; set; } = null!;
}