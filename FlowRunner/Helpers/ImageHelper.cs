using FileFlows.Plugin;
using FileFlows.Plugin.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace FileFlows.FlowRunner.Helpers;

/// <summary>
/// Image Helper
/// </summary>
public class ImageHelper : IImageHelper
{
    private readonly ILogger Logger;
    /// <summary>
    /// Initialises a new instance of the image helper
    /// </summary>
    /// <param name="logger">the logger</param>
    public ImageHelper(ILogger logger)
    {
        Logger = logger;
    }
    
    /// <inheritdoc />
    public void DrawRectangleOnImage(string imagePath, int x, int y, int width, int height)
    {
        // Check if the image path is empty or null
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            Logger?.WLog("Image path is empty or null.");
            return;
        }

        // Check if the image file exists
        if (!File.Exists(imagePath))
        {
            Logger?.WLog($"Image file does not exist: {imagePath}");
            return;
        }
        // Load the image from file
        using (var image = Image.Load<Rgb24>(imagePath))
        {
            Rectangle rectangle = new Rectangle(x, y, width, height);
            int thickness = (int)Math.Round(image.Width / 320f);

            image.Mutate(x => x.Draw(Color.Red, thickness, rectangle));
            
            // Overwrite the original image file with the modified image
            image.Save(imagePath);
        }
    }
}