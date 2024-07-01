using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Web;
using FileFlows.DataLayer.DatabaseConnectors;
using FileFlows.Plugin;
using FileFlows.Shared.Models;
using Humanizer;

namespace FileFlows.DataLayer.Reports;

/// <summary>
/// A report to run against the FileFlows data
/// </summary>
public abstract class Report
{
    /// <summary>
    /// Gets this reports UID
    /// </summary>
    public abstract Guid Uid { get; }

    /// <summary>
    /// Gets this reports name
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Gets this reports description
    /// </summary>
    public abstract string Description { get; }

    /// <summary>
    /// Gets this reports icon
    /// </summary>
    public abstract string Icon { get; }

    /// <summary>
    /// Gets the default report period for this report if it supports a period
    /// </summary>
    public virtual ReportPeriod? DefaultReportPeriod => ReportPeriod.Any;

    /// <summary>
    /// Gets if this report supports flow selection
    /// </summary>
    public virtual ReportSelection FlowSelection => ReportSelection.None;

    /// <summary>
    /// Gets if this report supports library selection
    /// </summary>
    public virtual ReportSelection LibrarySelection => ReportSelection.None;

    /// <summary>
    /// Gets all reports in the system
    /// </summary>
    /// <returns>all reports in the system</returns>
    public static List<Report> GetReports()
    {
        return
        [
            new FlowElementExecution(), new LibrarySavings(),
            new Codecs(), new Languages(), new FilesProcessed()
        ];
    }

    /// <summary>
    /// Generates the report and returns the HTML
    /// </summary>
    /// <param name="model">the model for the report</param>
    /// <returns>the reports generated HTML</returns>
    public abstract Task<Result<string>> Generate(Dictionary<string, object> model);

    /// <summary>
    /// Gets the database connection
    /// </summary>
    /// <returns>the database connection</returns>
    protected Task<DatabaseConnection> GetDb()
        => DatabaseAccessManager.Instance.GetDb();

    /// <summary>
    /// Wraps a field name in the character supported by this database
    /// </summary>
    /// <param name="name">the name of the field to wrap</param>
    /// <returns>the wrapped field name</returns>
    protected string Wrap(string name)
        => DatabaseAccessManager.Instance.WrapFieldName(name);


    /// <summary>
    /// Converts a datetime to a string for the database in quotes
    /// </summary>
    /// <param name="date">the date to convert</param>
    /// <returns>the converted data as a string</returns>
    public string FormatDateQuoted(DateTime date)
        => DatabaseAccessManager.Instance.FormatDateQuoted(date);

    /// <summary>
    /// Adds the period to the SQL command
    /// </summary>
    /// <param name="model">the model passed to the report</param>
    /// <param name="sql">the SQL to update</param>
    protected void AddPeriodToSql(Dictionary<string, object> model, ref string sql)
    {
        (DateTime? startUtc, DateTime? endUtc) = GetPeriod(model);
        if (startUtc != null && endUtc != null)
            sql +=
                $" and {Wrap("ProcessingEnded")} between {FormatDateQuoted(startUtc.Value)} and {FormatDateQuoted(endUtc.Value)}";
    }

    /// <summary>
    /// Adds the libraries to the SQL command
    /// </summary>
    /// <param name="model">the model passed to the report</param>
    /// <param name="sql">the SQL to update</param>
    protected void AddLibrariesToSql(Dictionary<string, object> model, ref string sql)
    {
        var libraryUids = GetLibraryUids(model);
        if (libraryUids.Count > 0)
            sql += $" and {Wrap("LibraryUid")} in ({string.Join(", ", libraryUids.Select(x => $"'{x}'"))})";
    }

    /// <summary>
    /// Gets the period
    /// </summary>
    /// <param name="model">the model passed into the report</param>
    /// <returns>the period</returns>
    protected (DateTime? StartUtc, DateTime? EndUtc) GetPeriod(Dictionary<string, object> model)
    {
        if (model.TryGetValue("Period", out var period) == false || period is not JsonElement jsonElement)
            return (null, null);
        if (jsonElement.ValueKind != JsonValueKind.Object)
            return (null, null);

        var start = jsonElement.GetProperty("Start").GetDateTime().ToUniversalTime();
        var end = jsonElement.GetProperty("End").GetDateTime().ToUniversalTime();
        return (start, end);
    }

