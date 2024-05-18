

namespace FileFlows.RemoteServices;

/// <summary>
/// Statistics service
/// </summary>
public class StatisticService : RemoteService, IStatisticService
{
    /// <inheritdoc />
    public async Task RecordRunningTotal(string name, string value)
    {
        try
        {
            await HttpHelper.Post($"{ServiceBaseUrl}/remote/statistic/record-running-total" +
                                  $"?name={HttpUtility.UrlEncode(name)}" +
                                  $"&value={HttpUtility.UrlEncode(value)}");
        }
        catch (Exception)
        {
            // Ignored
        }
    }

    /// <inheritdoc />
    public async Task RecordAverage(string name, int value)
    {
        try
        {
            await HttpHelper.Post($"{ServiceBaseUrl}/remote/statistic/record-average" +
                                  $"?name={HttpUtility.UrlEncode(name)}" +
                                  $"&value={value}");
        }
        catch (Exception)
        {
        }
    }
}
