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

}