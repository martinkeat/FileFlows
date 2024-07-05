namespace FileFlows.Shared.Formatters;

/// <summary>
/// Formatter used to format bytes to a file size string 
/// </summary>
public class FileSizeFormatter : Formatter
{
    /// <summary>
    /// Formats a value as a file size
    /// </summary>
    /// <param name="value">the value to format</param>
    /// <returns>the value as a file size</returns>
    protected override string Format(object value)
    {
        double dValue = 0;
        if (value is long longValue)
        {
            dValue = longValue;
        }
        else if (value is int intValue)
        {
            dValue = intValue;
        }
        else if (value is decimal decimalValue)
        {
            dValue = Convert.ToDouble(decimalValue);
        }
        else if (value is float floatValue)
        {
            dValue = floatValue;
        }
        else if (value is short shortValue)
        {
            dValue = shortValue;
        }
        else if (value is byte byteValue)
        {
            dValue = byteValue;
        }

        return Format(dValue);
    }

    /// <summary>
    /// The sizable units
    /// </summary>
    static string[] sizes = { "B", "KB", "MB", "GB", "TB" };

    /// <summary>
    /// Formats a byte value as a string
    /// </summary>
    /// <param name="size">The size in bytes</param>
    /// <param name="decimalPoints">the number of decimal points</param>
    /// <returns>The size in a formatted string</returns>
    public static string Format(double size, int decimalPoints = 2)
    {
        int order = 0;
        double num = size;
        while (num >= 1000 && order < sizes.Length - 1)
        {
            order++;
            num /= 1000;  // 1024 would be a kibibyte.  I'm trying to embrace the proper metric meaning....
        }
        if (decimalPoints == 0)
            return $"{num:0} {sizes[order]}";
        return $"{num.ToString($"F{decimalPoints}")} {sizes[order]}";

    }
}