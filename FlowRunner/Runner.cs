using System.Diagnostics;
using FileFlows.Server;
using FileFlows.ServerShared;
using FileFlows.ServerShared.Helpers;
using FileFlows.Plugin;
using FileFlows.ServerShared.Services;
using FileFlows.Shared;
using FileFlows.Shared.Models;
using System.Reflection;
using System.Text.RegularExpressions;
using FileFlows.Plugin.Services;
using FileFlows.ServerShared.FileServices;

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

    /// <summary>
    /// Records the execution of a flow node
    /// </summary>
    /// <param name="nodeName">the name of the flow node</param>
    /// <param name="nodeUid">the UID of the flow node</param>
    /// <param name="output">the output after executing the flow node</param>
    /// <param name="duration">how long it took to execution</param>
    /// <param name="part">the flow node part</param>
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
    /// Downloads a file from the specified server URL and saves it to the temporary path.
    /// </summary>
    /// <param name="libFile">The LibraryFile object representing the file to download.</param>
    /// <param name="serverUrl">The URL of the server where the file is located.</param>
    /// <param name="tempPath">The temporary path where the downloaded file will be saved.</param>
    /// <returns>The full path of the downloaded file if successful, otherwise an empty string.</returns>
    private string DownloadFile(ILogger logger, LibraryFile libFile, string serverUrl, string tempPath)
    {
        string filename = Path.Combine(tempPath, new FileInfo(libFile.Name).Name);
        var result = new FileDownloader(logger, serverUrl, Program.Uid).DownloadFile(libFile.Name, filename).Result;
        if (result.IsFailed)
        {
            logger.ELog("Failed to remotely download file: " + result.Error);
            return string.Empty;
        }

        return filename;
    }

    /// <summary>
    /// Starts the flow runner processing
    /// </summary>
    public void Run(FlowLogger logger)
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
            logger.SetCommunicator(communicator);
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
                RunActual(logger);
            }
            catch (Exception ex)
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
        catch (Exception ex)
        {
            Program.Logger.ELog("Failure in runner: " + ex.Message + Environment.NewLine + ex.StackTrace);
        }
        finally
        {
            try
            {
                Finish().Wait();
            }
            catch (Exception ex)
            {
                Program.Logger.ELog("Failed 'Finishing' runner: " + ex.Message + Environment.NewLine + ex.StackTrace);
            }

            systemHelper.Stop();
        }
    }

    /// <summary>
    /// Called when the communicator receives a cancel request
    /// </summary>
    private void Communicator_OnCancel()
    {
        nodeParameters?.Logger?.ILog("##### CANCELING FLOW!");
        CancellationToken.Cancel();
        nodeParameters?.Cancel();
        Canceled = true;
        if (CurrentNode != null)
            CurrentNode.Cancel().Wait();
    }

    /// <summary>
    /// Finish executing of a file
    /// </summary>
    public async Task Finish()
    {
        string log = null;
        if (nodeParameters?.Logger is FlowLogger fl)
        {
            log = fl.ToString();
            await fl.Flush();
        }

        if(nodeParameters?.OriginalMetadata != null)
            Info.LibraryFile.OriginalMetadata = nodeParameters.OriginalMetadata;
        if (nodeParameters?.Metadata != null)
            Info.LibraryFile.FinalMetadata = nodeParameters.Metadata;
        // calculates the final finger print
        if (string.IsNullOrWhiteSpace(Info.LibraryFile.OutputPath) == false)
        {
            Info.LibraryFile.FinalFingerprint =
                FileFlows.ServerShared.Helpers.FileHelper.CalculateFingerprint(Info.LibraryFile.OutputPath);
        }

        await Complete(log);
        OnFlowCompleted?.Invoke(this, Info.LibraryFile.Status == FileStatus.Processed);
    }

    /// <summary>
    /// Calculates the final size of the file
    /// </summary>
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

    /// <summary>
    /// Called when the flow execution completes
    /// </summary>
    private async Task Complete(string log)
    {
        DateTime start = DateTime.Now;
        do
        {
            try
            {
                if(nodeParameters != null) // this is null if it fails to remotely download the file
                    CalculateFinalSize();

                var service = FlowRunnerService.Load();
                Info.LibraryFile.ProcessingEnded = DateTime.Now;
                await service.Complete(Info, log);
                return;
            }
            catch (Exception) { }
            await Task.Delay(30_000);
        } while (DateTime.Now.Subtract(start) < new TimeSpan(0, 10, 0));
        Program.Logger?.ELog("Failed to inform server of flow completion");
    }

    /// <summary>
    /// Called when the current flow step changes, ie it moves to a different node to execute
    /// </summary>
    /// <param name="step">the step index</param>
    /// <param name="partName">the step part name</param>
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
            Program.Logger.WLog("Failed to record step change: " + step + " : " + partName);
        }
    }

    /// <summary>
    /// Updates the currently steps completed percentage
    /// </summary>
    /// <param name="percentage">the percentage</param>
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

    /// <summary>
    /// When an update was last sent to the server to say this is still alive
    /// </summary>
    private DateTime LastUpdate;
    /// <summary>
    /// A semaphore to ensure only one update is set at a time
    /// </summary>
    private SemaphoreSlim UpdateSemaphore = new SemaphoreSlim(1);
    
    /// <summary>
    /// Sends an update to the server
    /// </summary>
    /// <param name="info">the information to send to the server</param>
    /// <param name="waitMilliseconds">how long to wait to send, if takes longer than this, it wont be sent</param>
    private void SendUpdate(FlowExecutorInfo info, int waitMilliseconds = 50)
    {
        if (UpdateSemaphore.Wait(waitMilliseconds) == false)
        {
            Program.Logger.DLog("Failed to wait for SendUpdate semaphore");
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

    /// <summary>
    /// Sets the status of file
    /// </summary>
    /// <param name="status">the status</param>
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
                Program.Logger?.DLog("Set final status to: " + status);
                return;
            }
            catch (Exception ex)
            {
                // this is more of a problem, its not ideal, so we do try again
                Program.Logger?.WLog("Failed to set status on server: " + ex.Message);
            }
            Thread.Sleep(5_000);
        } while (DateTime.Now.Subtract(start) < new TimeSpan(0, 3, 0));
    }

    /// <summary>
    /// Starts processing a file
    /// </summary>
    /// <param name="logger">the logger used to log messages</param>
    private void RunActual(FlowLogger logger)
    {
        string initialFile;
        if (Info.IsRemote)
        {
            initialFile = DownloadFile(logger, Info.LibraryFile, Service.ServiceBaseUrl, WorkingDir);
            if (string.IsNullOrEmpty(initialFile))
            {
                Info.LibraryFile.Status = FileStatus.MappingIssue;
                SendUpdate(Info, waitMilliseconds: 1000);
                return;
            }
        }
        else
        {
            initialFile = Info.LibraryFile.Name;
        }
        
        nodeParameters = new NodeParameters(initialFile, logger,
            Info.IsDirectory, Info.LibraryPath)
        {
            Enterprise = Info.Config.Enterprise,
            LibraryFileName = Info.LibraryFile.Name,
            IsRemote = Info.IsRemote,
            FileService = FileService.Instance
        };

        // set the method to replace variables
        // this way any path can have variables and will just automatically get replaced
        FileService.Instance.ReplaceVariables = nodeParameters.ReplaceVariables;
        FileService.Instance.Logger = logger;
        
        nodeParameters.HasPluginActual = (name) =>
        {
            var normalizedSearchName = Regex.Replace(name.ToLower(), "[^a-z]", string.Empty);
            return Info.Config.PluginNames?.Any(x =>
                Regex.Replace(x.ToLower(), "[^a-z]", string.Empty) == normalizedSearchName) == true;
        };
        nodeParameters.UploadFile = (string source, string destination) =>
        {
            var task = new FileUploader(logger, Service.ServiceBaseUrl, Program.Uid).UploadFile(source, destination);
            task.Wait();
            return task.Result;
        };
        nodeParameters.DeleteRemote = (path, ifEmpty, includePatterns) =>
        {
            var task = new FileUploader(logger, Service.ServiceBaseUrl, Program.Uid).DeleteRemote(path, ifEmpty, includePatterns);
            task.Wait();
            return task.Result.Success;
        };
        
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
            object value = variable.Value;
            if (variable.Value?.Trim()?.ToLowerInvariant() == "true")
                value = true;
            else if (variable.Value?.Trim()?.ToLowerInvariant() == "false")
                value = false;
            else if (Regex.IsMatch(variable.Value?.Trim(), @"^[\d](\.[\d]+)?$"))
                value = variable.Value.IndexOf(".", StringComparison.Ordinal) > 0 ? float.Parse(variable.Value) : int.Parse(variable.Value);
            
            nodeParameters.Variables.TryAdd(variable.Key, value);
        }

        LoadFlowVariables(Flow.Properties?.Variables);
        
        Plugin.Helpers.FileHelper.DontChangeOwner = Node.DontChangeOwner;
        Plugin.Helpers.FileHelper.DontSetPermissions = Node.DontSetPermissions;
        Plugin.Helpers.FileHelper.Permissions = Node.Permissions;

        List<Guid> runFlows = new List<Guid>();
        runFlows.Add(Flow.Uid);

        nodeParameters.RunnerUid = Info.Uid;
        nodeParameters.TempPath = WorkingDir;
        nodeParameters.TempPathName = new DirectoryInfo(WorkingDir).Name;
        nodeParameters.RelativeFile = Info.LibraryFile.RelativePath;
        nodeParameters.PartPercentageUpdate = UpdatePartPercentage;
        Shared.Helpers.HttpHelper.Logger = nodeParameters.Logger;

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
        
        LogHeader(nodeParameters, Info.ConfigDirectory, Flow);
        DownloadPlugins();
        DownloadScripts();

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

    /// <summary>
    /// Loads flow variables into the node parameters variables
    /// </summary>
    /// <param name="flowVariables">the variables</param>
    private void LoadFlowVariables(Dictionary<string, object> flowVariables)
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
            this.nodeParameters.Variables[variable.Key] = value;
        }

    }

    /// <summary>
    /// Logs the version info for all plugins etc
    /// </summary>
    /// <param name="nodeParameters">the node parameters</param>
    /// <param name="configDirectory">the directory of the configuration</param>
    /// <param name="flow">the flow being executed</param>
    private static void LogHeader(NodeParameters nodeParameters, string configDirectory, Flow flow)
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
        nodeParameters.Logger!.ILog("Executing Flow: " + flow.Name);

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

        LogFFmpegVersion(nodeParameters);
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
        //if(failure == false)
        //    Info.LibraryFile.ExecutedNodes = new List<ExecutedNode>();
       
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
                    // happens when canceled or when the node failed to load
                    if(part.Name == "FileFlows.VideoNodes.VideoFile")
                        nodeParameters.Logger?.ELog("Video Nodes Plugin missing, download the from the Plugins page");
                    else
                        nodeParameters.Logger?.ELog("Failed to load node: " + part.Name);                    
                    nodeParameters.Result = NodeResult.Failure;
                    return FileStatus.ProcessingFailed;
                }
                ++step;
                StepChanged(step, CurrentNode.Name);

                nodeParameters.Logger?.ILog(new string('=', 70));
                nodeParameters.Logger?.ILog($"Executing Node {(Info.LibraryFile.ExecutedNodes.Count + 1)}: {part.Label?.EmptyAsNull() ?? part.Name?.EmptyAsNull() ?? CurrentNode.Name} [{CurrentNode.GetType().FullName}]");
                nodeParameters.Logger?.ILog(new string('=', 70));
                nodeParameters.Logger?.ILog("Working File: " + nodeParameters.WorkingFile);

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
                    // update the flow properties if there are any
                    LoadFlowVariables(flow.Properties?.Variables);
                    Info.TotalParts = flow.Parts.Count;
                    step = 0;
                }
                else
                {
                    nodeParameters.Logger?.DLog("output: " + output);
                    if (output == -1)
                    {
                        // the execution failed                     
                        nodeParameters.Logger?.ELog("Flow Element returned error code: " + CurrentNode!.Name);
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
                Program.Logger?.ELog("Execution error: " + ex.Message + Environment.NewLine + ex.StackTrace);
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
            nodeParameters.Logger?.ILog("Node output: " + output);
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
            DirectoryHelper.CopyDirectory(sub.FullName, dest);
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
            catch (Exception ex)
            {
                Program.Logger.WLog("Failed to load assembly: " + dll.FullName + " > " + ex.Message);
            }
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
        {
            //throw new Exception("Failed to load Node: " + part.FlowElementUid);
            //return new Node();
            return null;
        }

        var node = Activator.CreateInstance(nt);
        if(node == null)
            return default;
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
            if (nodeParameters.Variables.TryGetValue(strongName, out object varValue) == false)
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
            Program.Logger.ELog("Failed to locate plugin: " + plugin);
            return null;
        }

        try
        {
            //var assembly = Context.LoadFromAssemblyPath(dll.FullName);
            var assembly = Assembly.LoadFrom(dll.FullName);
            var type = assembly.GetTypes().FirstOrDefault(x => x.Name == "StaticMethods");
            if (type == null)
            {
                Program.Logger.ELog("No static methods found in plugin: " + plugin);
                return null;
            }

            var methodInfo = type.GetMethod(method, BindingFlags.Public | BindingFlags.Static);
            if (methodInfo == null)
            {
                Program.Logger.ELog($"Method not found in plugin: {plugin}.{method}");
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
            Program.Logger.ELog($"Error executing plugin method [{plugin}.{method}]: " + ex.Message);
            return null;
        }
    }

    private static void LogFFmpegVersion(NodeParameters args)
    {
        string ffmpeg = args.GetToolPath("FFmpeg")?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(ffmpeg))
        {
            args.Logger.ILog("FFmpeg Version: Not configured");
            return; // no FFmpeg
        }

        try
        {
            Process process = new Process();
            process.StartInfo.FileName = ffmpeg;
            process.StartInfo.Arguments = "-version";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (string.IsNullOrEmpty(output))
            {
                args.Logger.ELog("Failed detecting FFmpeg version");
                return;

            }
            // Split the output into lines
            var line = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).First();
            
            
            string pattern = @"ffmpeg\s+version\s+(.*?)(?:\s+Copyright|$)";
            Regex regex = new Regex(pattern);
            Match match = regex.Match(line);
            var version = match.Success ? match.Groups[1].Value.Trim() : line;
            
            args.Logger.ILog("FFmpeg: " + version);
        }
        catch (Exception ex)
        {
            // Handle any exceptions that occurred during the process execution
            args.Logger.WLog("Failed detecting FFmpeg version: " + ex.Message);
        }
    }
}
