using System.Text.RegularExpressions;

namespace FileFlows.Plugin.Formatters;

/// <summary>
/// Formatter used to format variables
/// </summary>
public abstract class Formatter
{
    /// <summary>
    /// Tests if this formatter is a match for the given format
    /// </summary>
    /// <param name="format">the format to use</param>
    /// <returns>true if this formatter matches the format</returns>
    public abstract bool IsMatch(string format);

    /// <summary>
    /// Formats the value
    /// </summary>
    /// <param name="value">the value to format</param>
    /// <param name="format">the format to use</param>
    /// <returns>the formatted value string</returns>
    public abstract string Format(object value, string format);

    /// <summary>
    /// Gets the formatter for the given format
    /// </summary>
    /// <param name="format"></param>
    /// <returns></returns>
    public static Formatter GetFormatter(string format)
    {
        if (string.IsNullOrWhiteSpace(format))
            return _noFormatter;
        foreach (var formatter in Formatters)
        {
            if (formatter.IsMatch(format))
                return formatter;
        }

        if (Regex.IsMatch(format, @"^[yMdHhmstf\-._/:\s]+$"))
            return _dateFormatter;

        return _noFormatter;
    }

    private static readonly List<Formatter> Formatters;

    private static readonly DateFormatter _dateFormatter = new();
    private static readonly NoFormatter _noFormatter = new ();
    
    static Formatter()
    {
        Formatters = new List<Formatter>()
        {
            new UpperCaseFormatter(),
            new NumberFormatter(),
            new SizeFormatter(),
            new FileFormatter(),
            _dateFormatter
        };
    }
}