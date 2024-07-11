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
    /// <param name="emailing">If this table is being emailed</param>
    /// <returns>An HTML string representing the table.</returns>
    public static string Generate(IEnumerable<dynamic> data, bool dontWrap = false, bool emailing = false)
    {
        var list = data?.ToList();
        if (list?.Any() != true)
            return string.Empty;

        var sb = new StringBuilder();
        if(dontWrap == false)
            sb.AppendLine("<div class=\"table-container\">");
        
        if(emailing)
            sb.AppendLine("<table class=\"report-table large-table\">");
        else
            sb.AppendLine("<table class=\"report-table table large-table\">");

        // Add table headers
        sb.AppendLine("<thead>");
        sb.AppendLine("<tr>");
        var firstItem = list.FirstOrDefault();
        if (firstItem != null)
        {
            var properties = ((Type)firstItem.GetType()).GetProperties();
            int count = 0;
            foreach (var prop in properties)
            {
                sb.AppendFormat("<th{1}><span>{0}</span></th>",
                    System.Net.WebUtility.HtmlEncode(prop.Name.Humanize(LetterCasing.Title)),
                    emailing && count == 0 ? " style=\"text-align:left\"" : string.Empty);
                ++count;
            }
        }

        sb.AppendLine("</tr>");
        sb.AppendLine("</thead>");
        sb.AppendLine("<tbody>");

        // Add table rows
        foreach (var item in list)
        {
            sb.AppendLine("<tr>");
            var properties = ((Type)item.GetType()).GetProperties();
            int count = 0;
            foreach (var prop in properties)
            {
                var value = prop.GetValue(item, null) ?? string.Empty;
                sb.Append("<td");
                if (emailing && count == 0)
                    sb.Append(" style=\"text-align:left\"");
                sb.Append('>');
                if (prop.Name.Contains("Percentage"))
                {
                    var percent = (double)value;
                    sb.Append($"<div class=\"percentage {(percent > 100 ? "over-100" : "")}\">");
                    sb.Append($"<div class=\"bar\" style=\"width:{Math.Min(percent, 100)}%\"></div>");
                    sb.Append($"<span class=\"label\">{(percent / 100).ToString("P1")}<span>");
                    sb.Append("</div>");
                }
                else if (value is int or long)
                {
                    sb.Append(string.Format("{0:N0}", value)); // Format with thousands separator, no decimals
                }
                else if (value is IFormattable numericValue)
                {
                    // Format numeric value with thousands separator in current culture
                    sb.Append($"{numericValue.ToString("N", CultureInfo.CurrentCulture)}");
                }
                else
                {
                    sb.Append(System.Net.WebUtility.HtmlEncode(value.ToString()));
                }
                sb.AppendLine("</td>");

                ++count;
            }

            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</tbody>");
        sb.AppendLine("</table>");
        if(dontWrap == false)
            sb.AppendLine("</div>");
        return sb.ToString();
    }
    
    
    /// <summary>
    /// Generates an HTML table from a collection of data.
    /// </summary>
    /// <param name="title">the title of the table</param>
    /// <param name="columns">the name of the columns</param>
    /// <param name="data">The collection of data to generate the HTML table from.</param>
    /// <param name="widths">Optional custom widths for the columns</param>
    /// <param name="emailing">If the table will be emailed</param>
    /// <returns>An HTML string representing the table.</returns>
    public static string GenerateMinimumTable(string title, string[] columns, object[][] data, string[]? widths = null, bool emailing = false)
    {
        if (data.Any() != true)
            return string.Empty;

        var sb = new StringBuilder();
        if (emailing)
        {
            sb.AppendLine("<div>");
            sb.AppendLine($"<span class=\"table-title\" style=\"{ReportBuilder.EmailTitleStyling}\">{HttpUtility.HtmlEncode(title)}</span>");
            sb.AppendLine("<table style=\"width:100%;text-align:left\">");
            
        }else{
            sb.AppendLine("<div class=\"min-table\">");
            sb.AppendLine($"<span class=\"title\">{HttpUtility.HtmlEncode(title)}</span>");
            sb.AppendLine("<table class=\"table\">");
        }

        // Add table headers
        sb.Append("<thead>");
        sb.Append("<tr>");
        for(int i=0;i<columns.Length;i++)
        {
            var column = columns[i];
            if (widths != null && string.IsNullOrWhiteSpace(widths[i]) == false)
                sb.AppendFormat("<th style=\"width:{1}{2}\"><span>{0}</span></th>",
                    System.Net.WebUtility.HtmlEncode(column), widths[i], emailing && i > 0 ? "text-align:center" : string.Empty);
            else
                sb.AppendFormat("<th{1}><span>{0}</span></th>",
                    System.Net.WebUtility.HtmlEncode(column),
                    emailing && i > 0 ? " style=\"text-align:center\"" : string.Empty);
        }

        sb.Append("</tr>");
        sb.Append("</thead>");
        
        sb.Append("<tbody>");

        // Add table rows
        foreach (var row in data)
        {
            sb.Append("<tr>");
            
            for(int i=0;i<row.Length;i++)
            {
                var col = row[i];
                sb.Append("<td");
                if (emailing)
                {
                    sb.Append(" class=\"min-table-td\" style=\"line-height:24px;");
                    if (i > 0)
                        sb.Append("text-align:center");
                    sb.Append('"');
                }
                sb.Append('>');
                if (col is int or long)
                {
                    sb.AppendFormat("{0:N0}</td>", col); // Format with thousands separator, no decimals
                }
                else if (col is DateTime dt)
                {
                    sb.AppendFormat("{0:d MMMM yyyy}</td>", dt);
                }
                else if (col is IFormattable numericValue)
                {
                    // Format numeric value with thousands separator in current culture
                    sb.AppendFormat("{0}</td>", numericValue.ToString("N", CultureInfo.CurrentCulture));
                }
                else
                {
                    sb.AppendFormat("{0}</td>", System.Net.WebUtility.HtmlEncode(col.ToString()));
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