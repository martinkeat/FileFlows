using System.Diagnostics;
using FileFlows.Plugin;
using FileFlows.Plugin.Helpers;
using FileFlows.Shared;

namespace FileFlows.FlowRunner.Helpers.ArchiveHelpers;

/// <summary>
/// 7zip archive helper for extracting and compressing 7zip archives
/// </summary>
public class SevenZipArchiveHelper : IArchiveHelper
{
    /// <summary>
    /// The 7zip executable
    /// </summary>
    private readonly string SevenZipExecutable;
    /// <summary>
    /// The logger used for logging
    /// </summary>
    private readonly ILogger Logger;

    /// <summary>
    /// Initializes a new instance of the Seven Zip Archive Helper
    /// </summary>
    /// <param name="args">The Node parameters</param>
    public SevenZipArchiveHelper(NodeParameters args)
    {
        SevenZipExecutable = args.GetToolPath("7zip")?.EmptyAsNull() ?? args.GetToolPath("7z")?.EmptyAsNull() ?? 
            (OperatingSystem.IsWindows() ? "7z.exe" : "7z");
        Logger = args.Logger;
    }

    /// <summary>
    /// Initializes a new instance of the Seven Zip Archive Helper
    /// </summary>
    /// <param name="logger">The logger</param>
    /// <param name="sevenZipExecutable">The 7zip executable</param>
    public SevenZipArchiveHelper(ILogger logger, string sevenZipExecutable)
    {
        Logger = logger;
        SevenZipExecutable = sevenZipExecutable;
    }

    /// <inheritdoc />
    public Result<bool> FileExists(string archivePath, string file)
    {
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = SevenZipExecutable,
                ArgumentList = { "l", archivePath },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processStartInfo);
            if (process == null)
                return Result<bool>.Fail("Failed to start 7z process.");

            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                string output = process.StandardOutput.ReadToEnd();
                return output.Contains(file, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                string error = process.StandardError.ReadToEnd();
                return Result<bool>.Fail($"Failed to list files in 7zip archive: {error}");
            }
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail($"Error while listing files in 7zip archive: {ex.Message}");
        }
    }


    /// <inheritdoc />
    public Result<bool> AddToArchive(string archivePath, string file)
    {
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = SevenZipExecutable,
                ArgumentList = { "a", archivePath, file },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processStartInfo);
            if (process == null)
                return Result<bool>.Fail("Failed to start 7z process.");

            process.WaitForExit();

            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail($"Failed to add file to 7zip archive: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public Result<int> GetFileCount(string archivePath, string pattern)
    {
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = SevenZipExecutable,
                ArgumentList = { "l", archivePath },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processStartInfo);
            if (process == null)
                return Result<int>.Fail("Failed to start 7z process.");

            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                var output = process.StandardOutput.ReadToEnd();
                var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                if (!string.IsNullOrWhiteSpace(pattern) && pattern != "*.*")
                {
                    lines = lines.Where(line => line.EndsWith(pattern, StringComparison.OrdinalIgnoreCase)).ToArray();
                }

                return lines.Length;
            }
            else
            {
                var error = process.StandardError.ReadToEnd();
                var output = process.StandardOutput.ReadToEnd();
                return Result<int>.Fail($"Failed to list files in 7zip archive: {error?.EmptyAsNull() ?? output}");
            }
        }
        catch (Exception ex)
        {
            return Result<int>.Fail($"Error while listing files in 7zip archive: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public Result<bool> Compress(string path, string output, string pattern = "", bool allDirectories = true, Action<float>? percentCallback = null)
    {
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = SevenZipExecutable,
                ArgumentList = { "a", output, Path.Combine(path, pattern) },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processStartInfo);
            if (process == null)
                return Result<bool>.Fail("Failed to start 7z process.");

            process.WaitForExit();

            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail($"Failed to create 7zip archive: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public Result<bool> Extract(string archivePath, string destinationPath, Action<float>? percentCallback = null)
    {
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = SevenZipExecutable,
                ArgumentList = { "x", archivePath, $"-o{destinationPath}", "-y" },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processStartInfo);
            if (process == null)
                return Result<bool>.Fail("Failed to start 7z process.");

            process.WaitForExit();

            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail($"Failed to extract 7zip archive: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if a file is a 7zip archive.
    /// </summary>
    /// <param name="archivePath">Path to the file.</param>
    /// <returns>True if the file is a 7zip archive, otherwise false.</returns>
    public bool IsSevenZipArchive(string archivePath)
    {
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = SevenZipExecutable,
                ArgumentList = { "l", archivePath },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processStartInfo);
            if (process == null)
                return false;

            process.WaitForExit();

            return process.ExitCode == 0;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
