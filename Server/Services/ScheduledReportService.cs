using System.Web;
using FileFlows.FlowRunner.Helpers;
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
        string html = 
            "<html lang=\"en\">\n" +
            "<head><title>" + HttpUtility.HtmlEncode(subject) + "</title>\n" +
            GetCss() + "\n" +
            "</head>\n<body>\n" +
            "<div class=\"report-output emailed\">\n" +
            "<div class=\"report-header\"><div class=\"fileflows-logo\">" + GetLogoSvg() + "</div>\n" +
            "<div class=\"report-name\">" + HttpUtility.HtmlEncode(reportName) + "</div></div>\n" +
            reportHtml +
            "</div>" +
            "</body>" + 
            "</html>";
        await Emailer.Send(recipients, subject, html, isHtml: true);
    }
    /// <summary>
    /// Gets Logo SVG 
    /// </summary>
    /// <returns>the Logo SVG</returns>
    private string GetLogoSvg()
    {
#if (DEBUG)
        var dir = "wwwroot";
#else
        var dir = Path.Combine(DirectoryHelper.BaseDirectory, "Server/wwwroot");
#endif
        
        return GetImageBase64(dir, "report-logo.png");
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
        if (System.IO.File.Exists(file))
            return "<style>\n" + System.IO.File.ReadAllText(file) + "\n</style>\n";
        return string.Empty;
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