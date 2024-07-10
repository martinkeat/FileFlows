using System.Web;
using FileFlows.DataLayer.Reports.Charts;

namespace FileFlows.Server.Helpers;

/// <summary>
/// DataLayer delegates
/// </summary>
public static class DataLayerDelegates
{
    /// <summary>
    /// Sets up the data layer delegate methods
    /// </summary>
    public static void Setup()
    {
        DataLayer.Helpers.DelegateHelpers.GenerateLineChart = GenerateLineChart;
        DataLayer.Helpers.DelegateHelpers.GenerateBarChart = GenerateBarChart;
        DataLayer.Helpers.DelegateHelpers.GeneratePieChart = GeneratePieChart;
    }
    
    /// <summary>
    /// Generates a line chart
    /// </summary>
    /// <param name="chartData">the chart data</param>
    /// <returns>the HTML of the image tag</returns>
    static string GenerateLineChart(LineChartData chartData)
    {
        try
        {
            return new Charting.LineChart().GenerateImage(chartData);
        }
        catch (Exception ex)
        {
            return $"<span>Failed generating image: {HttpUtility.HtmlEncode(ex.Message)}</span>";
        }
    }

    /// <summary>
    /// Generates a bar chart
    /// </summary>
    /// <param name="chartData">the chart data</param>
    /// <returns>the HTML of the image tag</returns>
    static string GenerateBarChart(BarChartData chartData)
    {
        try
        {
            return new Charting.BarChart().GenerateImage(chartData);
        }
        catch (Exception ex)
        {
            return $"<span>Failed generating image: {HttpUtility.HtmlEncode(ex.Message)}</span>";
        }
    }
    
    /// <summary>
    /// Generates a pie chart
    /// </summary>
    /// <param name="chartData">the chart data</param>
    /// <returns>the HTML of the image tag</returns>
    static string GeneratePieChart(PieChartData chartData)
    {
        try
        {
            return new Charting.PieChart().GenerateImage(chartData);
        }
        catch (Exception ex)
        {
            return $"<span>Failed generating image: {HttpUtility.HtmlEncode(ex.Message)}</span>";
        }
    }
}