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
            FlowElementUid = typeof(Startup).FullName,
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
                FlowElementUid = typeof(FileDownloader).FullName,
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

        var partSubFlow = CreateSubFlowPart(initialFlow);

        flow.Parts.Last().OutputConnections.Add(new ()
        {
            Output = 1,
            Input = 1,
            InputNode = partSubFlow.Uid
        });
        flow.Parts.Add(partSubFlow);
        
        
        // connect the failure flow up to these so any sub flow will trigger a failure flow
        var failureFlow =
            Program.Config.Flows?.FirstOrDefault(x => x is { Type: FlowType.Failure, Default: true });
        if (failureFlow != null)
        {
            FlowPart fpFailure = new()
            {
                Uid = Guid.NewGuid(), // flow.Uid,
                FlowElementUid = typeof(ExecuteFlow).FullName,
                Name = failureFlow.Name,
                Inputs = 1,
                OutputConnections = new List<FlowConnection>(),
                Model = ((Func<ExpandoObject>)(() =>
                {
                    dynamic expandoObject = new ExpandoObject();
                    expandoObject.Flow = failureFlow;
                    return expandoObject;
                }))()
                
            };
            flow.Parts.Add(fpFailure);
            partSubFlow.ErrorConnection = new()
            {
                Output = -1,
                Input = 1,
                InputNode = fpFailure.Uid
            };
        }

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
    /// <param name="logger">The logger used to log</param>
    /// <param name="part">the part in the flow</param>
    /// <param name="variables">the variables that are executing in the flow from NodeParameters</param>
    /// <returns>the node instance</returns>
    /// <exception cref="Exception">If the flow element type cannot be found</exception>
    internal static Result<Node> LoadFlowElement(ILogger logger, FlowPart part, Dictionary<string, object> variables)
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

            if (dictModel.TryGetValue("Flow", out object oFlow) == false || oFlow == null)
                return Result<Node>.Fail("Failed to get flow from GotoFlow model.");

            ObjectReference? orFlow;
            string json = JsonSerializer.Serialize(oFlow);
            try
            {
                orFlow = JsonSerializer.Deserialize<ObjectReference>(json,new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception)
            {
                return Result<Node>.Fail("Failed to load GotoFlow model from: " + json);
            }
            if(orFlow == null)
                return Result<Node>.Fail("Failed to load GotoFlow model from: " + json);


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
            // add all the fields into the variables 
            if (subFlow.Properties?.Fields?.Any() == true && part.Model is IDictionary<string, object> subFlowModel)
            {
                foreach (var field in subFlow.Properties.Fields)
                {
                    if (string.IsNullOrWhiteSpace(field.FlowElementField))
                        continue;
                    if (subFlowModel.TryGetValue(field.Name, out object? fieldValue))
                    {
                        logger?.ILog(
                            $"Setting sub flow field variable [{field.FlowElementField}] = {fieldValue?.ToString() ?? "null"}");
                        variables[field.FlowElementField] = fieldValue;
                    }
                }
            }

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

        return CreateFlowElementInstance(logger, part, nt, variables);

    }

    /// <summary>
    /// Creates an instance of a flow element
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="part">the flow part to create an instance for</param>
    /// <param name="flowElementType">the flow element type</param>
    /// <param name="variables">the variables to load</param>
    /// <returns>an instance of the flow element</returns>
    private static Node CreateFlowElementInstance(ILogger logger, FlowPart part, Type flowElementType, Dictionary<string, object> variables)
    {
        var node = Activator.CreateInstance(flowElementType);
        if(node == null)
            return default;

        if (node is SubFlowOutput sfOutput && Regex.IsMatch(part.Name, @"[\d]$") && int.TryParse(part.Name[^1..], out int sfOutputValue))
        {
            // special case for a SubFlowOutput[1-9], these are special flow elements from the UI that shorthand a SubFlowOutput 
            sfOutput.Output = sfOutputValue;
            return sfOutput;
        }
        
        var properties = flowElementType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
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
                    Program.Logger?.ELog("Type: " + flowElementType.Name + ", Property: " + k);
                }
            }
        }
        
        // load any values that have been set by properties
        foreach (var prop in properties)
        {
            string strongName = part.Uid + "." + prop.Name;
            object? varValue;
            if (variables.TryGetValue(part.Uid + "." + prop.Name, out varValue) == false 
                    && variables.TryGetValue(strongName, out varValue) == false)
                continue;
            if (varValue == null)
                continue;

            if (varValue is JsonElement je)
            {
                switch (je.ValueKind)
                {
                    case JsonValueKind.False: varValue = (object)false;
                        break;
                    case JsonValueKind.True: varValue = (object)true;
                        break;
                    case JsonValueKind.String: varValue = je.GetString();
                        break;
                    case JsonValueKind.Number:
                        if (prop.PropertyType == typeof(long))
                            varValue = je.GetInt64();
                        else if (prop.PropertyType == typeof(float))
                            varValue = (float)je.GetDouble();
                        else if (prop.PropertyType == typeof(int))
                            varValue = je.GetInt32();
                        else if (prop.PropertyType == typeof(short))
                            varValue = je.GetInt16();
                        else if (je.TryGetInt32(out int i32))
                            varValue = i32;
                        else
                            continue;
                        break;
                }
            }

            logger?.ILog(strongName + " => Type Is: " + varValue.GetType().FullName);
            try
            {
                var value = Converter.ConvertObject(prop.PropertyType, varValue);
                if (value != null)
                    prop.SetValue(node, value);
            }
            catch (Exception ex)
            {
                logger.ELog("Failed setting variable: " + ex.Message);
            }
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
        if (fullName.EndsWith("." + nameof(FileDownloader)))
            return typeof(FileDownloader);
        if (fullName.EndsWith("." + nameof(ExecuteFlow)))
            return typeof(ExecuteFlow);
        if (fullName.EndsWith("." + nameof(Startup)))
            return typeof(Startup);
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