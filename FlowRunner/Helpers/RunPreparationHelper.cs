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
    /// <param name="runInstance">the run instance running this</param>
    internal static void DownloadScripts(RunInstance runInstance)
    {
        if (Directory.Exists(runInstance.WorkingDirectory) == false)
            Directory.CreateDirectory(runInstance.WorkingDirectory);
        
        DirectoryHelper.CopyDirectory(
            Path.Combine(runInstance.ConfigDirectory, "Scripts"),
            Path.Combine(runInstance.WorkingDirectory, "Scripts"));
    }
    
    /// <summary>
    /// Downloads the plugins being used
    /// </summary>
    /// <param name="runInstance">the run instance running this</param>
    internal static void DownloadPlugins(RunInstance runInstance)
    {
        var dir = Path.Combine(runInstance.ConfigDirectory, "Plugins");
        if (Directory.Exists(dir) == false)
            return;
        foreach (var sub in new DirectoryInfo(dir).GetDirectories())
        {
            string dest = Path.Combine(runInstance.WorkingDirectory, sub.Name);
            DirectoryHelper.CopyDirectory(sub.FullName, dest);
        }
    }
}