using FileFlows.Plugin;
using FileFlows.Shared;
using FileFlows.Shared.Models;

namespace FileFlows.FlowRunner.RunnerFlowElements;

/// <summary>
/// Executes a flow
/// </summary>
public class FlowFlowElement : Node
{
    /// <summary>
    /// Gets or sets the flow to execute
    /// </summary>
    public Flow Flow { get; set; }
    
    /// <summary>
    /// Gets or sets the runner executing this flow
    /// </summary>
    public Runner Runner { get; set; }
    
    /// <summary>
    /// Gets or sets the depth this flow is executing
    /// </summary>
    public int FlowDepthLevel { get; set; }
    
    /// <summary>
    /// Gets or sets the properties for this sub flow that were entered into the fields from the user
    /// </summary>
    public IDictionary<string, object> Properties { get; set; }

    // Unicode character for vertical line
    const char verticalLine = '\u2514'; 
    // Unicode character for horizontal line
    const char horizontalLine = '\u2500'; 

    /// <summary>
    /// Loads this flows properties into the variables
    /// </summary>
    /// <param name="args">the node parameters</param>
    /// <param name="restoring">if the properties are being restored, happens after another sub flow was executed</param>
    private void LoadPropertiesInVariables(NodeParameters args, bool restoring = false)
    {
        if (Properties?.Any() != true)
            return;

        args.Logger?.ILog((restoring ? "Restoring" : "Loading") + " Flow Properties into variables");
        foreach (var prop in Properties)
        {
            args.Logger?.ILog(prop.Key + " = " + prop.Value);
            args.Variables[prop.Key] = prop.Value;
        }
    }

