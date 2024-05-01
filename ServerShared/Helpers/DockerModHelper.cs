using System.Diagnostics;
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
    
    /// <summary>
    /// Executes a DockerMod, if does file does not exist on disk, this will write it
    /// </summary>
    /// <param name="mod">the DockerMod to execute</param>
    /// <param name="forceExecution">If this should run even if it has already been run</param>
    public static void Execute(DockerMod mod, bool forceExecution = false)
    {
        if (Globals.IsDocker == false)
            return; // Only run on Docker instances
        
        var directory = DirectoryHelper.DockerModsDirectory;
        var file = Path.Combine(directory, mod.Name + ".sh");
        if (Directory.Exists(directory) == false)
            Directory.CreateDirectory(directory);
        
        try
        {
            File.WriteAllText(file, mod.Code);
                
            // Set execute permission for the file
            Process.Start("chmod", $"+x {file}").WaitForExit();

            if (forceExecution == false && ExecutedDockerMods.TryGetValue(mod.Uid, out int value) && value == mod.Revision)
                return; // already executed
                
            // Run the file and capture output to string
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = file,
                RedirectStandardOutput = true,
                UseShellExecute = false
            });
            if (process == null) return;
            
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
                    
            var totalLength = 120;
            var modNameLength = mod.Name.Length;
            var sideLength = (totalLength - modNameLength - 14) / 2; // Subtracting 14 for the length of " Docker Mod: "
            var header = new string('-', sideLength) + " Docker Mod: " + mod.Name + new string('-', sideLength + (modNameLength % 2));
            Logger.Instance.ILog("\n" + header + "\n" + output + "\n" +
                                 new string('-', totalLength));
                    
            ExecutedDockerMods[mod.Uid] = mod.Revision;
        }
        catch (Exception ex)
        {
            Logger.Instance.WLog("Failed Running DockerMod: " + ex.Message);
        }
    }
    
    

    /// <summary>
    /// Deletes a DockerMod from disk
    /// </summary>
    /// <param name="mod">the DockerMod to delete</param>
    public static void DeleteFromDisk(DockerMod mod)
    {
        var file = Path.Combine(DirectoryHelper.DockerModsDirectory, mod.Name + ".sh");
        if(File.Exists(file))
            File.Delete(file);
    }
}