using FileFlows.Server;
using FileFlows.ServerShared;
using FileFlows.ServerShared.Helpers;
using FileFlows.Plugin;
using FileFlows.ServerShared.Services;
using FileFlows.Shared;
using FileFlows.Shared.Models;
using System.Reflection;

namespace FileFlows.FlowRunner;

/// <summary>
/// A runner instance, this is called as a standalone application that is fired up when FileFlows needs to process a file
/// it exits when done, free up any resources used by this process
/// </summary>
public class Runner
{
    private FlowExecutorInfo Info;
    private Flow Flow;
    private ProcessingNode Node;
    private CancellationTokenSource CancellationToken = new CancellationTokenSource();
    private bool Canceled = false;
    private string WorkingDir;
    //private string ScriptDir, ScriptSharedDir, ScriptFlowDir;

    /// <summary>
    /// Creates an instance of a Runner
    /// </summary>
    /// <param name="info">The execution info that is be run</param>
    /// <param name="flow">The flow that is being executed</param>
    /// <param name="node">The processing node that is executing this flow</param>
    /// <param name="workingDir">the temporary working directory to use</param>
    public Runner(FlowExecutorInfo info, Flow flow, ProcessingNode node, string workingDir)
    {
        this.Info = info;
        this.Flow = flow;
        this.Node = node;
        this.WorkingDir = workingDir;
    }

    /// <summary>
    /// A delegate for the flow complete event
    /// </summary>
    public delegate void FlowCompleted(Runner sender, bool success);
    /// <summary>
    /// An event that is called when the flow completes
    /// </summary>
    public event FlowCompleted OnFlowCompleted;
    private NodeParameters nodeParameters;

    private Node CurrentNode;

    private void RecordNodeExecution(string nodeName, string nodeUid, int output, TimeSpan duration, FlowPart part)
    {
        if (Info.LibraryFile == null)
            return;

        Info.LibraryFile.ExecutedNodes ??= new List<ExecutedNode>();
        Info.LibraryFile.ExecutedNodes.Add(new ExecutedNode
        {
            NodeName = nodeName,
            NodeUid = part.Type == FlowElementType.Script ? "ScriptNode" : nodeUid,
            Output = output,
            ProcessingTime = duration,
        });
    }

    /// <summary>
    /// Starts the flow runner processing
    /// </summary>
    public void Run()
    {
        var systemHelper = new SystemHelper();
        try
        {
            systemHelper.Start();
            var service = FlowRunnerService.Load();
            var updated = service.Start(Info).Result;
            if (updated == null)
                return; // failed to update
            var communicator = FlowRunnerCommunicator.Load(Info.LibraryFile.Uid);
            communicator.OnCancel += Communicator_OnCancel;
            bool finished = false;
            DateTime lastSuccessHello = DateTime.Now;
            var task = Task.Run(async () =>
            {
                while (finished == false)
                {
                    if (finished == false)
                    {
                        bool success = await communicator.Hello(Program.Uid, this.Info, nodeParameters);
                        if (success == false)
                        {
                            if (lastSuccessHello < DateTime.Now.AddMinutes(-2))
                            {
                                nodeParameters?.Logger?.ELog("Hello failed, cancelling flow");
                                Communicator_OnCancel();
                                return;
                            }
                            nodeParameters?.Logger?.WLog("Hello failed, if continues the flow will be canceled");
                        }
                        else
                        {
                            lastSuccessHello = DateTime.Now;
                        }
                    }

                    await Task.Delay(5_000);
                }
            });
            try
            {
                RunActual(communicator);
            }
            catch(Exception ex)
            {
                finished = true;
                task.Wait();
                
                if (Info.LibraryFile?.Status == FileStatus.Processing)
                    Info.LibraryFile.Status = FileStatus.ProcessingFailed;
                
                nodeParameters?.Logger?.ELog("Error in runner: " + ex.Message + Environment.NewLine + ex.StackTrace);
                throw;
            }
            finally
            {
                finished = true;
                task.Wait();
                communicator.OnCancel -= Communicator_OnCancel;
                communicator.Close();
            }
        }
        finally
        {
            try
            {
                Finish().Wait();
            }
            catch (Exception ex)
            {
                Logger.Instance.ELog("Failed 'Finishing' runner: " + ex.Message + Environment.NewLine + ex.StackTrace);
            }

            systemHelper.Stop();
        }
    }

