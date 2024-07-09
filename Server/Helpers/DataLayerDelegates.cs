using System.Text;
using System.Web;
using FileFlows.DataLayer.Reports.Charts;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

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
    }
    
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
    /// Converts an Image object to a base64-encoded PNG and returns an HTML img tag.
    /// </summary>
    /// <param name="image">The Image object to convert.</param>
    /// <returns>An HTML img tag with base64-encoded PNG.</returns>
    public static string ImageToBase64ImgTag(Image<Rgba32> image)
    {
        using MemoryStream memoryStream = new MemoryStream();
        // Save the Image to the memory stream as PNG
        image.Save(memoryStream, new PngEncoder());

        // Convert the image to base64
        string base64Image = Convert.ToBase64String(memoryStream.ToArray());

        // Construct the img tag
        StringBuilder imgTagBuilder = new StringBuilder();
        imgTagBuilder.Append("<img ");
        imgTagBuilder.Append($"src=\"data:image/png;base64,{base64Image}\" ");
        imgTagBuilder.Append($"width=\"{image.Width}\" ");
        imgTagBuilder.Append($"height=\"{image.Height}\" ");
        imgTagBuilder.Append("/>");

        return imgTagBuilder.ToString();
    }
}