    /// <summary>
    /// Executes the flow
    /// </summary>
    /// <param name="args">the node parameters</param>
    /// <returns>the output from the flow</returns>
    public override int Execute(NodeParameters args)
    {
        // add this flows parts to the total
        Runner.Info.TotalParts += Flow.Parts.Count;
        
        if (Flow.Parts?.FirstOrDefault()?.FlowElementUid?.EndsWith("." + nameof(StartupFlowElement)) == true)
            FlowDepthLevel = -1; // special case for startup flow, set depth to -1 so first flow is at 0
        
        LoadPropertiesInVariables(args);
        LoadFlowVariables(args, Flow.Properties?.Variables);
       
        // find the first node
        var part = Flow.Parts.FirstOrDefault(x => x.Inputs == 0);
        if (part == null)
        {
            args.Logger?.ELog("Failed to find Input node");
            return -1;
        }

        // if (Runner.StepChanged(part.Name).Failed(out string error))
        // {
        //     args.Logger?.ELog(error);
        //     return RunnerCodes.TerminalExit; // this is a terminal exit, cannot continue
        // }


        int count = 0;
        while(++count < Math.Min(Program.Config.MaxNodes * 2, 300))
        {
            if (Runner.CancellationToken.IsCancellationRequested || Runner.Canceled)
            {
                args.Logger?.WLog("Flow was canceled");
                return RunnerCodes.RunCanceled;
            }
            if (part == null) // always false, but just in case code changes and this is no longer always false
            {
                args.Logger?.WLog("Flow part was null");
                return RunnerCodes.Failure;
            }

            DateTime nodeStartTime = DateTime.Now;
            Node? currentFlowElement = null;
            try
            {
                var lfeResult = Helpers.FlowHelper.LoadFlowElement(part, args.Variables);
                if(lfeResult.Failed(out string lfeError) || lfeResult.Value == null)
                {
                    if(string.IsNullOrWhiteSpace(lfeError) == false)
                        args.Logger?.ELog(lfeError);
                    // happens when canceled or when the node failed to load
                    if(part.Name == "FileFlows.VideoNodes.VideoFile")
                        args.Logger?.ELog("Video Nodes Plugin missing, download the from the Plugins page");
                    else
                        args.Logger?.ELog("Failed to load flow element: " + part.Name + "\nEnsure you have the required plugins installed.");       
                    return RunnerCodes.Failure;
                }

                currentFlowElement = lfeResult.Value;

                if (currentFlowElement is SubFlowOutput subOutput)
                    return subOutput.Output;

                if (part.FlowElementUid?.EndsWith("FlowInput") == true)
                    part.Name = Flow.Name; // entering this flow
                    
                
                if (Runner.StepChanged(part.Name, DontCountStep(part)).Failed(out var stepChangeError))
                {
                    args.Logger?.ELog(stepChangeError);
                    return RunnerCodes.TerminalExit; // this is a terminal exit, cannot continue
                }

                if (currentFlowElement is FlowFlowElement sub)
                {
                    sub.Runner = Runner;
                    sub.FlowDepthLevel = FlowDepthLevel + 1;
                    //if (sub.FlowPartPrefix == null)
                    // {
                    //     if (string.IsNullOrWhiteSpace(this.FlowPartPrefix))
                    //         sub.FlowPartPrefix = string.Empty + verticalLine + horizontalLine + " ";
                    //     else
                    //         sub.FlowPartPrefix = this.FlowPartPrefix.Trim() + horizontalLine + " ";
                    // }
                }

                args.Logger?.ILog(new string('=', 70));
                args.Logger?.ILog($"Executing Node {(Runner.Info.LibraryFile.ExecutedNodes.Count + 1)}: {part.Label?.EmptyAsNull() ?? part.Name?.EmptyAsNull() ?? currentFlowElement.Name} [{currentFlowElement.GetType().FullName}]");
                args.Logger?.ILog(new string('=', 70));
                args.Logger?.ILog("Working File: " + args.WorkingFile);

                
                nodeStartTime = DateTime.Now;
                
                if (currentFlowElement.PreExecute(args) == false)
                    throw new Exception("PreExecute failed");
                int output = currentFlowElement.Execute(args);
                if (output == RunnerCodes.TerminalExit)
                    return output; // just finish this, the flow element that caused the terminal exit already was recorded
                
                RecordFlowElementFinish(args, nodeStartTime, output, part, currentFlowElement);
                
                args.Logger?.DLog("output: " + output);
                if (output == RunnerCodes.Failure && part.ErrorConnection == null)
                {
                    // the execution failed                     
                    args.Logger?.ELog("Flow Element returned error code: " + currentFlowElement!.Name);
                    return RunnerCodes.Failure;
                }

                var outputNode = output == RunnerCodes.Failure
                    ? part.ErrorConnection
                    : part.OutputConnections?.FirstOrDefault(x => x.Output == output);
                
                if (outputNode == null)
                {
                    args.Logger?.ILog($"Flow '{Flow.Name}' completed");
                    // flow has completed
                    return RunnerCodes.Completed;
                }
                
                var newPart = Flow.Parts.FirstOrDefault(x => x.Uid == outputNode.InputNode);
                
                if (newPart == null)
                {
                    // couldn't find the connection, maybe bad data, but flow has now finished
                    args.Logger?.WLog("Couldn't find output node, flow completed: " + outputNode?.Output);
                    return RunnerCodes.Completed;
                }
                
                if(part.Type == FlowElementType.SubFlow)
                    LoadPropertiesInVariables(args, true);

                part = newPart;
            }
            catch (Exception ex)
            {
                args.Result = NodeResult.Failure;
                args.Logger?.ELog("Execution error: " + ex.Message + Environment.NewLine + ex.StackTrace);
                Program.Logger?.ELog("Execution error: " + ex.Message + Environment.NewLine + ex.StackTrace);
                if(currentFlowElement != null)
                    RecordFlowElementFinish(args, nodeStartTime, RunnerCodes.Failure, part, currentFlowElement);
                return RunnerCodes.Failure;
            }
        }
        
        args.Logger?.ELog("Too many nodes in flow, processing aborted");
        return RunnerCodes.TerminalExit;
    }