    /// <summary>
    /// Gets the period
    /// </summary>
    /// <param name="model">the model passed into the report</param>
    /// <returns>the period</returns>
    protected T? GetEnumValue<T>(Dictionary<string, object> model, string name) where T : Enum
    {
        if (model.TryGetValue(name, out var value) == false || value is not JsonElement je)
            return default;
        if (je.ValueKind == JsonValueKind.Number)
            return (T)(object)je.GetInt32();
        return default;
    }

    /// <summary>
    /// Gets the selected library UIDs
    /// </summary>
    /// <param name="model">the model passed into the report</param>
    /// <returns>the selected library UIDs</returns>
    protected List<Guid> GetLibraryUids(Dictionary<string, object> model)
    {
        if (model.TryGetValue("Library", out var libraries) == false)
            return [];
        if (libraries is JsonElement je)
        {
            if (je.ValueKind == JsonValueKind.Array)
            {
                var guids = new List<Guid>();
                foreach (var element in je.EnumerateArray())
                {
                    if (element.TryGetGuid(out var guid))
                    {
                        guids.Add(guid);
                    }
                }

                return guids;
            }
        }

        return [];
    }


    /// <summary>
    /// Generates an HTML table from a collection of data.
    /// </summary>
    /// <param name="data">The collection of data to generate the HTML table from.</param>
    /// <param name="dontWrap">If the table should not be wrapped with a table-container class</param>
    /// <returns>An HTML string representing the table.</returns>
    protected string GenerateHtmlTable(IEnumerable<dynamic> data, bool dontWrap = false)
    {
        var list = data?.ToList();
        if (list?.Any() != true)
            return string.Empty;

        var sb = new StringBuilder();
        if(dontWrap == false)
            sb.Append("<div class=\"table-container\">");
        sb.Append("<table class=\"report-table table\">");

        // Add table headers
        sb.Append("<thead>");
        sb.Append("<tr>");
        var firstItem = list.FirstOrDefault();
        if (firstItem != null)
        {
            var properties = ((Type)firstItem.GetType()).GetProperties();
            foreach (var prop in properties)
            {
                sb.AppendFormat("<th><span>{0}</span></th>",
                    System.Net.WebUtility.HtmlEncode(prop.Name.Humanize(LetterCasing.Title)));
            }
        }

        sb.Append("</tr>");
        sb.Append("</thead>");
        sb.Append("<tbody>");

        // Add table rows
        foreach (var item in list)
        {
            sb.Append("<tr>");
            var properties = ((Type)item.GetType()).GetProperties();
            foreach (var prop in properties)
            {
                var value = prop.GetValue(item, null) ?? string.Empty;
                if (prop.Name.Contains("Percentage"))
                {
                    var percent = (double)value;
                    sb.Append("<td>");
                    sb.Append($"<div class=\"percentage {(percent > 100 ? "over-100" : "")}\">");
                    sb.Append($"<div class=\"bar\" style=\"width:{Math.Min(percent, 100)}%\"></div>");
                    sb.Append($"<span class=\"label\">{(percent / 100).ToString("P1")}<span>");
                    sb.Append("</div>");
                    sb.Append("</td>");
                }
                else if (value is int or long)
                {
                    sb.AppendFormat("<td>{0:N0}</td>", value); // Format with thousands separator, no decimals
                }
                else if (value is IFormattable numericValue)
                {
                    // Format numeric value with thousands separator in current culture
                    sb.AppendFormat("<td>{0}</td>", numericValue.ToString("N", CultureInfo.CurrentCulture));
                }
                else
                {
                    sb.AppendFormat("<td>{0}</td>", System.Net.WebUtility.HtmlEncode(value.ToString()));
                }
            }

            sb.Append("</tr>");
        }

        sb.Append("</tbody>");
        sb.Append("</table>");
        if(dontWrap == false)
            sb.Append("</div>");
        return sb.ToString();
    }

