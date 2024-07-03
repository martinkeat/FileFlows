using FileFlows.Plugin.Helpers;

namespace FileFlows.Shared.Formatters;

/// <summary>
/// Formatter used to format filenames to a short name.
/// </summary>
public class FileNameFormatter : Formatter
{
    /// <summary>
    /// Formats a value to a short filename.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The formatted value as a string.</returns>
    protected override string Format(object value)
    {
        // Convert the input value to a string, return empty string if null
        string str = value?.ToString() ?? string.Empty;

        // Return the original string if it is null or whitespace
        if (string.IsNullOrWhiteSpace(str))
            return str;
        
        // Format the string to a short filename
        return Format(str);
    }

    /// <summary>
    /// Formats a value to a short filename.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The formatted string.</returns>
    public static string Format(string value)
    {
        // Return empty string if the input value is null or whitespace
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        // Get the short name of the file using a helper method
        string shortName = FileHelper.GetShortFileName(value);

        // Check if the short name looks like a GUID, with or without special characters
        if (IsPotentialGuid(shortName) == false)
            return shortName;

        // Get the directory name if the short name is a GUID
        return FileHelper.GetDirectoryName(value);
    }

    /// <summary>
    /// Determines if a given string is a potential GUID.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <returns>True if the string looks like a GUID, otherwise false.</returns>
    private static bool IsPotentialGuid(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        int dotIndex = value.LastIndexOf('.');
        if (dotIndex > 0)
            value = value[..dotIndex];
        
        // A GUID has 32 hexadecimal characters, optionally separated by hyphens
        if (value.Length < 32)
            return false;

        string sanitizedValue = value.Replace("-", string.Empty);

        if (sanitizedValue.Length != 32)
            return false;

        // Check if all characters are valid hexadecimal digits
        return sanitizedValue.All(c => "0123456789abcdefABCDEF".Contains(c));
    }
}
