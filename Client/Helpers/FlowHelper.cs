namespace FileFlows.Client.Helpers;

using System.Text.RegularExpressions;
using FileFlows.Plugin;

/// <summary>
/// Helper used by the Flow page
/// </summary>
public class FlowHelper
{
    /// <summary>
    /// Gets the icon for a flow element type
    /// </summary>
    /// <param name="type">the type of flow element</param>
    /// <returns>the icon of the type</returns>
    public static string GetFlowPartIcon(FlowElementType type)
    {
        return "fas fa-chevron-right";
    }

    /// <summary>
    /// Converts a PascalCase string into aa human readable one with proper casing
    /// </summary>
    /// <param name="name">the string to format</param>
    /// <returns>a human readable formatted string</returns>
    public static string FormatLabel(string name)
    {
        return Regex.Replace(name.Replace("_", " "), "(?<=[A-Za-z])(?=[A-Z][a-z])|(?<=[a-z0-9])(?=[0-9]?[A-Z])", " ")
            .Replace("Ffmpeg", "FFmpeg");
    }
}