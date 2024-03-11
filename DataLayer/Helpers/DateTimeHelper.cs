namespace FileFlows.DataLayer.Helpers;

/// <summary>
/// Date Time Helper
/// </summary>
class DateTimeHelper
{
    /// <summary>
    /// Converts a datetime that's local to UTC
    /// </summary>
    /// <param name="dt">the DateTime</param>
    /// <returns>the date time as UTC</returns>
    internal static DateTime LocalToUtc(DateTime dt)
    {
        DateTime correctedDateTime = DateTime
            .SpecifyKind(
                new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond),
                DateTimeKind.Local).ToUniversalTime();
        if (correctedDateTime.Year < 1970)
            return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        return correctedDateTime;
    }
}