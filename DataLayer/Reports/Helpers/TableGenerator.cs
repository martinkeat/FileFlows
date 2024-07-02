using System.Globalization;
using System.Text;

namespace FileFlows.DataLayer.Reports.Helpers;

/// <summary>
/// Table generator used by reports
/// </summary>
public class TableGenerator
{
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
}