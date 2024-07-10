using FileFlows.DataLayer.Reports.Charts;

namespace FileFlows.DataLayer.Helpers;

/// <summary>
/// Helpers that are passed in from the Server, so this library doesn't need to reference extra packages
/// </summary>
public static class DelegateHelpers
{
    /// <summary>
    /// Generates a line chart image tag
    /// </summary>
    /// <returns>the line chart image tag</returns>
    public static Func<LineChartData, string>? GenerateLineChart { get; set; }

    /// <summary>
    /// Generates a bar chart image tag
    /// </summary>
    /// <returns>the bar chart image tag</returns>
    public static Func<BarChartData, string>? GenerateBarChart { get; set; }

    /// <summary>
    /// Generates a pie chart image tag
    /// </summary>
    /// <returns>the pie chart image tag</returns>
    public static Func<PieChartData, string>? GeneratePieChart { get; set; }
}
