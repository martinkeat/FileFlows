using FileFlows.ServerShared.Helpers;

namespace FileFlows.FlowRunner.Helpers;

/// <summary>
/// Helper to prepare a flow run
/// </summary>
public class RunPreparationHelper
{
    
    /// <summary>
    /// Downloads the scripts being used
    /// </summary>
    internal static void DownloadScripts()
    {
        if (Directory.Exists(Program.WorkingDirectory) == false)
            Directory.CreateDirectory(Program.WorkingDirectory);
        
        DirectoryHelper.CopyDirectory(
            Path.Combine(Program.ConfigDirectory, "Scripts"),
            Path.Combine(Program.WorkingDirectory, "Scripts"));
    }
    
    /// <summary>
    /// Downloads the plugins being used
    /// </summary>
    internal static void DownloadPlugins()
    {
        var dir = Path.Combine(Program.ConfigDirectory, "Plugins");
        if (Directory.Exists(dir) == false)
            return;
        foreach (var sub in new DirectoryInfo(dir).GetDirectories())
        {
            string dest = Path.Combine(Program.WorkingDirectory, sub.Name);
            DirectoryHelper.CopyDirectory(sub.FullName, dest);
        }
    }
}