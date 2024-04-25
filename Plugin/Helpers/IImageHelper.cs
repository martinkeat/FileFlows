namespace FileFlows.Plugin.Helpers;

/// <summary>
/// Image helper
/// </summary>
public interface IImageHelper
{
    /// <summary>
    /// Draws a red rectangle on the specified image at the specified coordinates and dimensions.
    /// </summary>
    /// <param name="imagePath">The path to the input image.</param>
    /// <param name="x">The x-coordinate of the top-left corner of the rectangle.</param>
    /// <param name="y">The y-coordinate of the top-left corner of the rectangle.</param>
    /// <param name="width">The width of the rectangle.</param>
    /// <param name="height">The height of the rectangle.</param>
    void DrawRectangleOnImage(string imagePath, int x, int y, int width, int height);

    /// <summary>
    /// Gets the dimensions of an image
    /// </summary>
    /// <param name="imagePath">The path to the input image.</param>
    /// <returns>the dimensions</returns>
    Result<(int Width, int Height)> GetDimensions(string imagePath);

    /// <summary>
    /// Converts an image to JPG
    /// </summary>
    /// <param name="imagePath">The path to the input image.</param>
    /// <param name="destination">The destination where to save the new image to</param>
    /// <returns>true if successful, otherwise false</returns>
    Result<bool> ConvertToJpeg(string imagePath, string destination);
    
    /// <summary>
    /// Resizes the image to the specified dimensions while maintaining the aspect ratio.
    /// </summary>
    /// <param name="width">The new width of the image. Pass in 0 to maintain the aspect ratio.</param>
    /// <param name="height">The new height of the image. Pass in 0 to maintain the aspect ratio.</param>
    /// <param name="destination">The file path where the resized image will be saved.</param>
    /// <returns>A result indicating whether the resizing operation was successful or not.</returns>
    Result<bool> Resize(string imagePath, int width, int height, string destination);

}