    private readonly string[] COLORS =
    [
        "#007bff", "#28a745", "#ffc107", "#dc3545", "#17a2b8",
        "#fd7e14", "#6f42c1", "#20c997", "#6610f2", "#e83e8c",
        "#ff6347", "#4682b4", "#9acd32", "#8a2be2", "#ff4500",
        "#2e8b57", "#d2691e", "#ff1493", "#00ced1", "#b22222",
        "#daa520", "#5f9ea0", "#7f007f", "#808000", "#3cb371"
    ];

    /// <summary>
    /// Generates an SVG pie chart from a collection of data with interactive pop-out slices on hover.
    /// </summary>
    /// <param name="data">The collection of data to generate the SVG pie chart from.</param>
    /// <param name="jsVersion">If the javascript version should be used</param>
    /// <returns>An SVG string representing the pie chart.</returns>
    protected string GenerateSvgPieChart(Dictionary<string, int> data, bool jsVersion = true)
    {
        var list = data?.ToList();
        if (list?.Any() != true)
            return string.Empty;
        if (jsVersion)
            return "<input type=\"hidden\" class=\"report-pie-chart-data\" value=\"" + HttpUtility.HtmlEncode(JsonSerializer.Serialize(data)) + "\" />";

        const int chartWidth = 600;
        const int chartHeight = 500;
        const int radius = 200;
        const int centerX = (chartWidth - 100) / 2;
        const int centerY = chartHeight / 2;
        const int legendX = chartWidth - 120; // Positioning the legend to the right of the pie chart
        const int legendY = 20;
        const int legendSpacing = 20;
        const int legendColorBoxSize = 10;

        double total = list.Sum(item => (double)item.Value);
        double startAngle = 270;

        StringBuilder svgBuilder = new StringBuilder();
        svgBuilder.AppendLine(
            $"<svg class=\"pie-chart\" width=\"{chartWidth + 200}\" height=\"{chartHeight}\" viewBox=\"0 0 {chartWidth + 200} {chartHeight}\" xmlns=\"http://www.w3.org/2000/svg\">");


        var legendEntries = new List<(string Label, string Color, double Percentage)>();

        int count = 0;
        foreach (var item in list)
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

            svgBuilder.AppendLine(
                $"<path data-title=\"{System.Net.WebUtility.HtmlEncode(tooltip)}\" class=\"slice\" d=\"M{centerX},{centerY} L{x1},{y1} A{radius},{radius} 0 {largeArcFlag},1 {x2},{y2} Z\" fill=\"{color}\">" +
                "</path>");

            legendEntries.Add((label, color, percentage));

            startAngle = endAngle;
            ++count;
        }

        // Add legend
        int legendYPosition = legendY;
        foreach (var entry in legendEntries)
        {
            svgBuilder.AppendLine(
                $"<rect x=\"{legendX}\" y=\"{legendYPosition}\" width=\"{legendColorBoxSize}\" height=\"{legendColorBoxSize}\" fill=\"{entry.Color}\" />");
            svgBuilder.AppendLine(
                $"<text x=\"{legendX + legendColorBoxSize + 5}\" y=\"{legendYPosition + legendColorBoxSize}\">{entry.Label} ({entry.Percentage:F2}%)</text>");
            legendYPosition += legendSpacing;
        }

