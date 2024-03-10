using Microsoft.AspNetCore.Mvc;
using FileFlows.Server.Services;
using FileFlows.ServerShared.Models;
using FileFlows.ServerShared.Models.StatisticModels;

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
    /// Gets storage saved
    /// </summary>
    /// <returns>the storage saved</returns>
    [HttpGet("storage-saved")]
    public async Task<object> GetStorageSaved()
    {
        var data = await new StatisticService().GetStorageSaved();
        data = data.OrderByDescending(x => x.FinalSize - x.OriginalSize).ToList();
        if (data.Count > 5)
        {
            var total = new StorageSavedData
            {
                Library = "Total",
                TotalFiles = data.Skip(4).Sum(x => x.TotalFiles),
                FinalSize = data.Skip(4).Sum(x => x.FinalSize),
                OriginalSize = data.Skip(4).Sum(x => x.OriginalSize)
            };
            data = data.Take(4).ToList();
            data.Add(total);
        }
        
        return new
        {
            series = new object[]
            {
                new { name = "Final Size", data = data.Select(x => x.FinalSize).ToArray() },
                new { name = "Savings", data = data.Select(x =>
                {
                    var change = x.OriginalSize - x.FinalSize;
                    if (change > 0)
                        return change;
                    return 0;
                }).ToArray() },
                new { name = "Increase", data = data.Select(x =>
                {
                    var change = x.OriginalSize - x.FinalSize;
                    if (change > 0)
                        return 0;
                    return change * -1;
                }).ToArray() }
            },
            labels = data.Select(x => x.Library.Replace("###TOTAL###", "Total")).ToArray(),
            items = data.Select(x => x.TotalFiles).ToArray()
        };
    }

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

