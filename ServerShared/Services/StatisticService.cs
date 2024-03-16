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

/// <summary>
/// Statistics service
/// </summary>
public class StatisticService : Service, IStatisticService
{

    /// <summary>
    /// Gets or sets a function used to load new instances of the service
    /// </summary>
    public static Func<IStatisticService> Loader { get; set; }

    /// <summary>
    /// Loads an instance of the plugin service
    /// </summary>
    /// <returns>an instance of the plugin service</returns>
    public static IStatisticService Load()
    {
        if (Loader == null)
            return new StatisticService();
        return Loader.Invoke();
    }

    /// <inheritdoc />
    public async Task RecordRunningTotal(string name, string value)
    {
        try
        {
            await HttpHelper.Post($"{ServiceBaseUrl}/api/statistics/record-running-total" +
                                  $"?name={HttpUtility.UrlEncode(name)}" +
                                  $"&value={HttpUtility.UrlEncode(value)}");
        }
        catch (Exception)
        {
        }
    }

    /// <inheritdoc />
    public async Task RecordAverage(string name, int value)
    {
        try
        {
            await HttpHelper.Post($"{ServiceBaseUrl}/api/statistics/record-average" +
                                  $"?name={HttpUtility.UrlEncode(name)}" +
                                  $"&value={value}");
        }
        catch (Exception)
        {
        }
    }
}
