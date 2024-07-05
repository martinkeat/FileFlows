using System.Text;
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
    /// <param name="generateSvg">If the image generated should be an SVG image, else the image will require javascript to render</param>
    /// <returns>The chart content</returns>
    public static string Generate(MultilineChartData data, string? yAxisFormatter = null, bool generateSvg = false)
        => $"<div class=\"chart\"><h2 class=\"title\">{data.Title}</h2>" +
           (generateSvg ? Svg(data) : JavaScript(data, yAxisFormatter))
           + "</div>";

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


    /// <summary>
    /// Generates an SVG line chart
    /// </summary>
    /// <param name="chartData">The data for the chart.</param>
    /// <returns>An SVG string representing the line chart.</returns>
    private static string Svg(MultilineChartData chartData)
    {
        string? yAxisLabel = null;

        int longestYLabel = 0;
        const int yAxisLabelFrequency = 4; // Change as needed to control the number of labels

        // Convert values to double for processing
        double maxValue = chartData.Series.SelectMany(x => x.Data).Max(item => item);
        
        for (int i = 0; i <= yAxisLabelFrequency; i++)
        {
            double value = (maxValue / yAxisLabelFrequency) * i;
            object yValue = string.IsNullOrWhiteSpace(chartData.YAxisFormatter) ? (object)Convert.ToInt64(value) : (object)value;
            string yLabel = ChartFormatter.Format(yValue, chartData.YAxisFormatter, axis: true);
            longestYLabel = Math.Max(longestYLabel, yLabel.Length);
        }

        // Constants and initial setup
        const int lineThickness = 2;
        int chartStartX = string.IsNullOrWhiteSpace(yAxisLabel) ? 12 : 100; // Adjusted based on yAxisLabel presence
        chartStartX += (longestYLabel * 10);
        int chartEndX = 20;
        const int chartStartY = 20; // The top of the y-axis where it starts, from the top
        int xAxisLabelOffset =
            string.IsNullOrWhiteSpace(yAxisLabel) ? 40 : 80; // Adjusted based on yAxisLabel presence
        const int yAxisLabelOffset = 10; // Fixed offset when yAxisLabel is present
        
        
        
        //if dark
        // const string backgroundColor = "#161616"; // Dark background color
        // const string foregroundColor = "#fff";
        // if light
        const string backgroundColor = "#e4e4e4"; 
        const string foregroundColor = "#000";
        const string lineColor = "#afafaf";

        // Start building SVG content
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("<div class=\"chart line-chart\">");
        builder.AppendLine(
            $"<svg class=\"line-chart\" width=\"100%\" height=\"100%\" viewBox=\"0 0 {EmailChartWidth} {EmailChartHeight}\" xmlns=\"http://www.w3.org/2000/svg\">");

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
            object yValue = string.IsNullOrWhiteSpace(chartData.YAxisFormatter) ? (object)Convert.ToInt64(value) : (object)value;
            string yLabel = ChartFormatter.Format(yValue, chartData.YAxisFormatter, axis: true);

            // Draw y-axis label
            builder.AppendLine(
                $"<text x=\"{chartStartX - yAxisLabelOffset}\" y=\"{y + 5}\" text-anchor=\"end\" fill=\"{foregroundColor}\">{yLabel}</text>");

            // Draw y-axis tick
            builder.AppendLine(
                $"<line x1=\"{chartStartX - 5}\" y1=\"{y}\" x2=\"{chartStartX}\" y2=\"{y}\" stroke=\"{lineColor}\" />");
        }

        // Draw y-axis main label if provided
        if (!string.IsNullOrEmpty(yAxisLabel))
        {
            builder.AppendLine(
                $"<text x=\"{chartStartX - yAxisLabelOffset - 30}\" y=\"{EmailChartHeight / 2}\" text-anchor=\"middle\" fill=\"{foregroundColor}\">{yAxisLabel}</text>");
        }

        // Calculate the total width needed by the lines and spacing
        int totalLines = chartData.Series.Max(x => x.Data.Length);
        int availableWidth = EmailChartWidth - chartStartX - chartEndX;
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
                int y = EmailChartHeight - xAxisLabelOffset - (int)((series.Data[i] / maxValue) * yAxisHeight);
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
                $"<text x=\"{x}\" y=\"{EmailChartHeight - xAxisLabelOffset + 25}\" text-anchor=\"middle\" fill=\"{foregroundColor}\">{label}</text>");

            // Draw x-axis tick
            builder.AppendLine(
                $"<line x1=\"{x}\" y1=\"{EmailChartHeight - xAxisLabelOffset}\" x2=\"{x}\" y2=\"{EmailChartHeight - xAxisLabelOffset + 5}\" stroke=\"{lineColor}\" />");
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


/// <summary>
/// Multi line chart data
/// </summary>
public class MultilineChartData
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