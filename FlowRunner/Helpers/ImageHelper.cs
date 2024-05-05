using FileFlows.Plugin;
using FileFlows.Plugin.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Webp;
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
    public Result<bool> ConvertToJpeg(string imagePath, string destination, ImageOptions? options)
        => DoConvert(imagePath, destination, ImageType.Jpeg, options);

    /// <inheritdoc />
    public Result<bool> ConvertToWebp(string imagePath, string destination, ImageOptions? options)
        => DoConvert(imagePath, destination, ImageType.Webp, options);
    
    private Result<bool> DoConvert(string imagePath, string destination, ImageType type, ImageOptions? options)
    {
        if (File.Exists(imagePath) == false)
            return Result<bool>.Fail("Image does not exist");
        try
        {
            using var image = Image.Load(imagePath);
            
            if (options != null)
            {
                // resize, should maintain aspect ratio
                int newWidth = image.Width;
                int newHeight = image.Height;

                // If both Width and Height are specified, use them directly
                if (options.Width > 0 && options.Height > 0)
                {
                    newWidth = options.Width;
                    newHeight = options.Height;
                }
                // If only Width is specified, scale Height proportionally
                else if (options.Width > 0)
                {
                    newWidth = options.Width;
                    newHeight = (int)((float)image.Height / image.Width * options.Width);
                }
                // If only Height is specified, scale Width proportionally
                else if (options.Height > 0)
                {
                    newHeight = options.Height;
                    newWidth = (int)((float)image.Width / image.Height * options.Height);
                }
                else if (options.MaxWidth > 0 && options.MaxHeight == 0)
                {
                    // Only MaxWidth is specified
                    newWidth = options.MaxWidth;
                    newHeight = (int)(image.Height * ((float)options.MaxWidth / image.Width));
                }
                else if (options.MaxHeight > 0 && options.MaxWidth == 0)
                {
                    // Only MaxHeight is specified
                    newWidth = (int)(image.Width * ((float)options.MaxHeight / image.Height));
                    newHeight = options.MaxHeight;
                }
                else if (options.MaxHeight > 0 && options.MaxWidth > 0)
                {
                    // Both MaxHeight and MaxWidth are specified
                    float widthRatio = (float)options.MaxWidth / image.Width;
                    float heightRatio = (float)options.MaxHeight / image.Height;
                    float ratio = Math.Min(widthRatio, heightRatio);

                    newWidth = (int)(image.Width * ratio);
                    newHeight = (int)(image.Height * ratio);
                }


                if (newWidth != image.Width || newHeight != image.Height)
                {
                    // Resize the image
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(newWidth, newHeight),
                        Mode = ResizeMode.Max
                    }));
                }
            }

            IImageEncoder encoder;
            switch (type)
            {
                case ImageType.Jpeg:
                    encoder = new JpegEncoder()
                    {
                        Quality = options?.Quality ?? 75
                    };
                    break;
                case ImageType.Webp:
                    encoder = new WebpEncoder()
                    {
                        Quality = options?.Quality ?? 75
                    };
                    break;
                default:
                    throw new ArgumentException("Unsupported image type");
            }
            
            image.Save(destination, encoder);

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

    /// <summary>
    /// Image types
    /// </summary>
    private enum ImageType
    {
        /// <summary>
        /// JPEG image
        /// </summary>
        Jpeg,
        /// <summary>
        /// WEBP image
        /// </summary>
        Webp
    }
}