namespace FileFlows.Shared.Helpers;

/// <summary>
/// Helper for file size
/// </summary>
public class FileSizeHelper
{
    /// <summary>
    /// Humanizes a file size into a human readable size
    /// </summary>
    /// <param name="bytes">the size in bytes</param>
    /// <param name="decimalPlaces">number of decimal points</param>
    /// <returns>a human readable string, eg. 2.03GB</returns>
    public static string Humanize(long bytes, int decimalPlaces = 2) 
    {
        var sizes = new [] { "B", "KB", "MB", "GB", "TB" };

        var order = 0;
        decimal num = bytes;
        while (num >= 1000 && order < sizes.Length - 1) {
            order++;
            num /= 1000;
        }

        if (decimalPlaces < 1)
            return num.ToString("0") + ' ' + sizes[order];
        return num.ToString("0." + new string('#', decimalPlaces)) + ' ' + sizes[order];
    }
}