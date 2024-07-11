using System.Text.RegularExpressions;
using System.Web;
using FileFlows.Managers;
using FileFlows.Plugin;
using FileFlows.Server.Helpers;
using FileFlows.ServerShared.Models;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// Service for scheduled reports
/// </summary>
public class ScheduledReportService
{
    /// <summary>
    /// Gets a ScheduledReport by its UID
    /// </summary>
    /// <param name="uid">the UID of the Scheduled Report</param>
    /// <returns>the Scheduled Report if found, otherwise null</returns>
    public Task<ScheduledReport?> GetByUid(Guid uid)
        => new ScheduledReportManager().GetByUid(uid);
    

    /// <summary>
    /// Gets all Scheduled Reports in the system
    /// </summary>
    /// <returns>all Scheduled Reports in the system</returns>
    public Task<List<ScheduledReport>> GetAll()
        => new ScheduledReportManager().GetAll();

    /// <summary>
    /// Updates a scheduled report
    /// </summary>
    /// <param name="report">the scheduled report to update</param>
    /// <param name="auditDetails">The audit details</param>
    /// <returns>the update result</returns>
    public Task<Result<ScheduledReport>> Update(ScheduledReport report, AuditDetails? auditDetails)
        => new ScheduledReportManager().Update(report, auditDetails);

    /// <summary>
    /// Deletes the given scheduled reports
    /// </summary>
    /// <param name="uids">the UID of the scheduled reports to delete</param>
    /// <param name="auditDetails">the audit details</param>
    /// <returns>a task to await</returns>
    public Task Delete(Guid[] uids, AuditDetails auditDetails)
        => new ScheduledReportManager().Delete(uids, auditDetails);


    /// <summary>
    /// Emails a report
    /// </summary>
    /// <param name="reportName">the name of the report</param>
    /// <param name="recipients">the recipients of the report</param>
    /// <param name="subject">the subject of the report</param>
    /// <param name="reportHtml">the HTML of the report</param>
    public async Task Email(string reportName, string[] recipients, string subject, string reportHtml)
    {
        string html = @$"
<html lang=""en"">
<head>
    <title>{HttpUtility.HtmlEncode(subject)}</title>
    <style>
        .rsb-title {{
            white-space: nowrap;
        }} 
        .rsb-value {{
            white-space: nowrap;
        }}
        .min-table-td {{
            white-space: nowrap;
            overflow:hidden;
            text-overflow:ellipsis;
        }}
        .large-table {{
          width: 100%;
          text-align: center;
          line-height: 1.8rem;
          border-collapse: collapse;
          font-size: 0.9rem;
        }}
        .large-table th {{
          border-bottom: solid 1px #181a1b !important;
          font-size:1rem;
        }}
        .percentage {{    
            width: 14rem;
            height: 3rem;
            position: relative;
            background: #181a1b;
            border-radius: 10px;
            overflow: hidden;
        }}

        .report-flex-data .title {{
          margin: 0 0 0 1rem !important;
          text-align: left;
          font-size: 1.5rem;
        }}
        .report-flex-data .icon {{
          width:4rem;
        }}
        .report-flex-data .icon img {{
          padding-top:10px !important
        }}
        .report-flex-data .info-box {{
          padding: 0 1rem;
          width:10rem;  
        }}
        .report-flex-data .info-box .ib-title {{
          font-weight: 600;
          display:block;
        }}
        .report-flex-data .info-box .ib-value {{
          font-size: 1.3rem;
          display:block;
        }}
        .report-flex-data .percent {{
            width:14rem;
        }}
        .report-flex-data .percentage {{
          width: 100%;
          height: 3rem;
        }}

        @media screen and (max-width:1200px){{
            .rsb-title, .table-title, .chart-title {{
                font-size: 14px !important;
            }}
            .rsb-value {{
                font-size: 18px !important;
            }}
            .min-table-td {{
                line-height: 20px !important;
            }}
        }}
        @media screen and (max-width:1000px){{
            .rsb-title, .table-title, .chart-title {{
                font-size: 12px !important;
            }}
            .rsb-value {{
                font-size: 15px !important;
            }}
            .rsb-icon img {{
                width: 28px !important;
                height: 28px !important;
                padding-top: 11px !important;
            }}
            .min-table-td {{
                line-height: 18px !important;
            }}
        }}

        /* samsung fold phones */
        @media screen and (max-width:900px){{
            .table-title, .chart-title
            {{
                font-size: 13px !important;
            }}
            .rsb-title {{
                font-size: 10px !important;
            }}
            .rsb-value {{
                font-size: 13px !important;
            }}
            .chart-table {{
              display: block;
              width: 100%;
            }}
            .chart-table > tr, .chart-table > tbody > tr {{
              display: block;
              width: 100%;
            }}
            .chart-table > tr > td, .chart-table > tbody > tr > td {{
              display: block;
              width: 100%;
              box-sizing: border-box;
            }}
            .chart-table > tr > td:first-child, .chart-table > tbody > tr > td:first-child {{
                padding-top:10px;
            }}
            .report-flex-data .title {{
                font-size:1rem;
            }}
            .report-flex-data .icon img {{
              width: 32px;
              height: 32px;
              padding-top: 0 !important;
            }}
            .report-flex-data .percent {{
                width:8rem;
            }}
            .report-flex-data .info-box {{
              width:5rem;  
            }}
            .report-flex-data .info-box .ib-title {{
              font-size:0.7rem;
            }}
            .report-flex-data .info-box .ib-value {{
              font-size: 1rem;
              padding-top: 0.5rem;
            }}
        }}
    </style>
</head>
<body style=""background:#181a1b !important; color: #fff !important"">
    <div class=""report-output emailed"" style=""font-family: sans-serif; font-size:12px; text-align:center;max-width:1200px;margin:auto;padding:2rem 0"">
        <table style=""width: 100%;border-bottom:solid 3px #999;margin-bottom:10px"">
            <tr>
                <td style=""width:260px"">
                    <img src=""logo.png"" />
                </td>
                <td style=""text-align:left;font-size:17px"">
                    <div style=""padding-top: 14px;"">{HttpUtility.HtmlEncode(reportName)}</div>
                </td>
            </tr>
        </table>
        <br />
        {reportHtml}
    </div>
</body>
</html>";

        await Emailer.Send(recipients, subject, html, isHtml: true);
    }
    