    private void Communicator_OnCancel()
    {
        nodeParameters?.Logger?.ILog("##### CANCELING FLOW!");
        CancellationToken.Cancel();
        nodeParameters?.Cancel();
        Canceled = true;
        if (CurrentNode != null)
            CurrentNode.Cancel().Wait();
    }

    public async Task Finish()
    {
        if (nodeParameters?.Logger is FlowLogger fl)
        {
            Info.Log = fl.ToString();
            await fl.Flush();
        }

        if(nodeParameters?.OriginalMetadata != null)
            Info.LibraryFile.OriginalMetadata = nodeParameters.OriginalMetadata;
        if (nodeParameters?.Metadata != null)
            Info.LibraryFile.FinalMetadata = nodeParameters.Metadata;
        // calculates the final finger print
        Info.LibraryFile.FinalFingerprint =
            FileFlows.ServerShared.Helpers.FileHelper.CalculateFingerprint(Info.LibraryFile.OutputPath);

        await Complete();
        OnFlowCompleted?.Invoke(this, Info.LibraryFile.Status == FileStatus.Processed);
    }

    private void CalculateFinalSize()
    {
        if (nodeParameters.IsDirectory)
            Info.LibraryFile.FinalSize = nodeParameters.GetDirectorySize(nodeParameters.WorkingFile);
        else
        {
            Info.LibraryFile.FinalSize = nodeParameters.LastValidWorkingFileSize;

            try
            {
                if (Info.Fingerprint)
                {
                    Info.LibraryFile.Fingerprint = ServerShared.Helpers.FileHelper.CalculateFingerprint(nodeParameters.WorkingFile) ?? string.Empty;
                    nodeParameters?.Logger?.ILog("Final Fingerprint: " + Info.LibraryFile.Fingerprint);
                }
                else
                {
                    Info.LibraryFile.Fingerprint = string.Empty;
                }
            }
            catch (Exception ex)
            {
                nodeParameters?.Logger?.ILog("Error with fingerprinting: " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }
        nodeParameters?.Logger?.ILog("Original Size: " + Info.LibraryFile.OriginalSize);
        nodeParameters?.Logger?.ILog("Final Size: " + Info.LibraryFile.FinalSize);
        Info.LibraryFile.OutputPath = Node.UnMap(nodeParameters.WorkingFile);
        nodeParameters?.Logger?.ILog("Output Path: " + Info.LibraryFile.OutputPath);
        nodeParameters?.Logger?.ILog("Final Status: " + Info.LibraryFile.Status);

    }

    private async Task Complete()
    {
        DateTime start = DateTime.Now;
        do
        {
            try
            {
                CalculateFinalSize();

                var service = FlowRunnerService.Load();
                await service.Complete(Info);
                return;
            }
            catch (Exception) { }
            await Task.Delay(30_000);
        } while (DateTime.Now.Subtract(start) < new TimeSpan(0, 10, 0));
        Logger.Instance?.ELog("Failed to inform server of flow completion");
    }

    private void StepChanged(int step, string partName)
    {
        Info.CurrentPartName = partName;
        Info.CurrentPart = step;
        try
        {
            SendUpdate(Info, waitMilliseconds: 1000);
        }
        catch (Exception ex) 
        { 
            // silently fail, not a big deal, just incremental progress update
            Logger.Instance.WLog("Failed to record step change: " + step + " : " + partName);
        }
    }

    private void UpdatePartPercentage(float percentage)
    {
        float diff = Math.Abs(Info.CurrentPartPercent - percentage);
        if (diff < 0.1)
            return; // so small no need to tell server about update;
        if (LastUpdate > DateTime.Now.AddSeconds(-2))
            return; // limit updates to one every 2 seconds

        Info.CurrentPartPercent = percentage;

        try
        {
            SendUpdate(Info);
        }
        catch (Exception)
        {
            // silently fail, not a big deal, just incremental progress update
        }
    }

    private DateTime LastUpdate;
    private SemaphoreSlim UpdateSemaphore = new SemaphoreSlim(1);
    
    private void SendUpdate(FlowExecutorInfo info, int waitMilliseconds = 50)
    {
        if (UpdateSemaphore.Wait(waitMilliseconds) == false)
        {
            Logger.Instance.DLog("Failed to wait for SendUpdate semaphore");
            return;
        }

        try
        {
            LastUpdate = DateTime.Now;
            var service = FlowRunnerService.Load();
            service.Update(info);
        }
        catch (Exception)
        {
        }
        finally
        {
            UpdateSemaphore.Release();
        }
    }

    private void SetStatus(FileStatus status)
    {
        DateTime start = DateTime.Now;
        Info.LibraryFile.Status = status;
        if (status == FileStatus.Processed)
        {
            Info.LibraryFile.ProcessingEnded = DateTime.Now;
        }
        else if(status == FileStatus.ProcessingFailed)
        {
            Info.LibraryFile.ProcessingEnded = DateTime.Now;
        }
        do
        {
            try
            {
                CalculateFinalSize();
                SendUpdate(Info, waitMilliseconds: 1000);
                Logger.Instance?.DLog("Set final status to: " + status);
                return;
            }
            catch (Exception ex)
            {
                // this is more of a problem, its not ideal, so we do try again
                Logger.Instance?.WLog("Failed to set status on server: " + ex.Message);
            }
            Thread.Sleep(5_000);
        } while (DateTime.Now.Subtract(start) < new TimeSpan(0, 3, 0));
    }

    private void RunActual(IFlowRunnerCommunicator communicator)
    {
        nodeParameters = new NodeParameters(Node.Map(Info.LibraryFile.Name), new FlowLogger(communicator), Info.IsDirectory, Info.LibraryPath);
        nodeParameters.IsDocker = Globals.IsDocker;
        nodeParameters.IsWindows = Globals.IsWindows;
        nodeParameters.IsLinux = Globals.IsLinux;
        nodeParameters.IsMac = Globals.IsMac;
        nodeParameters.IsArm = Globals.IsArm;
        nodeParameters.PathMapper = (path) => Node.Map(path);
        nodeParameters.PathUnMapper = (path) => Node.UnMap(path);
        nodeParameters.ScriptExecutor = new ScriptExecutor()
        {
            SharedDirectory = Path.Combine(Info.ConfigDirectory, "Scripts", "Shared"),
            FileFlowsUrl = Service.ServiceBaseUrl,
            PluginMethodInvoker = PluginMethodInvoker
        };
        foreach (var variable in Info.Config.Variables)
        {
            if (nodeParameters.Variables.ContainsKey(variable.Key) == false)
                nodeParameters.Variables.Add(variable.Key, variable.Value);
        }

        FileFlows.Plugin.Helpers.FileHelper.DontChangeOwner = Node.DontChangeOwner;
        FileFlows.Plugin.Helpers.FileHelper.DontSetPermissions = Node.DontSetPermissions;
        FileFlows.Plugin.Helpers.FileHelper.Permissions = Node.Permissions;

        List<Guid> runFlows = new List<Guid>();
        runFlows.Add(Flow.Uid);

        nodeParameters.RunnerUid = Info.Uid;
        nodeParameters.TempPath = WorkingDir;
        nodeParameters.TempPathName = new DirectoryInfo(WorkingDir).Name;
        nodeParameters.RelativeFile = Info.LibraryFile.RelativePath;
        nodeParameters.PartPercentageUpdate = UpdatePartPercentage;
        Shared.Helpers.HttpHelper.Logger = nodeParameters.Logger;

        nodeParameters.Logger!.ILog("File: " + nodeParameters.FileName);
        nodeParameters.Logger!.ILog("Executing Flow: " + Flow.Name);
        nodeParameters.Logger!.ILog("Version: " + Globals.Version);
        if(Globals.IsDocker)
            nodeParameters.Logger!.ILog("Platform: Docker" + (Globals.IsArm ? " (ARM)" : string.Empty));
        else if(Globals.IsLinux)
            nodeParameters.Logger!.ILog("Platform: Linux" + (Globals.IsArm ? " (ARM)" : string.Empty));
        else if(Globals.IsWindows)
            nodeParameters.Logger!.ILog("Platform: Windows" + (Globals.IsArm ? " (ARM)" : string.Empty));
        else if(Globals.IsMac)
            nodeParameters.Logger!.ILog("Platform: Mac" + (Globals.IsArm ? " (ARM)" : string.Empty));

        DownloadPlugins();
        DownloadScripts();

        nodeParameters.Result = NodeResult.Success;
        nodeParameters.GetToolPathActual = (name) =>
        {
            var variable = Info.Config.Variables.Where(x => x.Key.ToLowerInvariant() == name.ToLowerInvariant())
                .Select(x => x.Value).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(variable))
                return variable;
            return Node.Map(variable);
        };
        nodeParameters.GetPluginSettingsJson = (pluginSettingsType) =>
        {
            string? json = null;
            Info.Config.PluginSettings?.TryGetValue(pluginSettingsType, out json);
            return json;
        };
        nodeParameters.StatisticRecorder = (name, value) =>
        {
            var statService = StatisticService.Load();
            statService.Record(name, value);
        };

        var status = ExecuteFlow(Flow, runFlows);
        SetStatus(status);
        if(status == FileStatus.ProcessingFailed && Canceled == false)
        {
            // try run FailureFlow
            var failureFlow =
                Info.Config.Flows?.FirstOrDefault(x => x.Type == FlowType.Failure && x.Default && x.Enabled);
            if (failureFlow != null)
            {
                nodeParameters.UpdateVariables(new Dictionary<string, object>
                {
                    { "FailedNode", CurrentNode?.Name },
                    { "FlowName", Flow.Name }
                });
                ExecuteFlow(failureFlow, runFlows, failure: true);
            }
        }
    }

    private FileStatus ExecuteFlow(Flow flow, List<Guid> runFlows, bool failure = false)
    { 
        int count = 0;
        ObjectReference? gotoFlow = null;
        nodeParameters.GotoFlow = (flow) =>
        {
            if (runFlows.Contains(flow.Uid))
                throw new Exception($"Flow '{flow.Uid}' ['{flow.Name}'] has already been executed, cannot link to existing flow as this could cause an infinite loop.");
            gotoFlow = flow;
        };

        // find the first node
        var part = flow.Parts.FirstOrDefault(x => x.Inputs == 0);
        if (part == null)
        {
            nodeParameters.Logger!.ELog("Failed to find Input node");
            return FileStatus.ProcessingFailed;
        }

        int step = 0;
        StepChanged(step, part.Name);

        // need to clear this in case the file is being reprocessed
        if(failure == false)
            Info.LibraryFile.ExecutedNodes = new List<ExecutedNode>();
       
        while (count++ < Math.Max(25, Info.Config.MaxNodes))
        {
            if (CancellationToken.IsCancellationRequested || Canceled)
            {
                nodeParameters.Logger?.WLog("Flow was canceled");
                nodeParameters.Result = NodeResult.Failure;
                return FileStatus.ProcessingFailed;
            }
            if (part == null)
            {
                nodeParameters.Logger?.WLog("Flow part was null");
                nodeParameters.Result = NodeResult.Failure;
                return FileStatus.ProcessingFailed;
            }

            DateTime nodeStartTime = DateTime.Now;
            try
            {

                CurrentNode = LoadNode(part!);

                if (CurrentNode == null)
                {
                    // happens when canceled    
                    nodeParameters.Logger?.ELog("Failed to load node: " + part.Name);                    
                    nodeParameters.Result = NodeResult.Failure;
                    return FileStatus.ProcessingFailed;
                }
                ++step;
                StepChanged(step, CurrentNode.Name);

                nodeParameters.Logger?.ILog(new string('=', 70));
                nodeParameters.Logger?.ILog($"Executing Node {(Info.LibraryFile.ExecutedNodes.Count + 1)}: {part.Label?.EmptyAsNull() ?? part.Name?.EmptyAsNull() ?? CurrentNode.Name} [{CurrentNode.GetType().FullName}]");
                nodeParameters.Logger?.ILog(new string('=', 70));

                gotoFlow = null; // clear it, in case this node requests going to a different flow
                
                nodeStartTime = DateTime.Now;
                int output = 0;
                try
                {
                    if (CurrentNode.PreExecute(nodeParameters) == false)
                        throw new Exception("PreExecute failed");
                    output = CurrentNode.Execute(nodeParameters);
                    RecordNodeFinish(nodeStartTime, output);
                }
                catch(Exception)
                {
                    output = -1;
                    throw;
                }

                if (gotoFlow != null)
                {
                    var newFlow = Info.Config.Flows.FirstOrDefault(x => x.Uid == gotoFlow.Uid);
                    if (newFlow == null)
                    {
                        nodeParameters.Logger?.ELog("Unable goto flow with UID:" + gotoFlow.Uid + " (" + gotoFlow.Name + ")");
                        nodeParameters.Result = NodeResult.Failure;
                        return FileStatus.ProcessingFailed;
                    }
                    flow = newFlow;

                    nodeParameters.Logger?.ILog("Changing flows to: " + newFlow.Name);
                    this.Flow = newFlow;
                    runFlows.Add(gotoFlow.Uid);

                    // find the first node
                    part = flow.Parts.Where(x => x.Inputs == 0).FirstOrDefault();
                    if (part == null)
                    {
                        nodeParameters.Logger!.ELog("Failed to find Input node");
                        return FileStatus.ProcessingFailed;
                    }
                    Info.TotalParts = flow.Parts.Count;
                    step = 0;
                }
                else
                {
                    nodeParameters.Logger?.DLog("output: " + output);
                    if (output == -1)
                    {
                        // the execution failed                     
                        nodeParameters.Logger?.ELog("node returned error code:", CurrentNode!.Name);
                        nodeParameters.Result = NodeResult.Failure;
                        return FileStatus.ProcessingFailed;
                    }
                    var outputNode = part.OutputConnections?.Where(x => x.Output == output)?.FirstOrDefault();
                    if (outputNode == null)
                    {
                        nodeParameters.Logger?.DLog("Flow completed");
                        // flow has completed
                        nodeParameters.Result = NodeResult.Success;
                        nodeParameters.Logger?.DLog("File status set to processed");
                        return FileStatus.Processed;
                    }

                    var newPart = outputNode == null ? null : flow.Parts.Where(x => x.Uid == outputNode.InputNode).FirstOrDefault();
                    if (newPart == null)
                    {
                        // couldn't find the connection, maybe bad data, but flow has now finished
                        nodeParameters.Logger?.WLog("Couldn't find output node, flow completed: " + outputNode?.Output);
                        return FileStatus.Processed;
                    }

                    part = newPart;
                }
            }
            catch (Exception ex)
            {
                nodeParameters.Result = NodeResult.Failure;
                nodeParameters.Logger?.ELog("Execution error: " + ex.Message + Environment.NewLine + ex.StackTrace);
                Logger.Instance?.ELog("Execution error: " + ex.Message + Environment.NewLine + ex.StackTrace);
                RecordNodeFinish(nodeStartTime, -1);
                return FileStatus.ProcessingFailed;
            }
        }
        nodeParameters.Logger?.ELog("Too many nodes in flow, processing aborted");
        return FileStatus.ProcessingFailed;

        void RecordNodeFinish(DateTime nodeStartTime, int output)
        {
            TimeSpan executionTime = DateTime.Now.Subtract(nodeStartTime);
            if(failure == false)
                RecordNodeExecution(part.Label?.EmptyAsNull() ?? part.Name?.EmptyAsNull() ?? CurrentNode.Name, part.FlowElementUid, output, executionTime, part);
            nodeParameters.Logger?.ILog("Node execution time: " + executionTime);
            nodeParameters.Logger?.ILog(new string('=', 70));
        }
    }

    private void DownloadScripts()
    {
        if (Directory.Exists(nodeParameters.TempPath) == false)
            Directory.CreateDirectory(nodeParameters.TempPath);
        
        DirectoryHelper.CopyDirectory(
            Path.Combine(Info.ConfigDirectory, "Scripts"),
            Path.Combine(nodeParameters.TempPath, "Scripts"));
    }
    
    private void DownloadPlugins()
    {
        var dir = Path.Combine(Info.ConfigDirectory, "Plugins");
        if (Directory.Exists(dir) == false)
            return;
        foreach (var sub in new DirectoryInfo(dir).GetDirectories())
        {
            string dest = Path.Combine(nodeParameters.TempPath, sub.Name);
            ServerShared.Helpers.DirectoryHelper.CopyDirectory(sub.FullName, dest);
        }
    }

    private Type? GetNodeType(string fullName)
    {
        foreach (var dll in new DirectoryInfo(WorkingDir).GetFiles("*.dll", SearchOption.AllDirectories))
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
            catch (Exception) { }
        }
        return null;
    }

