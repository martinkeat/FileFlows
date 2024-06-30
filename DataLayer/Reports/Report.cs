using System.Text;
using System.Text.Json;
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
    /// Gets if this report supports flow selection
    /// </summary>
    public virtual ReportSelection FlowSelection => ReportSelection.None;
    /// <summary>
    /// Gets if this report supports library selection
    /// </summary>
    public virtual ReportSelection LibrarySelection => ReportSelection.None;

    /// <summary>
    /// Gets if this report supports a period selection
    /// </summary>
    public virtual bool PeriodSelection => false;

    /// <summary>
    /// Gets all reports in the system
    /// </summary>
    /// <returns>all reports in the system</returns>
    public static List<Report> GetReports()
    {
        return [new FlowElementExecution(), new LibrarySavings()];
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
    /// Gets the period
    /// </summary>
    /// <param name="model">the model passed into the report</param>
    /// <returns>the period</returns>
    protected (DateTime? StartUtc, DateTime? EndUtc) GetPeriod(Dictionary<string, object> model)
    {
        if (!model.TryGetValue("Period", out var period) || period is not JsonElement jsonElement) 
            return (null, null);
        if (jsonElement.ValueKind != JsonValueKind.Object) 
            return (null, null);
        
        var start = jsonElement.GetProperty("Start").GetDateTime().ToUniversalTime();
        var end = jsonElement.GetProperty("End").GetDateTime().ToUniversalTime();
        return (start, end);
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
    /// <returns>An HTML string representing the table.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the data is null.</exception>
    protected string GenerateHtmlTable(IEnumerable<dynamic> data)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        var sb = new StringBuilder();
        sb.Append("<table class=\"report-table table\">");

        // Add table headers
        sb.Append("<thead>");
        sb.Append("<tr>");
        var firstItem = data.FirstOrDefault();
        if (firstItem != null)
        {
            var properties = ((Type)firstItem.GetType()).GetProperties();
            foreach (var prop in properties)
            {
                sb.AppendFormat("<th>{0}</th>", System.Net.WebUtility.HtmlEncode(prop.Name.Humanize(LetterCasing.Title)));
            }
        }
        sb.Append("</tr>");
        sb.Append("</thead>");
        sb.Append("<tbody>");

        // Add table rows
        foreach (var item in data)
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
                else
                {
                    sb.AppendFormat("<td>{0}</td>", System.Net.WebUtility.HtmlEncode(value.ToString()));
                }
            }
            sb.Append("</tr>");
        }

        sb.Append("</tbody>");
        sb.Append("</table>");
        return sb.ToString();
    }
}