using FileFlows.Plugin;
using FileFlows.Plugin.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tiff;
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
            if (ImageMagickHelper.CanUseImageMagick())
            {
                var result = ImageMagickHelper.GetImageDimensions(imagePath);
                if (result.IsFailed == false)
                    return result.Value;
            }

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
            if (ImageMagickHelper.CanUseImageMagick())
            {
                var result = ImageMagickHelper.ConvertImage(imagePath, destination, type, options);
                if (result.IsFailed == false)
                    return true;
            }

            using var image = Image.Load(imagePath);
            
            if (options != null)
            {
                (int newWidth, int newHeight) = CalculateNewDimensions(image.Width, image.Height, options);

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

    /// <inheritdoc />
    public Result<string> SaveImage(byte[] imageBytes, string fileNameNoExtension)
    {
        if (imageBytes?.Any() != true)
            return Result<string>.Fail("No image bytes");
        if (string.IsNullOrWhiteSpace(fileNameNoExtension))
            return Result<string>.Fail("No file name");
        try
        {
            using var image = Image.Load(imageBytes);

            // Infer the image format
            (IImageFormat? imageFormat, string? fileExtension) = InferImageFormat(imageBytes);
            if (imageFormat == null)
            {
                Logger?.WLog("Failed to inter image type from PDF, failing back to JPG");
                imageFormat = JpegFormat.Instance;
                fileExtension = "jpg";
            }
            else
            {
                Logger?.ILog("File Extension of image: " + fileExtension);
            }

            var file = fileNameNoExtension.TrimEnd('.') + "." + fileExtension;
            var fileInfo = new FileInfo(file);
            
            if (fileInfo.Directory.Exists == false)
                fileInfo.Directory.Create();

            using var outputStream = File.Create(file);
            image.Save(outputStream, imageFormat);
            return file;
        }
        catch (Exception ex)
        {
            return Result<string>.Fail(ex.Message);
        }
    }


    /// <summary>
    /// Infers the image format based on the first few bytes of the image data.
    /// </summary>
    /// <param name="bytes">The image data bytes.</param>
    /// <returns>The inferred image format and file extension.</returns>
    private static (IImageFormat? Format, string? Extension) InferImageFormat(byte[] bytes)
    {
        // Try to infer image format based on magic numbers
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xD8) // JPEG
            return (JpegFormat.Instance, "jpg");
        if (bytes.Length >= 8 && BitConverter.ToUInt64(bytes, 0) == 0x89504E470D0A1A0A) // PNG
            return (PngFormat.Instance, "png");
        if (bytes.Length >= 4 && bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x38) // GIF
            return (GifFormat.Instance, "gif");
        if (bytes.Length >= 4 && bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 &&
            bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50) // WebP
            return (WebpFormat.Instance, "webp");
        if (bytes.Length >= 4 && BitConverter.ToUInt32(bytes, 0) == 0x49492A00) // TIFF
            return (TiffFormat.Instance, "tiff");
        if (bytes.Length >= 2 && bytes[0] == 0x42 && bytes[1] == 0x4D) // BMP
            return (BmpFormat.Instance, "bmp");

        // If none of the known formats are detected, fall back to Image.DetectFormat()
        try
        {
            IImageFormat format = Image.DetectFormat(bytes);
            string extension = format?.DefaultMimeType?.Split('/')[1] ?? "png";
            return (format, extension);
        }
        catch (Exception)
        {
            return (null, null);
        }
    }

    /// <summary>
    /// Calculates the new dimensions for resizing an image based on the provided options.
    /// </summary>
    /// <param name="width">The original width of the image.</param>
    /// <param name="height">The original height of the image.</param>
    /// <param name="options">The options specifying the desired dimensions or constraints for resizing.</param>
    /// <returns>A tuple containing the new width and height for the resized image.</returns>
    internal static (int Width, int Height) CalculateNewDimensions(int width, int height, ImageOptions? options)
    {
        int newWidth = width;
        int newHeight = height;

        if (options != null)
        {
            // Calculate new dimensions based on options
            if (options.Width > 0 && options.Height > 0)
            {
                // Both width and height are specified, use them directly
                newWidth = options.Width;
                newHeight = options.Height;
            }
            else if (options.Width > 0)
            {
                // Only width is specified, scale height proportionally
                newWidth = options.Width;
                newHeight = (int)Math.Round((double)height / width * options.Width);
            }
            else if (options.Height > 0)
            {
                // Only height is specified, scale width proportionally
                newWidth = (int)Math.Round((double)width / height * options.Height);
                newHeight = options.Height;
            }
            else if (options.MaxWidth > 0 && options.MaxHeight > 0)
            {
                // Both max width and max height are specified, scale the image down to fit within the bounds
                double widthRatio = (double)width / options.MaxWidth;
                double heightRatio = (double)height / options.MaxHeight;
                double maxRatio = Math.Max(widthRatio, heightRatio);

                newWidth = (int)Math.Round(width / maxRatio);
                newHeight = (int)Math.Round(height / maxRatio);
            }
            else if (options.MaxWidth > 0)
            {
                // Only max width is specified, scale the image down to fit within the width
                double ratio = (double)width / options.MaxWidth;
                newWidth = options.MaxWidth;
                newHeight = (int)Math.Round(height / ratio);
            }
            else if (options.MaxHeight > 0)
            {
                // Only max height is specified, scale the image down to fit within the height
                double ratio = (double)height / options.MaxHeight;
                newWidth = (int)Math.Round(width / ratio);
                newHeight = options.MaxHeight;
            }
        }

        return (newWidth, newHeight);
    }

    /// <summary>
    /// Image types
    /// </summary>
    internal enum ImageType
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