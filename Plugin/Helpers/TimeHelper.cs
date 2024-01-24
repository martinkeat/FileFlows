namespace FileFlows.Plugin.Helpers;

/// <summary>
/// Helper class for working with time-related operations.
/// </summary>
public class TimeHelper
{
    /// <summary>
    /// Converts a <see cref="TimeSpan"/> into a human-readable string.
    /// </summary>
    /// <param name="t">The <see cref="TimeSpan"/> to be converted.</param>
    /// <returns>A human-readable representation of the <see cref="TimeSpan"/>.</returns>\
    public static string ToHumanReadableString(TimeSpan t)
    {
        if (t.TotalSeconds <= 1)
            return $@"{t:s\.ff} seconds";
        if (t.TotalMinutes < 2)
            return $@"{t:%s} seconds";
        if (t.TotalHours < 2)
            return $@"{t:%m} minutes";
        if (t.TotalDays <= 1)
            return $@"{t:%h} hours";
        return $@"{t:%d} days";
    }

}