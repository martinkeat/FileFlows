namespace FileFlows.DataLayer;

/// <summary>
/// Extension methods
/// </summary>
static class ExtensionMethods
{
    /// <summary>
    /// Ensures the data is no less than  than 1970
    /// </summary>
    /// <param name="date">The DateTime object to modify.</param>
    /// <returns>The DateTime object with the date set to the maximum of itself and the specified minimum date.</returns>
    public static DateTime EnsureNotLessThan1970(this DateTime date)
    {
        DateTime minUtcDate = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return date > minUtcDate ? date : minUtcDate;
    }
    
    /// <summary>
    /// Clamps the date within the specified range defined by the minimum and maximum values.
    /// </summary>
    /// <param name="date">The DateTime object to modify.</param>
    /// <param name="minDate">The minimum allowed date (optional, default is January 1, 1970 UTC).</param>
    /// <param name="maxDate">The maximum allowed date (optional, default is current UTC time).</param>
    /// <returns>The DateTime object clamped within the specified range.</returns>
    public static DateTime Clamp(this DateTime date, DateTime? minDate = null, DateTime? maxDate = null)
    {
        DateTime minUtcDate = minDate ?? new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime maxUtcDate = maxDate ?? DateTime.UtcNow;
        
        DateTime clampedDate = date < minUtcDate ? minUtcDate : date;
        return clampedDate > maxUtcDate ? maxUtcDate : clampedDate;
    }
    
    /// <summary>
    /// Returns the maximum of two DateTime values.
    /// </summary>
    /// <param name="date1">The first DateTime value to compare.</param>
    /// <param name="date2">The second DateTime value to compare.</param>
    /// <returns>The maximum of the two DateTime values.</returns>
    public static DateTime Max(this DateTime date1, DateTime date2)
    {
        return date1 > date2 ? date1 : date2;
    }

    /// <summary>
    /// Returns the minimum of two DateTime values.
    /// </summary>
    /// <param name="date1">The first DateTime value to compare.</param>
    /// <param name="date2">The second DateTime value to compare.</param>
    /// <returns>The minimum of the two DateTime values.</returns>
    public static DateTime Min(this DateTime date1, DateTime date2)
    {
        return date1 < date2 ? date1 : date2;
    }
}