    private Node LoadNode(FlowPart part)
    {
        if (part.Type == FlowElementType.Script)
        {
            // special type
            var nodeScript = new ScriptNode();
            nodeScript.Model = part.Model;
            string scriptName = part.FlowElementUid[7..]; // 7 to remove "Scripts." 
            nodeScript.Code = GetScriptCode(scriptName);
            if (string.IsNullOrEmpty(nodeScript.Code))
                throw new Exception("Script not found");
            
            if(string.IsNullOrWhiteSpace(part.Name))
                part.Name = scriptName;
            return nodeScript;
        }
        
        var nt = GetNodeType(part.FlowElementUid);
        if (nt == null)
            return new Node();
        var node = Activator.CreateInstance(nt);
        if (part.Model is IDictionary<string, object> dict)
        {
            foreach (var k in dict.Keys)
            {
                try
                {
                    if (k == "Name")
                        continue; // this is just the display name in the flow UI
                    var prop = nt.GetProperty(k, BindingFlags.Instance | BindingFlags.Public);
                    if (prop == null)
                        continue;

                    if (dict[k] == null)
                        continue;

                    var value = FileFlows.Shared.Converter.ConvertObject(prop.PropertyType, dict[k]);
                    if (value != null)
                        prop.SetValue(node, value);
                }
                catch (Exception ex)
                {
                    Logger.Instance?.ELog("Failed setting property: " + ex.Message + Environment.NewLine + ex.StackTrace);
                    Logger.Instance?.ELog("Type: " + nt.Name + ", Property: " + k);
                }
            }
        }
        if(node == null)
            return default;
        return (Node)node;

    }

