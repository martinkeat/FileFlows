using System.Web;
using FileFlows.Shared.Helpers;

namespace FileFlows.ServerShared.Services;

/// <summary>
/// Statistic Service interface
/// </summary>
public interface IStatisticService
{
    /// <summary>
    /// Records a running total value
    /// </summary>
    /// <returns>a task to await</returns>
    Task RecordRunningTotal(string name, string value);
    
    /// <summary>
    /// Records a average value
    /// </summary>
    /// <returns>a task to await</returns>
    Task RecordAverage(string name, int value);
}
