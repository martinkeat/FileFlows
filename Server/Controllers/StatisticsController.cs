using Microsoft.AspNetCore.Mvc;
using FileFlows.Server.Services;
using FileFlows.ServerShared.Models;

namespace FileFlows.Server.Controllers;


/// <summary>
/// Status controller
/// </summary>
[Route("/api/statistics")]
public class StatisticsController : Controller
{
    /// <summary>
    /// Records a statistic
    /// </summary>
    /// <param name="statistic">the statistic to record</param>
    [HttpPost("record")]
    public Task Record([FromBody] Statistic statistic)
        => new StatisticService().Record(statistic);

    /// <summary>
    /// Gets statistics by name
    /// </summary>
    /// <returns>the matching statistics</returns>
    [HttpGet("running-totals/{name}")]
    public Task<IEnumerable<Statistic>> GetRunningTotals([FromRoute] string name)
        => new StatisticService().GetRunningTotals(name);
    
    /// <summary>
    /// Clears statistics for
    /// </summary>
    /// <param name="name">Optional. The name for which DbStatistics should be cleared.</param>
    /// <returns>the response</returns>
    [HttpPost("clear")]
    public IActionResult Clear([FromQuery] string? name = null)
    {
        try
        {
            new StatisticService().Clear(name);
            return Ok();
        }
        catch (Exception)
        {
            return BadRequest();
        }
    }
}

