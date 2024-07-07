using System.Text;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace FileFlows.Server.Charting;

public abstract class ImageChart
{
    /// <summary>
    /// The width for emailed charts
    /// </summary>
    public const int EmailChartWidth = 590;
    /// <summary>
    /// The height for emailed charts
    /// </summary>
    public const int EmailChartHeight = 210;
    
    /// <summary>
    /// The colors to show on the chart
    /// </summary>
    public static readonly string[] COLORS =
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
    protected static Font Font;
    
    static ImageChart ()
    {
        if (Font != null)
            return;
        
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
    /// Converts an Image object to a base64-encoded PNG and returns an HTML img tag.
    /// </summary>
    /// <param name="image">The Image object to convert.</param>
    /// <returns>An HTML img tag with base64-encoded PNG.</returns>
    public string ImageToBase64ImgTag(Image<Rgba32> image)
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