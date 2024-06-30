using FileFlows.Managers;
using FileFlows.Server.Authentication;
using FileFlows.Server.Helpers;
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

        var result = await new ReportManager().Generate(uid, model);
        if (result.Failed(out var error))
            return BadRequest(error);
        return Ok(result.Value);
    }
}