using System.Text.RegularExpressions;

namespace FileFlows.Plugin.Formatters;

/// <summary>
/// Ensures a variable is a safe filename string
/// </summary>
public class FileFormatter : Formatter
{
    /// <inheritdoc />
    public override bool IsMatch(string format) =>
        format != null && (format.Equals("file", StringComparison.InvariantCultureIgnoreCase) ||
                           format.Equals("filename", StringComparison.InvariantCultureIgnoreCase));
    

    /// <inheritdoc />
    public override string Format(object value, string format)
    {
        var name = value as string ?? string.Empty;
        
        name = name.Replace(" : ", " - ");
        name = name.Replace(": ", " - ");
        
        // Define a set of invalid characters common to Windows, macOS, and Linux
        char[] invalidChars = [
            '<', '>', ':', '"', '/', '\\', '|', '?', '*', // Windows
            '\0', '\t', '\n', '\r', '\f', '\b', // Control characters
            '\u0001', '\u0002', '\u0003', '\u0004', '\u0005', '\u0006', '\u0007', '\u0008', '\u0009',
            '\u000A', '\u000B', '\u000C', '\u000D', '\u000E', '\u000F', '\u0010', '\u0011', '\u0012',
            '\u0013', '\u0014', '\u0015', '\u0016', '\u0017', '\u0018', '\u0019', '\u001A', '\u001B',
            '\u001C', '\u001D', '\u001E', '\u001F', // ASCII control characters
            ':' // macOS
        ];

        // Create a regex pattern to match invalid characters
        var pattern = $"[{Regex.Escape(new string(invalidChars))}]";
        
        // Replace invalid characters with an underscore
        var sanitizedFileName = Regex.Replace(name, pattern, "");

        // Trimming any trailing dots or spaces which are not allowed in Windows
        sanitizedFileName = sanitizedFileName.TrimEnd('.', ' ');
        return sanitizedFileName;
    }
}