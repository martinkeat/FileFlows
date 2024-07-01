using System.Diagnostics;
using System.Text;
using FileFlows.Plugin;
using FileFlows.Shared.Models;

namespace FileFlows.ServerShared.Helpers;

/// <summary>
/// Helper for DockerMods
/// </summary>
public static class DockerModHelper
{
    /// <summary>
    /// A collection of executed docker mods
    /// </summary>
    private static Dictionary<Guid, int> ExecutedDockerMods = new();

    private static FairSemaphore _semaphore = new(1);
    
    /// <summary>
    /// Executes a DockerMod, if does file does not exist on disk, this will write it
    /// </summary>
    /// <param name="mod">the DockerMod to execute</param>
    /// <param name="forceExecution">If this should run even if it has already been run</param>
    /// <param name="outputCallback">Callback to log the output to</param>
    /// <returns>true if successful, otherwise the failure reason</returns>
    public static async Task<Result<bool>> Execute(DockerMod mod, bool forceExecution = false, Action<string>? outputCallback = null)
    {
        if (Globals.IsDocker == false)
            return true; // Only run on Docker instances

        await _semaphore.WaitAsync();

        try
        {
            var directory = DirectoryHelper.DockerModsDirectory;
            var file = Path.Combine(directory, mod.Name.Replace(" ","-").TrimStart('.').Replace("/", "-") + ".sh");
            if (Directory.Exists(directory) == false)
                Directory.CreateDirectory(directory);
            await File.WriteAllTextAsync(file, mod.Code.Replace("\r\n", "\n"));

            // Set execute permission for the file
            await Process.Start("chmod", $"+x {file}").WaitForExitAsync();

            if (forceExecution == false && ExecutedDockerMods.TryGetValue(mod.Uid, out int value) &&
                value == mod.Revision)
                return true; // already executed

            // Run dpkg to configure any pending package installations
            //await Process.Start("dpkg", "--configure -a").WaitForExitAsync();
            await Process.Start(new ProcessStartInfo
            {
                //FileName = "/bin/bash",
                FileName = "/bin/su",
                ArgumentList = { "c", "dpkg --configure -a" },
                UseShellExecute = false
            })!.WaitForExitAsync();

            // Run the file and capture output to string
            var process = Process.Start(new ProcessStartInfo
            {
                //FileName = "/bin/bash",
                FileName = "/bin/su",
                ArgumentList = { "-c", file },
                RedirectStandardOutput = true,
                RedirectStandardError = true, // Redirect standard error stream
                UseShellExecute = false,
                WorkingDirectory = DirectoryHelper.DockerModsDirectory
            });

            if (process == null)
            {
                Logger.Instance.WLog($"Failed Running DockerMod '{mod.Name}': Failed to start the process.");
                return Result<bool>.Fail($"Failed Running DockerMod '{mod.Name}': Failed to start the process.");
            }

            StringBuilder outputBuilder = new StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputBuilder.AppendLine(e.Data);
                    outputCallback?.Invoke(outputBuilder.ToString());
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputBuilder.AppendLine(e.Data);
                    outputCallback?.Invoke(outputBuilder.ToString());
                }
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine(); // Begin reading standard error stream asynchronously

            await process.WaitForExitAsync();
            int exitCode = process.ExitCode;
            string output = outputBuilder.ToString();

            var totalLength = 120;
            var modNameLength = mod.Name.Length;
            var sideLength = (totalLength - modNameLength - 14) / 2; // Subtracting 14 for the length of " Docker Mod: "

            if (exitCode != 0)
            {
                var header = new string('-', sideLength) + " Docker Mod Failed: " + mod.Name + " " +
                             new string('-', sideLength + (modNameLength % 2));
                Logger.Instance.ELog("\n" + header + "\n" + output + "\n" +
                                     new string('-', totalLength));
                return Result<bool>.Fail(output);
            }
            else
            {
                var header = new string('-', sideLength) + " Docker Mod: " + mod.Name + " " +
                             new string('-', sideLength + (modNameLength % 2));
                Logger.Instance.ILog("\n" + header + "\n" + output + "\n" +
                                 new string('-', totalLength));
            }

            ExecutedDockerMods[mod.Uid] = mod.Revision;
            return true;
        }
        catch (Exception ex)
        {
            Logger.Instance.WLog("Failed Running DockerMod: " + ex.Message);
            return Result<bool>.Fail("Failed Running DockerMod: " + ex.Message);
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    
    //
    // /// <summary>
    // /// Deletes a DockerMod from disk
    // /// </summary>
    // /// <param name="mod">the DockerMod to delete</param>
    // public static void DeleteFromDisk(DockerMod mod)
    // {
    //     var file = Path.Combine(DirectoryHelper.DockerModsDirectory, mod.Name + ".sh");
    //     if(File.Exists(file))
    //         File.Delete(file);
    // }

    /// <summary>
    /// Uninstalls any DockerMod that is not know
    /// </summary>
    /// <param name="mods">The known DockerMods</param>
    public static async Task UninstallUnknownMods(List<DockerMod> mods)
    {
        var modNames = mods?.Select(x => x.Name.ToLowerInvariant())?.ToList() ?? new (); // Convert to list for efficient lookup
        var modFiles = new DirectoryInfo(DirectoryHelper.DockerModsDirectory).GetFiles("*.sh");
        var unknownMods = modFiles
            .Where(file => !modNames.Contains(Path.GetFileNameWithoutExtension(file.Name).ToLowerInvariant())).ToList();

        foreach (var unknown in unknownMods)
        {
            string name = Path.GetFileNameWithoutExtension(unknown.Name);
            try
            {
                Logger.Instance.WLog($"About to uninstall DockerMod '{name}'");

                // Run the file and capture output to string
                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "/bin/su",
                    ArgumentList = { "-c", "\"" + unknown.FullName + "\" --uninstall" },
                    RedirectStandardOutput = true,
                    RedirectStandardError = true, // Redirect standard error stream
                    UseShellExecute = false,
                    WorkingDirectory = DirectoryHelper.DockerModsDirectory
                });

                if (process == null)
                {
                    Logger.Instance.WLog($"Failed Uninstalling DockerMod '{name}': Failed to start the process.");
                    continue;
                }

                StringBuilder outputBuilder = new StringBuilder();

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        outputBuilder.AppendLine(e.Data);
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        outputBuilder.AppendLine(e.Data);
                    }
                };

                process.BeginOutputReadLine();
                process.BeginErrorReadLine(); // Begin reading standard error stream asynchronously

                await process.WaitForExitAsync();
                int exitCode = process.ExitCode;
                string output = outputBuilder.ToString();

                var totalLength = 120;
                var modNameLength = unknown.Name.Length;
                var sideLength =
                    (totalLength - modNameLength - 14) / 2; // Subtracting 14 for the length of " Docker Mod: "

                if (exitCode != 0)
                {
                    var header = new string('-', sideLength) + " Docker Mod Uninstall Failed: " + name + " " +
                                 new string('-', sideLength + (modNameLength % 2));
                    Logger.Instance.ELog("\n" + header + "\n" + output + "\n" +
                                         new string('-', totalLength));
                }
                else
                {
                    var header = new string('-', sideLength) + " Docker Mod Uninstall: " + name + " " +
                                 new string('-', sideLength + (modNameLength % 2));
                    Logger.Instance.ILog("\n" + header + "\n" + output + "\n" +
                                         new string('-', totalLength));
                }
            }
            catch (Exception)
            {
                Logger.Instance.ELog("Failed to uninstall DockerMod: " + name);
            }

            unknown.Delete();
        }
    }
}