using Mysqlx.Datatypes;
using Microsoft.AspNetCore.Mvc;
using FileFlows.Server.Helpers;
using FileFlows.Server.Services;

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
    [HttpGet("by-name/{name}")]
    public Task<IEnumerable<Statistic>> GetStatisticsByName([FromRoute] string name)
        => new StatisticService().GetStatisticsByName(name);

    /// <summary>
    /// Gets statistics totaled by their name
    /// </summary>
    /// <returns>the matching statistics</returns>
    [HttpGet("totals-by-name/{name}")]
    public Task<Dictionary<string, int>> GetTotalsByName([FromRoute] string name)
        => new StatisticService().GetTotalsByName(name);
    
    /// <summary>
    /// Clears statistics for
    /// </summary>
    /// <param name="name">Optional. The name for which DbStatistics should be cleared.</param>
    /// <param name="before">Optional. The date before which DbStatistics should be cleared.</param>
    /// <param name="after">Optional. The date after which DbStatistics should be cleared.</param>
    /// <returns>the response</returns>
    [HttpPost("clear")]
    public IActionResult Clear([FromQuery] string? name = null, DateTime? before = null, DateTime? after = null)
    {
        try
        {
            new StatisticService().Clear(name, before, after);
            return Ok();
        }
        catch (Exception)
        {
            return BadRequest();
        }
    }
}


/// <summary>
/// A statistic
/// </summary>
public class Statistic
{
    /// <summary>
    /// Gets or sets the name of the statistic
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the value
    /// </summary>
    public object Value { get; set; }
}