namespace FileFlows.Server.Helpers;

/// <summary>
/// Helper for variables
/// </summary>
public class VariableHelper
{
    /// <summary>
    /// Gets the default FFmpeg Location
    /// </summary>
    /// <returns>the default FFmpeg location</returns>
    public static string GetDefaultFFmpegLocation()
    {
        if (OperatingSystem.IsWindows())
            return Path.Combine(DirectoryHelper.BaseDirectory, @"Tools\ffmpeg.exe");
        if(OperatingSystem.IsMacOS())
            return "/opt/homebrew/bin/ffmpeg";
        return "/usr/local/bin/ffmpeg";
    }
    /// <summary>
    /// Gets the default FFprobe Location
    /// </summary>
    /// <returns>the default FFprobe location</returns>
    public static string GetDefaultFFprobeLocation()
    {
        if (OperatingSystem.IsWindows())
            return Path.Combine(DirectoryHelper.BaseDirectory, @"Tools\ffprobe.exe");
        if(OperatingSystem.IsMacOS())
            return "/opt/homebrew/bin/ffprobe";
        return "/usr/local/bin/ffprobe";
    }
}