    /// <summary>
    /// Gets if the flow part should count towards the total
    /// </summary>
    /// <param name="part">the flow part to check</param>
    /// <returns>true if the flow part does NOT count towards total</returns>
    private bool DontCountStep(FlowPart part)
    {
        if (part.FlowElementUid.EndsWith("." + nameof(FlowFlowElement)))
            return true;
        if (part.FlowElementUid.EndsWith("." + nameof(StartupFlowElement)))
            return true;
        if (part.FlowElementUid.EndsWith("." + nameof(FileDownloaderFlowElement)))
            return true;
        if (part.FlowElementUid.EndsWith(nameof(SubFlowInput)))
            return true;
        if (part.FlowElementUid.EndsWith(nameof(SubFlowOutput)))
            return true;
        
        return false;
    }


    /// <summary>
    /// Records a flow element finishes
    /// </summary>
    /// <param name="args">the node arguments</param>
    /// <param name="startTime">the date the flow element started</param>
    /// <param name="output">the output from the flow element</param>
    /// <param name="part">the part the flow element was created from</param>
    /// <param name="flowElement">the flow element that was executed</param>
    void RecordFlowElementFinish(NodeParameters args, DateTime startTime, int output, FlowPart part, Node flowElement)
    {
        if (part.FlowElementUid?.EndsWith(nameof(SubFlowOutput)) == true ||
            part.FlowElementUid?.EndsWith("." + nameof(FlowFlowElement)) == true)
            return; // we dont record this output, the flow element that called the flow will record this for us
        
        TimeSpan executionTime = DateTime.Now.Subtract(startTime);
        string feName = part.Label?.EmptyAsNull() ?? part.Name?.EmptyAsNull() ?? flowElement.Name;
        string feElementUid = part.FlowElementUid;

        int depthAdjustment = 0;
        if (part.FlowElementUid.EndsWith(nameof(SubFlowInput)))
        {
            // record this as the sub flow
            depthAdjustment = -1;
            feName = Flow.Name + " Start";
            feElementUid = "Flow";
            output = 0; // special case, we want this to be green
        }
        else if (part.FlowElementUid.StartsWith("SubFlow:"))
        {
            feName = (part.Label?.EmptyAsNull() ?? part.Name) + " End";
            feElementUid = "Flow";
        }

        Runner.RecordNodeExecution(GetFlowPartPrefix(depthAdjustment) + feName, feElementUid, output, executionTime, part);
        args.Logger?.ILog("Flow Element execution time: " + executionTime);
        args.Logger?.ILog("Flow Element output: " + output);
        args.Logger?.ILog(new string('=', 70));
    }

    private string GetFlowPartPrefix(int depthAdjustment = 0)
    {
        int depth = FlowDepthLevel + depthAdjustment;
        if (depth < 1)
            return string.Empty;
        var prefix = string.Empty + verticalLine + horizontalLine;
        for (int i = 1; i < depth; i++)
            prefix += horizontalLine.ToString() + horizontalLine;
        
        return prefix + " ";

    }
    
    /// <summary>
    /// Loads flow variables into the node parameters variables
    /// </summary>
    /// <param name="flowVariables">the variables</param>
    private void LoadFlowVariables(NodeParameters args, Dictionary<string, object> flowVariables)
    {
        if (flowVariables?.Any() != true)
            return;
        
        foreach (var variable in flowVariables)
        {
            object value = variable.Value;
            if (value is JsonElement je)
            {
                if (je.ValueKind == JsonValueKind.False)
                    value = false;
                else if (je.ValueKind == JsonValueKind.True)
                    value = true;
                else if (je.ValueKind == JsonValueKind.Number)
                    value = je.GetInt32();
                else if (je.ValueKind == JsonValueKind.String)
                    value = je.GetString() ?? string.Empty;
                else
                    continue; // bad type
            }
            args.Variables[variable.Key] = value;
        }

    }
}