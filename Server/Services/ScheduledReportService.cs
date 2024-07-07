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
        string html = GetCss() + "<div class=\"report-output emailed\">\n" +
                               "<div class=\"report-header\"><div class=\"fileflows-logo\">" + GetLogoSvg() + "</div>\n" + 
                               "<div class=\"report-name\">" + HttpUtility.HtmlEncode(reportName) + "</div></div>\n" +
                               reportHtml +
                               "</div>";
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
        string file = Path.Combine(dir, "logo.svg"); //"logo-color-full.svg");
        if (File.Exists(file))
            return File.ReadAllText(file);
        return string.Empty;
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
}