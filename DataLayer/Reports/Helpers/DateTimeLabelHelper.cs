namespace FileFlows.DataLayer.Reports.Helpers;

/// <summary>
/// Date time label helper
/// </summary>
public static class DateTimeLabelHelper
{
    /// <summary>
    /// Generates daily labels between the min and max dates.
    /// </summary>
    /// <param name="minDateUtc">The minimum date.</param>
    /// <param name="maxDateUtc">The maximum date.</param>
    /// <returns>An array of daily labels.</returns>
    public static DateTime[] Generate(DateTime minDateUtc, DateTime maxDateUtc)
        => GenerateDates(minDateUtc, maxDateUtc).Dates;
    
    /// <summary>
    /// Generates daily labels between the min and max dates.
    /// </summary>
    /// <param name="minDateUtc">The minimum date.</param>
    /// <param name="maxDateUtc">The maximum date.</param>
    /// <returns>An array of daily labels.</returns>
    public static (bool Hourly, DateTime[] Dates) GenerateDates(DateTime minDateUtc, DateTime maxDateUtc)
    {
        List<DateTime> labels = new List<DateTime>();
        bool hourly = maxDateUtc.Subtract(minDateUtc).TotalDays <= 1;
        DateTime currentDate = hourly ? minDateUtc : minDateUtc.Date;

        while (currentDate <= maxDateUtc)
        {
            labels.Add(currentDate);
            if(hourly)
                currentDate = currentDate.AddHours(1);
            else
                currentDate = currentDate.AddDays(1);
        }

        return (hourly, labels.ToArray());
    }
}