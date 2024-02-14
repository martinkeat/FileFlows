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
        if (t.TotalSeconds < 1) // Less than 2 minutes
            return $@"1 second";
        if (t.TotalSeconds < 120) // Less than 2 minutes
            return $@"{((int)t.TotalSeconds)} seconds";
        if (t.TotalMinutes < 120) // Less than 2 hours
            return $@"{t:%m} minutes";
        if (t.TotalHours < 24) // Less than 1 day
            return $@"{t:%h} hours";
        return $@"{t:%d} days";
    }

}