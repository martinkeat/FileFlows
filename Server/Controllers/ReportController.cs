using FileFlows.Managers;
using FileFlows.Server.Authentication;
using FileFlows.Server.Helpers;
using FileFlows.Server.Services;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace FileFlows.Server.Controllers;

/// <summary>
/// Controller for the Reports
/// </summary>
[Route("/api/report")]
[FileFlowsAuthorize(UserRole.Admin)]
public class ReportController : BaseController
{
    /// <summary>
    /// Gets the report definitions
    /// </summary>
    /// <returns>the report definitions</returns>
    [HttpGet("definitions")]
    public IActionResult GetReportDefinitions()
    {
        if (LicenseHelper.IsLicensed(LicenseFlags.Reporting) == false)
            return NotFound();
        
        var results = new ReportManager().GetReports();
        return Ok(results.OrderBy(x => x.Name.ToLowerInvariant()));
    }

    /// <summary>
    /// Gets a report definition by its UID
    /// </summary>
    /// <param name="uid">the UID fo the report to get the definition for</param>
    /// <returns>the report definitions</returns>
    [HttpGet("definition/{uid}")]
    public IActionResult GetReportDefinition([FromRoute] Guid uid)
    {
        if (LicenseHelper.IsLicensed(LicenseFlags.Reporting) == false)
            return NotFound();
        
        var result = new ReportManager().GetReports().FirstOrDefault(x => x.Uid == uid);
        if (result == null)
            return NotFound();
        return Ok(result);
    }
    
    /// <summary>
    /// Generates the report
    /// </summary>
    /// <param name="uid">the UID of the report</param>
    /// <param name="model">the report model</param>
    /// <returns>the reports HTML</returns>
    [HttpPost("generate/{uid}")]
    public async Task<IActionResult> Generate([FromRoute] Guid uid, [FromBody] Dictionary<string, object> model)
    {
        if (LicenseHelper.IsLicensed(LicenseFlags.Reporting) == false)
            return NotFound();

        string? email = null;
        if (model.TryGetValue("Email", out var value) && value is JsonElement je )
            email = je.ValueKind == JsonValueKind.String ? je.GetString() : null;

        bool emailing = string.IsNullOrWhiteSpace(email) == false;

        if (emailing)
        {
            _ = Task.Run(async () =>
            {
                var result = await new ReportManager().Generate(uid, emailing, model);
                if (result.Failed(out var rError))
                {
                    _ = ServiceLoader.Load<NotificationService>()
                        .Record(NotificationSeverity.Warning, "Report Failed to generate", rError);
                    return;
                }

                string css = GetCss();
                string html = css + "<div class=\"report-output emailed\">" + result.Value + "</div>";
                await Emailer.Send([email], "FileFlows Report", html, isHtml: true);
            });
            
            // email reports we just exit early
            return Ok();
        }

        var result = await new ReportManager().Generate(uid, emailing, model);
        if (result.Failed(out var error))
            return BadRequest(error);
        return Ok(result.Value);
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