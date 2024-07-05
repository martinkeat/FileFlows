using System.Globalization;
using System.Text;
using System.Web;
using Humanizer;

namespace FileFlows.DataLayer.Reports.Helpers;

/// <summary>
/// Table generator used by reports
/// </summary>
public class TableGenerator
{
    /// <summary>
    /// The rows that can appear in a minimum table
    /// </summary>
    public const int MIN_TABLE_ROWS = 6;
    
    /// <summary>
    /// Generates an HTML table from a collection of data.
    /// </summary>
    /// <param name="columns">the name of the columns</param>
    /// <param name="data">The collection of data to generate the HTML table from.</param>
    /// <param name="dontWrap">If the table should not be wrapped with a table-container class</param>
    /// <returns>An HTML string representing the table.</returns>
    public static string Generate(string[] columns, object[][] data, bool dontWrap = false)
    {
        if (data.Any() != true)
            return string.Empty;

        var sb = new StringBuilder();
        if(dontWrap == false)
            sb.Append("<div class=\"table-container\">");
        sb.Append("<table class=\"report-table table\">");

        // Add table headers
        sb.Append("<thead>");
        sb.Append("<tr>");
        foreach(var column in columns)
        {
            sb.AppendFormat("<th><span>{0}</span></th>",
                System.Net.WebUtility.HtmlEncode(column));
        }

        sb.Append("</tr>");
        sb.Append("</thead>");
        sb.Append("<tbody>");

        // Add table rows
        foreach (var row in data)
        {
            sb.Append("<tr>");
            foreach (var col in row)
            {
                if (col is int or long)
                {
                    sb.AppendFormat("<td>{0:N0}</td>", col); // Format with thousands separator, no decimals
                }
                else if (col is DateTime dt)
                {
                    sb.AppendFormat("<td>{0:d MMMM yyyy}</td>", dt);
                }
                else if (col is IFormattable numericValue)
                {
                    // Format numeric value with thousands separator in current culture
                    sb.AppendFormat("<td>{0}</td>", numericValue.ToString("N", CultureInfo.CurrentCulture));
                }
                else
                {
                    sb.AppendFormat("<td>{0}</td>", System.Net.WebUtility.HtmlEncode(col.ToString()));
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
    
    
    /// <summary>
    /// Generates an HTML table from a collection of data.
    /// </summary>
    /// <param name="data">The collection of data to generate the HTML table from.</param>
    /// <param name="dontWrap">If the table should not be wrapped with a table-container class</param>
    /// <returns>An HTML string representing the table.</returns>
    public static string Generate(IEnumerable<dynamic> data, bool dontWrap = false)
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
    
    
    /// <summary>
    /// Generates an HTML table from a collection of data.
    /// </summary>
    /// <param name="title">the title of the table</param>
    /// <param name="columns">the name of the columns</param>
    /// <param name="data">The collection of data to generate the HTML table from.</param>
    /// <returns>An HTML string representing the table.</returns>
    public static string GenerateMinimumTable(string title, string[] columns, object[][] data)
    {
        if (data.Any() != true)
            return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("<div class=\"min-table\">");
        sb.AppendLine($"<span class=\"title\">{HttpUtility.HtmlEncode(title)}</span>");
        sb.AppendLine("<table class=\"table\">");

        // Add table headers
        sb.Append("<thead>");
        sb.Append("<tr>");
        foreach(var column in columns)
        {
            sb.AppendFormat("<th><span>{0}</span></th>",
                System.Net.WebUtility.HtmlEncode(column));
        }

        sb.Append("</tr>");
        sb.Append("</thead>");
        
        sb.Append("<tbody>");

        // Add table rows
        foreach (var row in data)
        {
            sb.Append("<tr>");
            foreach (var col in row)
            {
                if (col is int or long)
                {
                    sb.AppendFormat("<td>{0:N0}</td>", col); // Format with thousands separator, no decimals
                }
                else if (col is DateTime dt)
                {
                    sb.AppendFormat("<td>{0:d MMMM yyyy}</td>", dt);
                }
                else if (col is IFormattable numericValue)
                {
                    // Format numeric value with thousands separator in current culture
                    sb.AppendFormat("<td>{0}</td>", numericValue.ToString("N", CultureInfo.CurrentCulture));
                }
                else
                {
                    sb.AppendFormat("<td>{0}</td>", System.Net.WebUtility.HtmlEncode(col.ToString()));
                }
            }

            sb.Append("</tr>");
        }

        sb.Append("</tbody>");
        sb.Append("</table>");
        sb.Append("</div>");
        return sb.ToString();
    }
    

}