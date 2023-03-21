using System.Text.Json;
using FileFlows.ScriptExecution;
using Logger = FileFlows.Shared.Logger;

namespace FileFlows.Server;

using FileFlows.Plugin;
using System.Dynamic;


/// <summary>
/// A special node that is not shown in the UI and only created
/// by the Flow Runner to execute a script.
/// This Node exists in this plugin to make use of the Javascript executor
/// </summary>
public class ScriptNode:Node
{
    /// <summary>
    /// Gets the number of inputs of this node
    /// </summary>
    public override int Inputs => 1;

    /// <summary>
    /// Gets or sets the model to pass to the node
    /// </summary>
    public ExpandoObject Model { get; set; }

    /// <summary>
    /// Gets or sets the code to execute
    /// </summary>
    public string Code { get; set; }


    /// <summary>
    /// Executes the script node
    /// </summary>
    /// <param name="args">the NodeParameters passed into this from the flow runner</param>
    /// <returns>the output node to call next</returns>
    public override int Execute(NodeParameters args)
    {
        // will throw exception if invalid
        var script = new ScriptParser().Parse("ScriptNode", Code);

        // build up the entry point
        string epParams = string.Join(", ", script.Parameters?.Select(x => x.Name).ToArray());
        // all scripts must contain the "Script" method we then add this to call that 
        //string entryPoint = $"Script({epParams});";
        string entryPoint = $"var scriptResult = Script({epParams});\nexport const result = scriptResult;";

        var execArgs = new Plugin.Models.ScriptExecutionArgs
        {
            Args = args,
            ScriptType = ScriptType.Flow,
            //Code = ("try\n{\n\t" + Code.Replace("\n", "\n\t") + "\n\n\t" + entryPoint + "\n} catch (err) { \n\tLogger.ELog(`Error in script [${err.line}]: ${err}`);\n\treturn -1;\n}").Replace("\t", "   ")
            Code = (Code + "\n\n" + entryPoint).Replace("\t", "   ").Trim(),
            AdditionalArguments = new (),
        };

        if (script.Parameters?.Any() == true)
        {
            var dictModel = Model as IDictionary<string, object>;
            foreach (var p in script.Parameters) 
            {
                var value = dictModel?.ContainsKey(p.Name) == true ? dictModel[p.Name] : null;
                if (value is JsonElement je && je.ValueKind == JsonValueKind.String)
                {
                    var str = je.GetString();
                    if (string.IsNullOrWhiteSpace(str) == false)
                    {
                        Logger.Instance.ILog("Parameter is string replacing variables: " + str);
                        string replaced = args.ReplaceVariables(str);
                        if (replaced != str)
                        {
                            Logger.Instance.ILog("Variables replaced: " + replaced);
                            value = replaced;
                        }
                    }
                }
                execArgs.AdditionalArguments.Add(p.Name, value);
            }
        }

        return args.ScriptExecutor.Execute(execArgs);
    }
}
