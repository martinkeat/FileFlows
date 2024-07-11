using System.Text;
using System.Text.Json;
using System.Web;
using FileFlows.DataLayer.DatabaseConnectors;
using FileFlows.Plugin;
using FileFlows.Shared.Models;

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
    public virtual ReportPeriod? DefaultReportPeriod => ReportPeriod.Last31Days;

    /// <summary>
    /// Gets if this report supports flow selection
    /// </summary>
    public virtual ReportSelection FlowSelection => ReportSelection.None;

    /// <summary>
    /// Gets if this report supports library selection
    /// </summary>
    public virtual ReportSelection LibrarySelection => ReportSelection.None;

    /// <summary>
    /// Gets if this report supports node selection
    /// </summary>
    public virtual ReportSelection NodeSelection => ReportSelection.None;

    /// <summary>
    /// Gets if the IO Direction is shown in this report
    /// </summary>
    public virtual bool Direction => false;

    /// <summary>
    /// Gets all reports in the system
    /// </summary>
    /// <returns>all reports in the system</returns>
    public static List<Report> GetReports()
    {
        return
        [
            new FlowElementExecution(), new LibrarySavings(),
            new Codecs(), new Languages(), new FilesProcessed(),
            new ProcessingSummary()
            //new NodeProcessing()
        ];
    }

    /// <summary>
    /// Generates the report and returns the HTML
    /// </summary>
    /// <param name="model">the model for the report</param>
    /// <param name="emailing">if this report is being emailed</param>
    /// <returns>the reports generated HTML</returns>
    public abstract Task<Result<string>> Generate(Dictionary<string, object> model, bool emailing);

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
        var libraryUids = GetUids("Library", model);
        if (libraryUids.Count > 0)
            sql += $" and {Wrap("LibraryUid")} in ({string.Join(", ", libraryUids.Select(x => $"'{x}'"))})";
    }

    /// <summary>
    /// Adds the flows to the SQL command
    /// </summary>
    /// <param name="model">the model passed to the report</param>
    /// <param name="sql">the SQL to update</param>
    protected void AddFlowsToSql(Dictionary<string, object> model, ref string sql)
    {
        var uids = GetUids("Flow", model);
        if (uids.Count > 0)
            sql += $" and {Wrap("FlowUid")} in ({string.Join(", ", uids.Select(x => $"'{x}'"))})";
    }

    /// <summary>
    /// Adds the nodes to the SQL command
    /// </summary>
    /// <param name="model">the model passed to the report</param>
    /// <param name="sql">the SQL to update</param>
    protected void AddNodesToSql(Dictionary<string, object> model, ref string sql)
    {
        var nodeUids = GetUids("Node", model).Where(x => x != null).ToArray();
        if (nodeUids.Length > 0)
            sql += $" and {Wrap("NodeUid")} in ({string.Join(", ", nodeUids.Select(x => $"'{x}'"))})";
    }

    /// <summary>
    /// Gets the period
    /// </summary>
    /// <param name="model">the model passed into the report</param>
    /// <returns>the period</returns>
    protected (DateTime? StartUtc, DateTime? EndUtc) GetPeriod(Dictionary<string, object> model)
    {
        if(model.TryGetValue("StartUtc", out var oStartUtc) && oStartUtc is DateTime startUtc &&
           model.TryGetValue("EndUtc", out var oEndUtc) && oEndUtc is DateTime endUtc)
            return (startUtc, endUtc);
        
        if (model.TryGetValue("Period", out var period) == false || period is not JsonElement jsonElement)
            return (null, null);
        if (jsonElement.ValueKind != JsonValueKind.Object)
            return (null, null);

        var start = jsonElement.GetProperty("Start").GetDateTime().ToUniversalTime();
        var end = jsonElement.GetProperty("End").GetDateTime().ToUniversalTime();
        return (start, end);
    }

    /// <summary>
    /// Gets the report direction
    /// </summary>
    /// <param name="model">the model passed into the report</param>
    /// <returns>the report direction</returns>
    protected ReportDirection GetDirection(Dictionary<string, object> model)
    {
        if (model.TryGetValue("Direction", out var value) == false || value is not JsonElement je)
            return default;
        if (je.ValueKind == JsonValueKind.Number)
            return (ReportDirection)(object)je.GetInt32();
        return default;
    }
    
    // /// <summary>
    // /// Gets an enum
    // /// </summary>
    // /// <param name="model">the model passed into the report</param>
    // /// <returns>the period</returns>
    // protected T? GetEnumValue<T>(Dictionary<string, object> model, string name) where T : Enum
    // {
    //     if (model.TryGetValue(name, out var value) == false || value is not JsonElement je)
    //         return default;
    //     if (je.ValueKind == JsonValueKind.Number)
    //         return (T)(object)je.GetInt32();
    //     return default;
    // }

    /// <summary>
    /// Gets the selected library UIDs
    /// </summary>
    /// <param name="key">the key in the model</param>
    /// <param name="model">the model passed into the report</param>
    /// <returns>the selected library UIDs</returns>
    protected List<Guid?> GetUids(string key, Dictionary<string, object> model)
    {
        if (model.TryGetValue(key, out var value) == false)
            return [];
        if (value is JsonElement je)
        {
            if (je.ValueKind == JsonValueKind.Array)
            {
                var guids = new List<Guid?>();
                foreach (var element in je.EnumerateArray())
                {
                    if(element.ValueKind == JsonValueKind.Null)
                        guids.Add(null);
                    else if (element.TryGetGuid(out var guid))
                        guids.Add(guid);
                }

                return guids;
            }
        }

        return [];
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
    protected string GenerateSvgBarChart<T>(Dictionary<object, T> data, string? yAxisLabel = null, string? yAxisFormatter = null, bool jsVersion = true)
        where T : struct, IConvertible
    {
        var list = data?.ToList();
        if (list?.Any() != true)
            return string.Empty;

        if (jsVersion)
            return "<input type=\"hidden\" class=\"report-bar-chart-data\" value=\"" + HttpUtility.HtmlEncode(
                JsonSerializer.Serialize(
                    new
                    {
                        data,
                        yAxisFormatter
                    })) + "\" />";

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

            //string yLabel = yAxisFormatter == null ? $"{value:F0}" : yAxisFormatter(value);
            string yLabel = $"{value:F0}";
            
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

}