using System.Dynamic;
using System.Reflection;
using System.Text.RegularExpressions;
using FileFlows.FlowRunner.RunnerFlowElements;
using FileFlows.Plugin;
using FileFlows.Server;
using FileFlows.Shared;
using FileFlows.Shared.Models;

namespace FileFlows.FlowRunner.Helpers;

/// <summary>
/// Helper for executing the flow
/// </summary>
public class FlowHelper
{
    private static readonly Dictionary<Guid, Flow> FlowInstances = new();
    
    /// <summary>
    /// Creates the instance of the startup flow
    /// </summary>
    /// <param name="isRemote">if this is a remote flow and the file needs downloading</param>
    /// <param name="initialFlow">the initial flow to run after startup</param>
    /// <returns>the startup flow</returns>
    internal static Flow GetStartupFlow(bool isRemote, Flow initialFlow)
    {
        FlowInstances[initialFlow.Uid] = initialFlow;

        var flow = new Flow { Parts = new() };
        var partStartup = new FlowPart()
        {
            Uid = Guid.NewGuid(),
            FlowElementUid = typeof(StartupFlowElement).FullName,
            Name = "Startup",
            OutputConnections = new List<FlowConnection>(),
            Outputs = 1
        };
        flow.Parts.Add(partStartup);

        if (isRemote)
        {
            var partDownload = new FlowPart()
            {
                Uid = Guid.NewGuid(),
                FlowElementUid = typeof(FileDownloaderFlowElement).FullName,
                Name = "Downloading...",
                OutputConnections = new List<FlowConnection>(),
                Inputs = 1,
                Outputs = 1
            };
            flow.Parts.Add(partDownload);
            partStartup.OutputConnections.Add(new ()
            {
                Output = 1,
                Input = 1,
                InputNode = partDownload.Uid
            });
        }
        else
        {
            // a flow element that ensures the file exists, otherwise exit with mapping issue
        }

        var partSubFlow = CreateSubFlowPart(initialFlow);

        flow.Parts.Last().OutputConnections.Add(new ()
        {
            Output = 1,
            Input = 1,
            InputNode = partSubFlow.Uid
        });
        flow.Parts.Add(partSubFlow);
        
        
        // conenect the failure flow up to these so any sub flow will trigger a failure flow
        // try run FailureFlow
        // var failureFlow =
        //     Program.Config.Flows?.FirstOrDefault(x => x.Type == FlowType.Failure && x.Default && x.Enabled);
        // if (failureFlow != null)
        // {
        //     nodeParameters.UpdateVariables(new Dictionary<string, object>
        //     {
        //         { "FailedNode", CurrentNode?.Name },
        //         { "FlowName", Flow.Name }
        //     });
        //     ExecuteFlow(failureFlow, runFlows, failure: true);
        // }

        return flow;
    }

    /// <summary>
    /// Creates a flow part for a sub flow
    /// </summary>
    /// <param name="flow">the flow to create the sub flow part for</param>
    /// <returns>the flow part</returns>
    internal static FlowPart CreateSubFlowPart(Flow flow)
        => new ()
        {
            Uid = Guid.NewGuid(), // flow.Uid,
            FlowElementUid = typeof(ExecuteFlow).FullName,
            Name = flow.Name,
            Inputs = 1,
            OutputConnections = new List<FlowConnection>(),
            Model = ((Func<ExpandoObject>)(() =>
            {
                dynamic expandoObject = new ExpandoObject();
                expandoObject.Flow = flow;
                return expandoObject;
            }))()
        };

    /// <summary>
    /// Loads a flow element instance
    /// </summary>
    /// <param name="part">the part in the flow</param>
    /// <param name="variables">the variables that are executing in the flow from NodeParameters</param>
    /// <returns>the node instance</returns>
    /// <exception cref="Exception">If the flow element type cannot be found</exception>
    internal static Result<Node> LoadFlowElement(FlowPart part, Dictionary<string, object> variables)
    {
        if (part.Type == FlowElementType.Script)
        {
            // special type
            var nodeScript = new ScriptNode();
            nodeScript.Model = part.Model;
            string scriptName = part.FlowElementUid[7..]; // 7 to remove "Scripts." 
            nodeScript.Code = GetScriptCode(scriptName);
            if (string.IsNullOrEmpty(nodeScript.Code))
                return Result<Node>.Fail("Script not found");
            
            if(string.IsNullOrWhiteSpace(part.Name))
                part.Name = scriptName;
            return nodeScript;
        }
        
        if (part.FlowElementUid.EndsWith(".GotoFlow"))
        {
            // special case, dont use the BasicNodes execution of this, use the runners execution,
            // we have more control and can load it as a sub flow
            if (part.Model is IDictionary<string, object> dictModel == false)
                return Result<Node>.Fail("Failed to load model for GotoFlow flow element.");

            if (dictModel.TryGetValue("Flow", out object oFlow) == false || oFlow == null ||
                oFlow is ObjectReference orFlow == false)
                return Result<Node>.Fail("Failed to get flow from GotoFlow model.");

            var gotoFlow = Program.Config.Flows.FirstOrDefault(x => x.Uid == orFlow.Uid);
            if(gotoFlow == null)
                return Result<Node>.Fail("Failed to locate Flow defined in the GotoFlow flow element.");

            return new ExecuteFlow()
            {
                Flow = gotoFlow
            };
        }

        if (part.Type == FlowElementType.SubFlow)
        {
            string sUid = part.FlowElementUid[8..]; // remove SubFlow:
            var subFlow = Program.Config.Flows.FirstOrDefault(x => x.Uid.ToString() == sUid);
            if (subFlow == null)
                return Result<Node>.Fail($"Failed to locate sub flow '{sUid}'.");
            return new ExecuteFlow
            {
                Flow = subFlow,
                Properties = part.Model as IDictionary<string, object>,
            };
        }
        
        var nt = GetFlowElementType(part.FlowElementUid);
        if (nt == null)
        {
            return Result<Node>.Fail("Failed to load flow element: " + part.FlowElementUid);
        }

        return CreateFlowElementInstance(part, nt, variables);

    }

