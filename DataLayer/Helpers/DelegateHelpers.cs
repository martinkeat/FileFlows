using FileFlows.DataLayer.Reports.Charts;

namespace FileFlows.DataLayer.Helpers;

/// <summary>
/// Helpers that are passed in from the Server, so this library doesn't need to reference extra packages
/// </summary>
public static class DelegateHelpers
{
    /// <summary>
    /// Converts a SVG into an image tag
    /// </summary>
    public static Func<string, int, int, string>? SvgToImageTag { get; set; }

    /// <summary>
    /// Generates a multi-line chart iage tag
    /// </summary>
    /// <returns>the multi-line chart image tag</returns>
    public static Func<MultilineChartData, string>? GenerateMultilineChart { get; set; }
}