        svgBuilder.AppendLine("</svg>");
        return svgBuilder.ToString();
    }

    /// <summary>
    /// Generates an SVG bar chart from a collection of data with a single color for all bars.
    /// </summary>
    /// <typeparam name="T">The type of the numeric values in the data dictionary.</typeparam>
    /// <param name="data">The collection of data to generate the SVG bar chart from.</param>
    /// <param name="yAxisLabel">Optional label for the y-axis.</param>
    /// <param name="yAxisFormatter">Optional formatter for the y-axis labels</param>
    /// <param name="jsVersion">If the javascript version should be used</param>
    /// <returns>An SVG string representing the bar chart.</returns>
    protected string GenerateSvgBarChart<T>(Dictionary<object, T> data, string? yAxisLabel = null, Func<double, string>? yAxisFormatter = null, bool jsVersion = true)
        where T : struct, IConvertible
    {
        var list = data?.ToList();
        if (list?.Any() != true)
            return string.Empty;

        if (jsVersion)
            return "<input type=\"hidden\" class=\"report-bar-chart-data\" value=\"" + HttpUtility.HtmlEncode(JsonSerializer.Serialize(data)) + "\" />";

        const int chartWidth = 900; // Increased chart width
        const int chartHeight = 600; // Increased chart height
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
        double ConvertToDouble(T value) => Convert.ToDouble(value);
        double maxValue = list.Max(item => ConvertToDouble(item.Value));

        // Calculate bar width based on available space and number of bars
        int totalBars = list.Count;
        int availableWidth = chartWidth - 2 * chartStartX;
        int actualBarWidth = Math.Min(barWidth, (availableWidth - (totalBars - 1) * barSpacing) / totalBars);

        // Calculate the total width needed by the bars and spacing
        int totalWidthNeeded = totalBars * (actualBarWidth + barSpacing) - barSpacing;
        int startX = (availableWidth - totalWidthNeeded) / 2 + chartStartX; // Starting point for the first bar

        StringBuilder svgBuilder = new StringBuilder();
        svgBuilder.AppendLine(
            $"<svg class=\"bar-chart\" width=\"{chartWidth}\" height=\"{chartHeight}\" viewBox=\"0 0 {chartWidth} {chartHeight}\" xmlns=\"http://www.w3.org/2000/svg\">");

        // Draw background
        svgBuilder.AppendLine(
            $"<rect x=\"{chartStartX}\" y=\"{chartStartY}\" width=\"{chartWidth - 2 * chartStartX}\" height=\"{chartHeight - chartStartY - xAxisLabelOffset}\" fill=\"{backgroundColor}\" />");

        // Draw y-axis labels and grid lines
        int yAxisHeight = chartHeight - xAxisLabelOffset - chartStartY;
        for (int i = 0; i <= yAxisLabelFrequency; i++)
        {
            double value = (maxValue / yAxisLabelFrequency) * i;
            int y = chartHeight - xAxisLabelOffset - (int)((value / maxValue) * yAxisHeight);

            // Draw grid line
            svgBuilder.AppendLine(
                $"<line x1=\"{chartStartX}\" y1=\"{y}\" x2=\"{chartWidth - chartStartX}\" y2=\"{y}\" stroke=\"#555\" />");

            string yLabel = yAxisFormatter == null ? $"{value:F0}" : yAxisFormatter(value);
            
            // Draw y-axis label
            svgBuilder.AppendLine(
                $"<text x=\"{chartStartX - yAxisLabelOffset + 30}\" y=\"{y + 5}\" text-anchor=\"end\" fill=\"#fff\">{yLabel}</text>");

            // Draw y-axis tick
            svgBuilder.AppendLine(
                $"<line x1=\"{chartStartX - 5}\" y1=\"{y}\" x2=\"{chartStartX}\" y2=\"{y}\" stroke=\"#fff\" />");
        }

        // Draw y-axis main label if provided, rotated 90 degrees
        if (!string.IsNullOrEmpty(yAxisLabel))
        {
            svgBuilder.AppendLine(
                $"<text x=\"{chartStartX - yAxisLabelOffset - 30}\" y=\"{chartHeight / 2}\" text-anchor=\"middle\" fill=\"#fff\" transform=\"rotate(-90, {chartStartX - yAxisLabelOffset - 30}, {chartHeight / 2})\">{yAxisLabel}</text>");
        }

        // Draw bars
        int x = startX;
        int labelInterval = Math.Max(1, totalBars / 20); // Show approximately 20 labels on the x-axis
        for (int i = 0; i < totalBars; i++)
        {
            var item = list[i];
            string label;
            if (item.Key is DateTime dt)
                label = dt.ToString("d MMMM");
            else
                label = item.Key?.ToString() ?? string.Empty;
            double value = ConvertToDouble(item.Value);
            int barHeight = (int)((value / maxValue) * yAxisHeight);

            // Bar coordinates
            int y = chartHeight - xAxisLabelOffset - barHeight;

            // Bar color
            string color = "#007bff"; // Blue color

            // Tooltip
            string tooltip = $"{label}: {value}";

            // Draw bar
            svgBuilder.AppendLine(
                $"<rect class=\"bar-chart-bar\" x=\"{x}\" y=\"{y}\" width=\"{actualBarWidth}\" height=\"{barHeight}\" fill=\"{color}\" data-title=\"{System.Net.WebUtility.HtmlEncode(tooltip)}\" />");

            // Label below the bar (rotated 90 degrees)
            if (i % labelInterval == 0)
            {
                svgBuilder.AppendLine(
                    $"<g transform=\"translate({x + actualBarWidth / 2}, {chartHeight - xAxisLabelOffset + 10})\">" +
                    $"<text transform=\"rotate(90)\" dy=\"0.4em\" fill=\"#fff\">{label}</text>" +
                    "</g>");

                // Draw x-axis tick
                svgBuilder.AppendLine(
                    $"<line x1=\"{x + actualBarWidth / 2}\" y1=\"{chartHeight - xAxisLabelOffset}\" x2=\"{x + actualBarWidth / 2}\" y2=\"{chartHeight - xAxisLabelOffset + 5}\" stroke=\"#fff\" />");
            }

            x += actualBarWidth + barSpacing;
        }

        svgBuilder.AppendLine("</svg>");
        return svgBuilder.ToString();
    }
    
    /// <summary>
