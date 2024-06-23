using System.Globalization;

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