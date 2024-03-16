namespace FileFlows.ServerShared.Helpers;

/// <summary>
/// The time helper provides help methods regarding scheduling
/// </summary>
public class TimeHelper
{
    /// <summary>
    /// Gets the integer index of the current time quarter
    /// A time quarter is a 15minute block, starting on Sunday at midnight.
    /// </summary>
    /// <returns>The integer index of the current time quarter</returns>
    public static int GetCurrentQuarter()
    {
        DateTime date = DateTime.Now; // schedules are local to the server time, only thing left in non UTC dates

        int quarter = (((int)date.DayOfWeek) * 96) + (date.Hour * 4);
        if (date.Minute >= 45)
            quarter += 3;
        else if (date.Minute >= 30)
            quarter += 2;
        else if (date.Minute >= 15)
            quarter += 1;
        return quarter;
    }

    /// <summary>
    /// Gets the integer index of the a specific date
    /// A time quarter is a 15minute block, starting on Sunday at midnight.
    /// </summary>
    /// <param name="date">the date to get the quarter index for</param>
    /// <returns>The integer index of the time quarter</returns>
    public static int GetQuarter(DateTime date)
    {
        int quarter = (((int)date.DayOfWeek) * 96) + (date.Hour * 4);
        if (date.Minute >= 45)
            quarter += 3;
        else if (date.Minute >= 30)
            quarter += 2;
        else if (date.Minute >= 15)
            quarter += 1;
        return quarter;
    }

    /// <summary>
    /// Checks if the current time is in the supplied schedule
    /// </summary>
    /// <param name="schedule">The schedule to check</param>
    /// <returns>true if the current time is within the schedule</returns>
    public static bool InSchedule(string schedule)
    {
        if (string.IsNullOrEmpty(schedule) || schedule.Length != 672)
            return true; // bad schedule treat as always in schedule

        int quarter = GetCurrentQuarter();
        return schedule[quarter] == '1';
    }
}