    private static Node CreateFlowElementInstance(FlowPart part, Type nt, Dictionary<string, object> variables)
    {
        var node = Activator.CreateInstance(nt);
        if(node == null)
            return default;

        if (node is SubFlowOutput sfOutput && Regex.IsMatch(part.Name, @"[\d]$") && int.TryParse(part.Name[^1..], out int sfOutputValue))
        {
            // special case for a SubFlowOutput[1-9], these are special flow elements from the UI that shorthand a SubFlowOutput 
            sfOutput.Output = sfOutputValue;
            return sfOutput;
        }
        
        var properties = nt.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        if (part.Model is IDictionary<string, object> dict)
        {
            foreach (var k in dict.Keys)
            {
                try
                {
                    if (k == "Name")
                        continue; // this is just the display name in the flow UI
                    var prop = properties.FirstOrDefault(x => x.Name == k);
                    if (prop == null)
                        continue;

                    if (dict[k] == null)
                        continue;

                    var value = Converter.ConvertObject(prop.PropertyType, dict[k]);
                    if (value != null)
                        prop.SetValue(node, value);
                }
                catch (Exception ex)
                {
                    Program.Logger?.ELog("Failed setting property: " + ex.Message + Environment.NewLine + ex.StackTrace);
                    Program.Logger?.ELog("Type: " + nt.Name + ", Property: " + k);
                }
            }
        }
        
        // load any values that have been set by properties
        foreach (var prop in properties)
        {
            string strongName = part.Name + "." + prop.Name;
            object varValue;
            if (variables.TryGetValue(part.Uid + "." + prop.Name, out varValue) == false 
                    && variables.TryGetValue(strongName, out varValue) == false)
                continue;
            
            var value = Converter.ConvertObject(prop.PropertyType, varValue);
            if (value != null)
                prop.SetValue(node, value);
        }

        return (Node)node;
    }


    /// <summary>
    /// Loads the code for a script
    /// </summary>
    /// <param name="scriptName">the name of the script</param>
    /// <returns>the code of the script</returns>
    private static string GetScriptCode(string scriptName)
    {
        if (scriptName.EndsWith(".js") == false)
            scriptName += ".js";
        var file = new FileInfo(Path.Combine(Program.ConfigDirectory, "Scripts", "Flow", scriptName));
        if (file.Exists == false)
            return string.Empty;
        return File.ReadAllText(file.FullName);
    }
    
    
    /// <summary>
    /// Gets the flow element type from the full name of a flow element
    /// </summary>
    /// <param name="fullName">the full name of the flow element</param>
    /// <returns>the type if known otherwise null</returns>
    internal static Type? GetFlowElementType(string fullName)
    {
        // special checks for our internal flow elements
        if (fullName.EndsWith("." + nameof(FileDownloaderFlowElement)))
            return typeof(FileDownloaderFlowElement);
        if (fullName.EndsWith("." + nameof(ExecuteFlow)))
            return typeof(ExecuteFlow);
        if (fullName.EndsWith("." + nameof(StartupFlowElement)))
            return typeof(StartupFlowElement);
        if (fullName.EndsWith(nameof(SubFlowInput)))
            return typeof(SubFlowInput);
        if (fullName.EndsWith(nameof(SubFlowOutput)) || fullName.StartsWith(nameof(SubFlowOutput)))
            return typeof(SubFlowOutput);
        
        foreach (var dll in new DirectoryInfo(Program.WorkingDirectory).GetFiles("*.dll", SearchOption.AllDirectories))
        {
            try
            {
                //var assembly = Context.LoadFromAssemblyPath(dll.FullName);
                var assembly = Assembly.LoadFrom(dll.FullName);
                var types = assembly.GetTypes();
                var pluginType = types.FirstOrDefault(x => x.IsAbstract == false && x.FullName == fullName);
                if (pluginType != null)
                    return pluginType;
            }
            catch (Exception ex)
            {
                Program.Logger.WLog("Failed to load assembly: " + dll.FullName + " > " + ex.Message);
            }
        }
        return null;
    }
}