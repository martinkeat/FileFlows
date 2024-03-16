using Humanizer;

namespace FileFlows.Client.Helpers;

/// <summary>
/// Helper used to format strings
/// </summary>
public static class FormatHelper
{
    /// <summary>
    /// Formats a date so its easily readable to a user
    /// </summary>
    /// <param name="dateUtc">the UTC date</param>
    /// <returns>the human readable date</returns>
    public static string HumanizeDate(DateTime dateUtc)
    {
        var local = dateUtc.ToLocalTime();
        try
        {
            return local.Humanize(false, DateTime.Now);
        }
        catch (Exception)
        {
            // see FF-1130 - can throw an exception
            if (dateUtc < DateTime.Now)
                return local.ToShortDateString();
            return local.ToShortTimeString();
        }
    }
    /// <summary>
    /// Formats a time so its easily readable to a user
    /// </summary>
    /// <param name="time">the time</param>
    /// <returns>the human readable time</returns>
    public static string HumanizeTime(TimeSpan time)
    {
        try
        {
            return time.Humanize();
        }
        catch (Exception)
        {
            return time.ToString();
        }
    }
}