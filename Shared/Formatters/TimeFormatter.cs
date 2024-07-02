namespace FileFlows.Shared.Formatters;

/// <summary>
/// Formatter used to format values as a time duration string 
/// </summary>
public class TimeFormatter : Formatter
{
    /// <summary>
    /// Formats a value as a time duration
    /// </summary>
    /// <param name="value">The value to format</param>
    /// <returns>The value as a time duration</returns>
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
    /// The time units
    /// </summary>
    static readonly (string Singular, string Plural, double Value)[] timeUnits = 
    {
        ("second", "seconds", 1),
        ("minute", "minutes", 60),
        ("hour", "hours", 3600),
        ("day", "days", 86400),
        ("week", "weeks", 604800)
    };

    /// <summary>
    /// Formats a time value as a string
    /// </summary>
    /// <param name="seconds">The time duration in seconds</param>
    /// <returns>The time duration in a formatted string</returns>
    public static string Format(double seconds)
    {
        int order = 0;
        while (order < timeUnits.Length - 1 && seconds >= timeUnits[order + 1].Value)
        {
            order++;
        }

        double num = seconds / timeUnits[order].Value;
        string unit = num == 1 ? timeUnits[order].Singular : timeUnits[order].Plural;
        return $"{num:0.##} {unit}";
    }
}
