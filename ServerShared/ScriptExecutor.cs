using System.Text;
using FileFlows.Plugin;
using FileFlows.Plugin.Models;
using FileFlows.ScriptExecution;
using FileFlows.Shared.Models;

namespace FileFlows.ServerShared;

/// <summary>
/// A JavaScript code executor
/// </summary>
public class ScriptExecutor:IScriptExecutor
{
    /// <summary>
    /// The HTTP Client used in scripts for requests
    /// </summary>
    private static HttpClient httpClient = new ();
    
    /// <summary>
    /// Delegate used by the executor so log messages can be passed from the javascript code into the flow runner
    /// </summary>
    /// <param name="values">the parameters for the logger</param>
    delegate void LogDelegate(params object[] values);

    /// <summary>
    /// Gets or sets the shared directory 
    /// </summary>
    public string SharedDirectory { get; set; }
    
    /// <summary>
    /// Gets or sets the URL to the FileFlows server 
    /// </summary>
    public string FileFlowsUrl { get; set; }
    
    /// <summary>
    /// Gets or sets the plugin method invoker
    /// This allows plugins to expose static functions that can be called from functions/scripts
    /// </summary>
    public Func<string, string, object[], object> PluginMethodInvoker { get; set; }
    
    /// <summary>
    /// Executes javascript
    /// </summary>
    /// <param name="execArgs">the execution arguments</param>
    /// <returns>the output to be called next</returns>
    public int Execute(ScriptExecutionArgs execArgs)
    {
        if (string.IsNullOrEmpty(execArgs?.Code))
            return -1; // no code, flow cannot continue doesnt know what to do

        var args = execArgs.Args;
        
        Executor executor = new();
        executor.Logger = new ScriptExecution.Logger();
        executor.Logger.ELogAction = (largs) => args.Logger.ELog(largs);
        executor.Logger.WLogAction = (largs) => args.Logger.WLog(largs);
        executor.Logger.ILogAction = (largs) => args.Logger.ILog(largs);
        executor.Logger.DLogAction = (largs) => args.Logger.DLog(largs);
        executor.HttpClient = httpClient;
        if (string.IsNullOrWhiteSpace(FileFlowsUrl) == false)
            args.Variables["FileFlows.Url"] = FileFlowsUrl;

        executor.Variables = args.Variables;
        executor.SharedDirectory = SharedDirectory;

        
        executor.Code = execArgs.Code;
        if (execArgs.ScriptType == ScriptType.Flow && executor.Code.IndexOf("function Script", StringComparison.Ordinal) < 0)
        {
            executor.Code = "function Script() {\n" + executor.Code + "\n}\n";
            executor.Code += $"var scriptResult = Script();\nexport const result = scriptResult;";
        }
        
        executor.ProcessExecutor = new ScriptProcessExecutor(args);
        foreach (var arg in execArgs.AdditionalArguments ?? new ())
            executor.AdditionalArguments.Add(arg.Key, arg.Value);
        executor.AdditionalArguments["Flow"] = args;

        executor.AdditionalArguments["PluginMethod"] = PluginMethodInvoker;
        
        executor.SharedDirectory = SharedDirectory;
        try
        {
            object result = executor.Execute();
            if (result is int iOutput)
                return iOutput;
            return -1;
        }
        catch (Exception ex)
        {
            args.Logger.ELog("Failed executing: " + ex.Message);
            return -1;
        }
    }
    

    /// <summary>
    /// Executes code and returns the result
    /// </summary>
    /// <param name="code">the code to execute</param>
    /// <param name="variables">any variables to be passed to the executor</param>
    /// <param name="sharedDirectory">[Optional] the shared script directory to look in</param>
    /// <param name="dontLogCode">If the code should be included in the log if the execution fails</param>
    /// <returns>the result of the execution</returns>
    public static FileFlowsTaskRun Execute(string code, Dictionary<string, object> variables, string sharedDirectory = null, bool dontLogCode = false)
    {
        Executor executor = new Executor();
        executor.Code = code;
        executor.SharedDirectory = sharedDirectory?.EmptyAsNull() ?? DirectoryHelper.ScriptsDirectoryShared;
        executor.HttpClient = httpClient;
        executor.Logger = new ScriptExecution.Logger();
        executor.DontLogCode = dontLogCode;
        StringBuilder sbLog = new();
        executor.Logger.DLogAction = (args) => StringBuilderLog(sbLog, LogType.Debug, args);
        executor.Logger.ILogAction = (args) => StringBuilderLog(sbLog, LogType.Info, args);
        executor.Logger.WLogAction = (args) => StringBuilderLog(sbLog, LogType.Warning, args);
        executor.Logger.ELogAction = (args) => StringBuilderLog(sbLog, LogType.Error, args);
        executor.Variables = variables;
        try
        {
            object returnValue = executor.Execute();
            return new FileFlowsTaskRun()
            {
                Log = FixLog(sbLog),
                Success = true,
                ReturnValue = returnValue
            };
        }
        catch (Exception ex)
        {
            return new FileFlowsTaskRun()
            {
                Log = FixLog(sbLog),
                Success = false,
                ReturnValue = ex.Message
            };
        }

        string FixLog(StringBuilder sb)
            => sb.ToString()
                .Replace("\\n", "\n").Trim();
    }
    
        
        
        
    private static void StringBuilderLog(StringBuilder builder, LogType type, params object[] args)
    {
        string typeString = type switch
        {
            LogType.Debug => "[DBUG] ",
            LogType.Info => "[INFO] ",
            LogType.Warning => "[WARN] ",
            LogType.Error => "[ERRR] ",
            _ => "",
        };
        string message = typeString + string.Join(", ", args.Select(x =>
            x == null ? "null" :
            x.GetType().IsPrimitive ? x.ToString() :
            x is string ? x.ToString() :
            System.Text.Json.JsonSerializer.Serialize(x)));
        builder.AppendLine(message);
    }

    /// <summary>
    /// Script Process Executor that executes the process using node parameters
    /// </summary>
    class ScriptProcessExecutor : IProcessExecutor
    {
        private NodeParameters NodeParameters;
        
        /// <summary>
        /// Constructs an instance of a Script Process Executor 
        /// </summary>
        /// <param name="nodeParameters">the node parameters</param>
        public ScriptProcessExecutor(NodeParameters nodeParameters)
        {
            this.NodeParameters = nodeParameters;
        }
        
        /// <summary>
        /// Executes the process
        /// </summary>
        /// <param name="args">the arguments to execute</param>
        /// <returns>the result of the execution</returns>
        public ProcessExecuteResult Execute(ProcessExecuteArgs args)
        {
            var result = NodeParameters.Execute(new ExecuteArgs()
            {
                Arguments = args.Arguments,
                Command = args.Command,
                Timeout = args.Timeout,
                ArgumentList = args.ArgumentList,
                WorkingDirectory = args.WorkingDirectory
            });
            return new ProcessExecuteResult()
            {
                Completed = result.Completed,
                Output = result.Output,
                ExitCode = result.ExitCode,
                StandardError = result.StandardError,
                StandardOutput = result.StandardOutput,
            };
        }
    }
}