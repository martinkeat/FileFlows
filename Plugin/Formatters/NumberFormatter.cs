using System.Text.RegularExpressions;

namespace FileFlows.Plugin.Formatters;

/// <summary>
/// Formats a value as a number
/// </summary>
public class NumberFormatter  : Formatter
{
    /// <summary>
    /// Tests if this formatter is a match for the given format
    /// </summary>
    /// <param name="format">the format to use</param>
    /// <returns>true if this formatter matches the format</returns>
    public override bool IsMatch(string format)
    {
        if (string.IsNullOrEmpty(format))
            return false;
        if (Regex.IsMatch(format, "^[#]+"))
            return true;
        if (Regex.IsMatch(format, "^[0]+"))
            return true;
        return false;
    }

    
    /// <summary>
    /// Formats the value
    /// </summary>
    /// <param name="value">the value to format</param>
    /// <param name="format">the format to use</param>
    /// <returns>the formatted value string</returns>
    public override string Format(object value, string format)
    {
        if (value == null)
            return string.Empty;
        
        int digits = format.Length;
        
        if (value is int iValue)
            return iValue.ToString(new string('0', digits));
        
        if(value is double dValue)
            value = dValue.ToString(new string('0', digits));
        
        if (value is Int64 i64Value)
            value = i64Value.ToString(new string('0', digits));
        
        return value.ToString();
    }
}