    /// <summary>
    /// Gets the CSS 
    /// </summary>
    /// <returns>the CSS</returns>
    private string GetCss()
    {
#if (DEBUG)
        var dir = "wwwroot/css";
#else
        var dir = Path.Combine(DirectoryHelper.BaseDirectory, "Server/wwwroot/css");
#endif
        string file = Path.Combine(dir, "report-styles.css");
        if (File.Exists(file) == false)
            return string.Empty;
        var css = File.ReadAllText(file);

        var index = css.IndexOf(".report-output.onscreen", StringComparison.Ordinal);
        if (index > 0)
            css = css[0..index].Trim();
            
        // Regex to remove all CSS variable declarations
        css = Regex.Replace(css, @"--[^:]+:[^;]+;", "");

        // Regex to replace all instances of var(...) with their fallback values
        css = Regex.Replace(css, @"var\(--[^,]+, ([^)]+)\)", "$1");
        
        return"<style>\n" + css + "\n</style>\n";
    }


    /// <summary>
    /// The CSS for the reports 
    /// </summary>
    private const string CSS = @"
<style>
.report-output {
    font-family: sans-serif;
    font-size:12px;
    text-align:center;
}
table {
    width: 100%;
    font-size:12px;
    border-collapse: collapse;
    text-align: left;
    margin: 0;
    table-layout: fixed;
    min-width: min(100vw, 40rem);
    margin: auto;
}
table thead > tr {
    background:#e0e0e0;

}
table th, table td {
  border: solid 1px black;
  user-select: none;
  padding: 0 0.25rem 0 0.7rem;
  line-height: 1.75rem;
}
table td:not(:first-child) {
  border-left: none;
}
table td:not(:last-child) {
  border-right: none;
}
div.chart {
    text-align: center;
}
</style>
";
    /// <summary>
    /// Converts an image file to a Base64 encoded string and returns it as an HTML img tag.
    /// </summary>
    /// <param name="dir">The directory where the image file is located.</param>
    /// <param name="fileName">The name of the image file.</param>
    /// <returns>An HTML img tag with the Base64 encoded image, or an empty string if the file does not exist.</returns>
    public static string GetImageBase64(string dir, string fileName)
    {
        string filePath = Path.Combine(dir, fileName);
        if (File.Exists(filePath))
        {
            byte[] fileBytes = File.ReadAllBytes(filePath);
            string base64String = Convert.ToBase64String(fileBytes);
            string mimeType = GetMimeType(Path.GetExtension(filePath));
            return $"<img src=\"data:{mimeType};base64,{base64String}\" alt=\"Logo\">";
        }
        return string.Empty;
    }

    /// <summary>
    /// Gets the MIME type based on the file extension.
    /// </summary>
    /// <param name="extension">The file extension.</param>
    /// <returns>The MIME type as a string.</returns>
    private static string GetMimeType(string extension)
    {
        return extension.ToLower() switch
        {
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            _ => "application/octet-stream"
        };
    }
}