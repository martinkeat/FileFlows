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
    /// The width for emailed charts
    /// </summary>
    const int EmailChartWidth = 590;
    /// <summary>
    /// The height for emailed charts
    /// </summary>
    const int EmailChartHeight = 210;
    
    /// <summary>
    /// The colors to show on the chart
    /// </summary>
    static readonly string[] COLORS =
    [
        // same colors as JS
        "#33b2df","#33DF55A6", "#84004bd9", "#007bff", "#6610f2",
        "#17a2b8", "#fd7e14", "#28a745", "#20c997", "#ffc107",  "#ff4d76",
        // different colors
        "#007bff", "#28a745", "#ffc107", "#dc3545", "#17a2b8",
        "#fd7e14", "#6f42c1", "#20c997", "#6610f2", "#e83e8c",
        "#ff6347", "#4682b4", "#9acd32", "#8a2be2", "#ff4500",
        "#2e8b57", "#d2691e", "#ff1493", "#00ced1", "#b22222",
        "#daa520", "#5f9ea0", "#7f007f", "#808000", "#3cb371"
    ];
    private static Font Font;

    static DataLayerDelegates ()
    {
        
#if (DEBUG)
        var dir = "wwwroot";
#else
        var dir = Path.Combine(DirectoryHelper.BaseDirectory, "Server/wwwroot");
#endif
        string font = Path.Combine(dir, "font.ttf");
        FontCollection collection = new();
        var family = collection.Add(font);
        // collection.TryGet("Font Name", out FontFamily font);
        Font = family.CreateFont(12, FontStyle.Regular);
    }
    
    /// <summary>
    /// Sets up the data layer delegate methods
    /// </summary>
    public static void Setup()
    {
        DataLayer.Helpers.DelegateHelpers.SvgToImageTag = ConvertSvgToBase64ImgTag;
        DataLayer.Helpers.DelegateHelpers.GenerateMultilineChart = GenerateMultilineChart;
    }
    
    /// <summary>
    /// Converts an SVG string to a Base64 encoded PNG image and returns it as an HTML img tag.
    /// </summary>
    /// <param name="svgContent">The SVG content as a string.</param>
    /// <param name="width">The width of the output PNG image.</param>
    /// <param name="height">The height of the output PNG image.</param>
    /// <returns>An HTML img tag with the Base64 encoded PNG image.</returns>
    public static string ConvertSvgToBase64ImgTag(string svgContent, int width, int height)
    {
        // Convert SVG string to ImageSharp image
        using (var svgStream = new MemoryStream(Encoding.UTF8.GetBytes(svgContent)))
        using (var image = Image.Load(svgStream))
        using (var memoryStream = new MemoryStream())
        {
            // Resize image if needed (optional)
            image.Mutate(x => x.Resize(width, height));

            // Save the ImageSharp image to a MemoryStream as a PNG
            image.SaveAsPng(memoryStream);
            string base64String = Convert.ToBase64String(memoryStream.ToArray());
            return $"<img src=\"data:image/png;base64,{base64String}\" alt=\"SVG Image\">";
        }
    }

    static string GenerateMultilineChart(MultilineChartData chartData)
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

    static string GenerateMultilineChartOld(MultilineChartData chartData)
    {
        try
        {
            int width = EmailChartWidth;
            int height = EmailChartHeight;

            // Initialize ImageSharp Image
            using var image = new Image<Rgba32>(width, height);

            // Create a font
            //var font = SystemFonts.CreateFont("Arial", 12);


            // Define chart dimensions and positions
            int chartStartX = 50;
            int chartStartY = 50;
            int chartWidth = image.Width - 100;
            int chartHeight = image.Height - 100;
            // Calculate maximum value from series data
            double maxValue = chartData.Series.SelectMany(s => s.Data).Max();

            // Draw chart elements using Mutate()
            image.Mutate(ctx =>
            {
                // Draw x-axis
                ctx.DrawLine(Color.Black, 1, new PointF(chartStartX, chartStartY),
                    new PointF(chartStartX + chartWidth, chartStartY));

                // Draw y-axis
                ctx.DrawLine(Color.Black, 1, new PointF(chartStartX, chartStartY),
                    new PointF(chartStartX, chartStartY + chartHeight));

                // Draw axis labels (example)
                ctx.DrawText("X Axis", Font, Color.Black,
                    new PointF(chartStartX + chartWidth / 2, chartStartY + chartHeight + 20));
                ctx.DrawText("Y Axis", Font, Color.Black, new PointF(chartStartX - 30, chartStartY + chartHeight / 2));

                // Example: Draw series data
                for (int seriesIndex = 0; seriesIndex < chartData.Series.Length; seriesIndex++)
                {
                    var series = chartData.Series[seriesIndex];
                    string color = COLORS[seriesIndex % COLORS.Length];

                    // Convert color string to Color object
                    var lineColor = Color.ParseHex(color);

                    // Example: Draw polyline for series
                    for (int i = 1; i < series.Data.Length; i++)
                    {
                        float x1 = chartStartX + (i - 1) * ((float)chartWidth / (series.Data.Length - 1));
                        float y1 = chartStartY + chartHeight - (float)(series.Data[i - 1] / maxValue) * chartHeight;

                        float x2 = chartStartX + i * ((float)chartWidth / (series.Data.Length - 1));
                        float y2 = chartStartY + chartHeight - (float)(series.Data[i] / maxValue) * chartHeight;

                        ctx.DrawLine(lineColor, 2, new PointF(x1, y1), new PointF(x2, y2));
                    }
                }
            });

            return ImageToBase64ImgTag(image);
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