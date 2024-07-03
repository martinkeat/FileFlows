using System.Web;

namespace FileFlows.DataLayer.Reports.Helpers;

/// <summary>
/// Report summary box for small single value information
/// </summary>
public class ReportSummaryBox
{
    /// <summary>
    /// Generates a report summary box
    /// </summary>
    /// <param name="title">the title</param>
    /// <param name="value">the value to display</param>
    /// <param name="icon">the icon to show</param>
    /// <param name="cssClass">the CSS class</param>
    /// <returns>the HTML of the report summary box</returns>
    public static string Generate(string title, string value, string icon, string cssClass)
        => $"<div class=\"report-summary-box {cssClass}\">" +
           $"<span class=\"icon\"><i class=\"{icon}\"></i></span>" +
           $"<span class=\"title\">{HttpUtility.HtmlEncode(title)}</span>" +
           $"<span class=\"value\">{HttpUtility.HtmlEncode(value)}</span>" +
           "</div>";
}