using System.Globalization;
using FileFlows.Plugin;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;
using System.Net;
using System.Net.Sockets;
using FileFlows.Plugin.Services;
using FileFlows.RemoteServices;
using FileFlows.ServerShared;
using FileFlows.ServerShared.FileServices;
using FileFlows.ServerShared.Helpers;
using FileFlows.ServerShared.Models;
using FileFlows.Shared;
using Microsoft.VisualBasic;

namespace FileFlows.FlowRunner;

/// <summary>
/// Flow Runner
/// </summary>
public class Program
{

    /// <summary>
    /// Main entry point for the flow runner
    /// </summary>
    /// <param name="args">the command line arguments</param>
    public static void Main(string[] args)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

        RunInstance instance = new();
        int exitCode = instance.Run(args);
        instance.LogInfo("Exit Code: " + exitCode);
        Environment.ExitCode = exitCode;
    }

    #if(DEBUG)
    /// <summary>
    /// Used for debugging to capture full log and test the full log update method
    /// </summary>
    /// <param name="args">the args</param>
    /// <returns>the exit code and full log</returns>
    public static (int ExitCode, string Log) RunWithLog(string[] args)
    {
        RunInstance instance = new();
        int exitCode = instance.Run(args);
        return (exitCode,instance.Logger.ToString());
    }
    #endif
    
    

}