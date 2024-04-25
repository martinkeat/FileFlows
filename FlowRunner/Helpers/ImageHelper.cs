using FileFlows.Plugin;
using FileFlows.Plugin.Helpers;
using SixLabors.ImageSharp;
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
        if (File.Exists(imagePath) == false)
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

    /// <inheritdoc />
    public Result<(int Width, int Height)> GetDimensions(string imagePath)
    {
        if (File.Exists(imagePath) == false)
            return Result<(int Width, int Height)>.Fail("Image does not exist");
        try
        {
            using var image = Image.Load<Rgb24>(imagePath);
            return (image.Width, image.Height);
        }
        catch (Exception ex)
        {
            return Result<(int Width, int Height)>.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public Result<bool> ConvertToJpeg(string imagePath, string destination)
    {
        if (File.Exists(imagePath) == false)
            return Result<bool>.Fail("Image does not exist");
        try
        {
            using var image = Image.Load(imagePath);
            image.Save(destination, new JpegEncoder());
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public Result<bool> Resize(string imagePath, int width, int height, string destination)
    {
        // Validate input size
        if ((width == 0 && height == 0) || width < 0 || height < 0)
            return Result<bool>.Fail("Width and height must be positive values, or one dimension must be 0 to maintain aspect ratio.");

        if (File.Exists(imagePath) == false)
            return Result<bool>.Fail("Image does not exist");
        
        try
        {
            using var image = Image.Load(imagePath);
            
            image.Mutate(x => x.Resize(width, height));
            image.Save(destination);
            
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            // Return failure with the error message
            return Result<bool>.Fail(ex.Message);
        }
    }
}