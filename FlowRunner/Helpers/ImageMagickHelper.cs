using System.Diagnostics;
using FileFlows.Plugin;
using FileFlows.Plugin.Helpers;
using FileFlows.Shared;

namespace FileFlows.FlowRunner.Helpers;

/// <summary>
/// Helper for ImageMagick
/// </summary>
internal static class ImageMagickHelper
{
    private static bool? _CanUseImageMagick = null;
    /// <summary>
    /// Semaphore used to check if imagemagick can be used
    /// </summary>
    private static FairSemaphore _semaphore = new(1);
    
    /// <summary>
    /// Gets if ImageMagick can be used
    /// </summary>
    /// <returns></returns>
    public static bool CanUseImageMagick()
    {
        _semaphore.WaitAsync().Wait();
        try
        {
            if (_CanUseImageMagick == null)
            {
                bool isConvertAvailable = IsCommandAvailable("convert");
                bool isIdentifyAvailable = IsCommandAvailable("identify");
                _CanUseImageMagick = isConvertAvailable && isIdentifyAvailable;
            }

            return _CanUseImageMagick == true;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    

    /// <summary>
    /// Checks if the specified command is available in the system path.
    /// </summary>
    /// <param name="command">The name of the command to check.</param>
    /// <returns>True if the command is available, otherwise false.</returns>
    static bool IsCommandAvailable(string command)
    {
        try
        {
            // Start a process to execute the specified command with the --version argument
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Start the process
            using Process? process = Process.Start(startInfo);
            // Wait for the process to exit
            process.WaitForExit();
            // Check if the process exited successfully (exit code 0)
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            Logger.Instance.WLog(ex.Message);
            // An exception occurred, command is not available
            return false;
        }
    }
    
    /// <summary>
    /// Converts an image from one format to another and applies optional resizing options.
    /// </summary>
    /// <param name="imagePath">The path to the input image file.</param>
    /// <param name="destination">The path to save the converted image.</param>
    /// <param name="type">The target image type to convert to (e.g., Jpeg, Webp).</param>
    /// <param name="options">Optional parameters for resizing the image.</param>
    /// <returns>A result indicating whether the conversion was successful.</returns>
    public static Result<bool> ConvertImage(string imagePath, string destination, ImageHelper.ImageType type, ImageOptions? options)
    {
        try
        {
            // Get image dimensions using ImageMagick
            var result = GetImageDimensions(imagePath);
            if (result.Failed(out string error))
                return Result<bool>.Fail(error);

            (int width, int height) = result.Value;

            // Apply image resizing logic
            (int newWidth, int newHeight) = ImageHelper.CalculateNewDimensions(width, height, options);

            // Execute ImageMagick command for resizing
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "convert", // ImageMagick's convert command
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add(imagePath);
            startInfo.ArgumentList.Add("-resize");
            startInfo.ArgumentList.Add($"{newWidth}x{newHeight}!");
            if (options != null)
            {
                startInfo.ArgumentList.Add("-quality");
                startInfo.ArgumentList.Add(options.Quality.ToString());
            } 
            startInfo.ArgumentList.Add(destination);
            
            using Process? process = Process.Start(startInfo);
            process.WaitForExit();
            if (process.ExitCode != 0)
                return Result<bool>.Fail("Failed to resize image using ImageMagick");

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex.Message);
        }
    }

    /// <summary>
    /// Gets the image dimensions
    /// </summary>
    /// <param name="imagePath">the path to the file</param>
    /// <returns>the image dimensions</returns>
    public static Result<(int Width, int Height)> GetImageDimensions(string imagePath)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "identify", // ImageMagick's identify command
            ArgumentList = {"-format", "%w %h", imagePath},
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using Process? process = Process.Start(startInfo);
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
            return Result<(int, int)>.Fail("Failed to get image dimensions using ImageMagick");

        string[] dimensions = output.Trim().Split(' ');
        if (dimensions.Length != 2 || int.TryParse(dimensions[0], out var width) == false ||
            int.TryParse(dimensions[1], out var height) == false)
            return Result<(int, int)>.Fail("Invalid image dimensions retrieved from ImageMagick");

        return (width, height);
    }
}