    /// <summary>
    /// Loads the code for a script
    /// </summary>
    /// <param name="scriptName">the name of the script</param>
    /// <returns>the code of the script</returns>
    private string GetScriptCode(string scriptName)
    {
        if (scriptName.EndsWith(".js") == false)
            scriptName += ".js";
        var file = new FileInfo(Path.Combine(Info.ConfigDirectory, "Scripts", "Flow", scriptName));
        if (file.Exists == false)
            return string.Empty;
        return File.ReadAllText(file.FullName);
    }

    private object PluginMethodInvoker(string plugin, string method, object[] args)
    {
        var dll = new DirectoryInfo(WorkingDir).GetFiles(plugin + ".dll", SearchOption.AllDirectories).FirstOrDefault();
        if (dll == null)
        {
            Logger.Instance.ELog("Failed to locate plugin: " + plugin);
            return null;
        }

        try
        {
            //var assembly = Context.LoadFromAssemblyPath(dll.FullName);
            var assembly = Assembly.LoadFrom(dll.FullName);
            var type = assembly.GetTypes().FirstOrDefault(x => x.Name == "StaticMethods");
            if (type == null)
            {
                Logger.Instance.ELog("No static methods found in plugin: " + plugin);
                return null;
            }

            var methodInfo = type.GetMethod(method, BindingFlags.Public | BindingFlags.Static);
            if (methodInfo == null)
            {
                Logger.Instance.ELog($"Method not found in plugin: {plugin}.{method}");
                return null;
            }

            var result = methodInfo.Invoke(null, new[]
            {
                nodeParameters
            }.Union(args ?? new object[] { }).ToArray());
            return result;
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog($"Error executing plugin method [{plugin}.{method}]: " + ex.Message);
            return null;
        }
    }
}
