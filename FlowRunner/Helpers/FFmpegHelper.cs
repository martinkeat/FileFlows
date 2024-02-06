using System.Diagnostics;
using System.Text.RegularExpressions;
using FileFlows.Plugin;

namespace FileFlows.FlowRunner.Helpers;

/// <summary>
/// Helper for FFmpeg
/// </summary>
public class FFmpegHelper
{
    /// <summary>
    /// Logs the current version of FFmpeg on the processing node
    /// </summary>
    /// <param name="args">the node parameters</param>
    internal static void LogFFmpegVersion(NodeParameters args)
    {
        string ffmpeg = args.GetToolPath("FFmpeg")?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(ffmpeg))
        {
            args.Logger.ILog("FFmpeg Version: Not configured");
            return; // no FFmpeg
        }

        try
        {
            Process process = new Process();
            process.StartInfo.FileName = ffmpeg;
            process.StartInfo.Arguments = "-version";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (string.IsNullOrEmpty(output))
            {
                args.Logger.ELog("Failed detecting FFmpeg version");
                return;

            }
            // Split the output into lines
            var line = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).First();
            
            
            string pattern = @"ffmpeg\s+version\s+(.*?)(?:\s+Copyright|$)";
            Regex regex = new Regex(pattern);
            Match match = regex.Match(line);
            var version = match.Success ? match.Groups[1].Value.Trim() : line;
            
            args.Logger.ILog("FFmpeg: " + version);
        }
        catch (Exception ex)
        {
            // Handle any exceptions that occurred during the process execution
            args.Logger.WLog("Failed detecting FFmpeg version: " + ex.Message);
        }
    }
}