/// Generates an SVG line chart from a collection of data with a single color for all lines.
/// </summary>
/// <typeparam name="T">The type of the numeric values in the data dictionary.</typeparam>
/// <param name="data">The collection of data to generate the SVG line chart from.</param>
/// <param name="yAxisLabel">Optional label for the y-axis.</param>
/// <param name="yAxisFormatter">Optional formatter for the y-axis labels</param>
/// <param name="jsVersion">If the javascript version should be used</param>
/// <returns>An SVG string representing the line chart.</returns>
protected string GenerateSvgLineChart<T>(Dictionary<object, T> data, string? yAxisLabel = null, Func<double, string>? yAxisFormatter = null, bool jsVersion = true)
    where T : struct, IConvertible
{
    var list = data?.ToList();
    if (list?.Any() != true)
        return string.Empty;
    
    if (jsVersion)
        return "<input type=\"hidden\" class=\"report-line-chart-data\" value=\"" + HttpUtility.HtmlEncode(JsonSerializer.Serialize(data)) + "\" />";

    const int chartWidth = 900; // Increased chart width
    const int chartHeight = 600; // Increased chart height
    const int lineThickness = 3;
    int chartStartX = string.IsNullOrWhiteSpace(yAxisLabel) ? 60 : 100; // Adjusted based on yAxisLabel presence
    const int chartStartY = 20; // The top of the y-axis where it starts, from the top
    int xAxisLabelOffset =
        string.IsNullOrWhiteSpace(yAxisLabel) ? 110 : 150; // Adjusted based on yAxisLabel presence
    const int yAxisLabelOffset = 40; // Fixed offset when yAxisLabel is present
    const int yAxisLabelFrequency = 10; // Change as needed to control the number of labels
    const string backgroundColor = "#161616"; // Dark background color

    // Convert values to double for processing
    double ConvertToDouble(T value) => Convert.ToDouble(value);
    double maxValue = list.Max(item => ConvertToDouble(item.Value));

    // Calculate the total width needed by the lines and spacing
    int totalLines = list.Count;
    int availableWidth = chartWidth - 2 * chartStartX;
    int startX = chartStartX; // Starting point for the first line

    StringBuilder svgBuilder = new StringBuilder();
    svgBuilder.AppendLine(
        $"<svg class=\"line-chart\" width=\"{chartWidth}\" height=\"{chartHeight}\" viewBox=\"0 0 {chartWidth} {chartHeight}\" xmlns=\"http://www.w3.org/2000/svg\">");

    // Draw background
    svgBuilder.AppendLine(
        $"<rect x=\"{chartStartX}\" y=\"{chartStartY}\" width=\"{chartWidth - 2 * chartStartX}\" height=\"{chartHeight - chartStartY - xAxisLabelOffset}\" fill=\"{backgroundColor}\" />");

    // Draw y-axis labels and grid lines
    int yAxisHeight = chartHeight - xAxisLabelOffset - chartStartY;
    for (int i = 0; i <= yAxisLabelFrequency; i++)
    {
        double value = (maxValue / yAxisLabelFrequency) * i;
        int y = chartHeight - xAxisLabelOffset - (int)((value / maxValue) * yAxisHeight);

        // Draw grid line
        svgBuilder.AppendLine(
            $"<line x1=\"{chartStartX}\" y1=\"{y}\" x2=\"{chartWidth - chartStartX}\" y2=\"{y}\" stroke=\"#555\" />");

        string yLabel = yAxisFormatter == null ? $"{value:F0}" : yAxisFormatter(value);
        // Draw y-axis label
        svgBuilder.AppendLine(
            $"<text x=\"{chartStartX - yAxisLabelOffset}\" y=\"{y + 5}\" text-anchor=\"end\" fill=\"#fff\">{yLabel}</text>");

        // Draw y-axis tick
        svgBuilder.AppendLine(
            $"<line x1=\"{chartStartX - 5}\" y1=\"{y}\" x2=\"{chartStartX}\" y2=\"{y}\" stroke=\"#fff\" />");
    }

    // Draw y-axis main label if provided, rotated 90 degrees
    if (!string.IsNullOrEmpty(yAxisLabel))
    {
        svgBuilder.AppendLine(
            $"<text x=\"{chartStartX - yAxisLabelOffset - 30}\" y=\"{chartHeight / 2}\" text-anchor=\"middle\" fill=\"#fff\" transform=\"rotate(-90, {chartStartX - yAxisLabelOffset - 30}, {chartHeight / 2})\">{yAxisLabel}</text>");
    }

    // Draw lines
    var linePoints = new List<string>();
    int labelInterval = Math.Max(1, totalLines / 20); // Show approximately 20 labels on the x-axis
    double xStep = (double)availableWidth / (totalLines - 1); // Calculate the step for evenly spaced x-coordinates
    for (int i = 0; i < totalLines; i++)
    {
        var item = list[i];
        string label;
        if (item.Key is DateTime dt)
            label = dt.ToString("d MMMM");
        else
            label = item.Key?.ToString() ?? string.Empty;
        double value = ConvertToDouble(item.Value);
        int y = chartHeight - xAxisLabelOffset - (int)((value / maxValue) * yAxisHeight);

        // Add point to line path
        int x = (int)(startX + i * xStep);
        linePoints.Add($"{x},{y}");

        // Label below the line point (rotated 90 degrees)
        if (i % labelInterval == 0)
        {
            svgBuilder.AppendLine(
                $"<g transform=\"translate({x}, {chartHeight - xAxisLabelOffset + 10})\">" +
                $"<text transform=\"rotate(90)\" dy=\"0.4em\" fill=\"#fff\">{label}</text>" +
                "</g>");

            // Draw x-axis tick
            svgBuilder.AppendLine(
                $"<line x1=\"{x}\" y1=\"{chartHeight - xAxisLabelOffset}\" x2=\"{x}\" y2=\"{chartHeight - xAxisLabelOffset + 5}\" stroke=\"#fff\" />");
        }
    }

    // Draw the line connecting points
    svgBuilder.AppendLine(
        $"<polyline points=\"{string.Join(" ", linePoints)}\" fill=\"none\" stroke=\"#007bff\" stroke-width=\"{lineThickness}\" />");

    svgBuilder.AppendLine("</svg>");
    return svgBuilder.ToString();
}

}