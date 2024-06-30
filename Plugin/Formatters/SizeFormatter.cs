namespace FileFlows.Plugin.Formatters;

/// <summary>
/// Formats a value as a size
/// </summary>
public class SizeFormatter : Formatter
{
    static readonly string[] Sizes = new string[] { "B", "KB", "MB", "GB", "TB" };
    
    /// <summary>
    /// Tests if this formatter is a match for the given format
    /// </summary>
    /// <param name="format">the format to use</param>
    /// <returns>true if this formatter matches the format</returns>
    public override bool IsMatch(string format)
    {
        if (string.IsNullOrEmpty(format))
            return false;
        switch (format.ToLower())
        {
            case "size":
            case "filesize":
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Formats the value
    /// </summary>
    /// <param name="value">the value to format</param>
    /// <param name="format">the format to use</param>
    /// <returns>the formatted value string</returns>
    public override string Format(object value, string format)
        => Format(value, decimalPoints: 2);
    
    /// <summary>
    /// Formats the value
    /// </summary>
    /// <param name="value">the value to format</param>
    /// <param name="decimalPoints">the number of decimal points to use</param>
    /// <returns>the formatted value string</returns>
    public string Format(object value, int decimalPoints = 2)
    {
        if (value == null)
            return string.Empty;
        
        long bytes = 0;
        if (value is long s2)
            bytes = s2;
        else if (value is double dValue)
            bytes = (long)dValue;
        else if (value is int iValue)
            bytes = iValue;
        else if (long.TryParse(value.ToString(), out long parsed))
            bytes = parsed;
        else
            return value.ToString();
        

        var order = 0;
        decimal num = bytes;
        while (num >= 1000 && order < Sizes.Length - 1) {
            order++;
            num /= 1000;
        }
        if(decimalPoints < 1)
            return num.ToString("0") + ' ' + Sizes[order];
        return num.ToString("0." + new string('#', decimalPoints)) + ' ' + Sizes[order];
    }
}