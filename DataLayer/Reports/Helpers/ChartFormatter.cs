using System.Globalization;
using FileFlows.Shared.Formatters;

namespace FileFlows.DataLayer.Reports.Helpers;

/// <summary>
/// Formatter for charts
/// </summary>
public static class ChartFormatter
{
    /// <summary>
    /// Formats a value
    /// </summary>
    /// <param name="value">the value</param>
    /// <param name="formatter">the formatter</param>
    /// <returns>the formatted value</returns>
    internal static string Format(object value, string? formatter)
    {
        if (value == null)
            return string.Empty;

        if (formatter?.ToLowerInvariant() == "filesize")
            return FileSizeFormatter.Format(Convert.ToDouble(value));

        if (value is int or long)
            return $"{value:N0}"!; // Format with thousands separator, no decimals
        if (value is IFormattable numericValue)
            return numericValue.ToString("N", CultureInfo.CurrentCulture); //string.Format($"{value:F0}", numericValue);
        if (value is DateTime dt)
            return dt.ToString("d MMMM yyyy");
        
        return value.ToString()?? string.Empty;
    }
}