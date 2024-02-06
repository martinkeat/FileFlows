using System.Diagnostics;
using FileFlows.Plugin;
using FileFlows.Plugin.Services;
using FileFlows.ServerShared;
using FileFlows.Shared;
using FileFlows.Shared.Models;

namespace FileFlows.FlowRunner.RunnerFlowElements;

/// <summary>
/// Startup of a run, downloads scripts, plugins etc
/// </summary>
public class StartupFlowElement : Node
{
    /// <summary>
    /// Executes the startup of a flow
    /// </summary>
    /// <param name="args">the node parameters</param>
    /// <returns>the next output</returns>
    public override int Execute(NodeParameters args)
    {
        // now we can initialize the file safely
        args.InitFile(args.WorkingFile);
        
        LogHeader(args, Program.ConfigDirectory, Program.ProcessingNode);
        Helpers.RunPreparationHelper.DownloadPlugins();
        Helpers.RunPreparationHelper.DownloadScripts();
        

        return 1;
    }
    
    
    /// <summary>
    /// Logs the version info for all plugins etc
    /// </summary>
    /// <param name="nodeParameters">the node parameters</param>
    /// <param name="configDirectory">the directory of the configuration</param>
    /// <param name="flow">the flow being executed</param>
    /// <param name="node">the node executing this flow</param>
    private static void LogHeader(NodeParameters nodeParameters, string configDirectory, ProcessingNode node) //, Flow flow)
    {
        nodeParameters.Logger!.ILog("Version: " + Globals.Version);
        if (Globals.IsDocker)
            nodeParameters.Logger!.ILog("Platform: Docker" + (Globals.IsArm ? " (ARM)" : string.Empty));
        else if (Globals.IsLinux)
            nodeParameters.Logger!.ILog("Platform: Linux" + (Globals.IsArm ? " (ARM)" : string.Empty));
        else if (Globals.IsWindows)
            nodeParameters.Logger!.ILog("Platform: Windows" + (Globals.IsArm ? " (ARM)" : string.Empty));
        else if (Globals.IsMac)
            nodeParameters.Logger!.ILog("Platform: Mac" + (Globals.IsArm ? " (ARM)" : string.Empty));

        nodeParameters.Logger!.ILog("File: " + nodeParameters.FileName);
        //nodeParameters.Logger!.ILog("Executing Flow: " + flow.Name);
        nodeParameters.Logger!.ILog("File Service: " + FileService.Instance.GetType().Name);
        nodeParameters.Logger!.ILog("Processing Node: " + node.Name);

        var dir = Path.Combine(configDirectory, "Plugins");
        if (Directory.Exists(dir))
        {
            foreach (var dll in new DirectoryInfo(dir).GetFiles("*.dll", SearchOption.AllDirectories))
            {
                try
                {
                    string version = string.Empty;
                    var versionInfo = FileVersionInfo.GetVersionInfo(dll.FullName);
                    if (versionInfo.CompanyName != "FileFlows")
                        continue;
                    version = versionInfo.FileVersion?.EmptyAsNull() ?? versionInfo.ProductVersion ?? string.Empty;
                    nodeParameters.Logger!.ILog("Plugin: " + dll.Name + " version " +
                                                (version?.EmptyAsNull() ?? "unknown"));
                }
                catch (Exception)
                {
                }
            }
        }

        Helpers.FFmpegHelper.LogFFmpegVersion(nodeParameters);

        foreach (var v in nodeParameters.Variables)
        {
            //if (v.Key.StartsWith("file.") || v.Key.StartsWith("folder.") || v.Key == "ext" ||  
            if ( v.Key.Contains(".Url") || v.Key.Contains("Key"))
                continue;
            
            nodeParameters.Logger!.ILog($"Variables['{v.Key}'] = {v.Value}");
        }
    }
}