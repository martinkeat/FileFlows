using FileFlows.ServerShared;
using FileFlows.ServerShared.Helpers;
using FileFlows.Plugin;
using FileFlows.ServerShared.Services;
using FileFlows.Shared;
using FileFlows.Shared.Models;
using System.Text.RegularExpressions;
using FileFlows.FlowRunner.Helpers;
using FileFlows.FlowRunner.RunnerFlowElements;
using FileFlows.Plugin.Services;
using FileFlows.ServerShared.FileServices;

namespace FileFlows.FlowRunner;

/// <summary>
/// A runner instance, this is called as a standalone application that is fired up when FileFlows needs to process a file
/// it exits when done, free up any resources used by this process
/// </summary>
public class Runner
{
    internal FlowExecutorInfo Info { get; private set; }
     private Flow Flow;
    private ProcessingNode Node;
    internal readonly CancellationTokenSource CancellationToken = new CancellationTokenSource();
    internal bool Canceled { get; private set; }
    private string WorkingDir;

    /// <summary>
    /// The number of flow elements that currently have been executed
    /// </summary>
    private int ExecutedSteps = 0;

    /// <summary>
    /// The number of flow elements that have been executed and that count towards the total allowed
    /// We dont count steps like startup, enter sub flow, sub flow output etc
    /// </summary>
    private int ExecutedStepsCountedTowardsTotal = 0;
    
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
    /// <param name="flowDepth">the depth of the executed flow</param>
    internal void RecordNodeExecution(string nodeName, string nodeUid, int output, TimeSpan duration, FlowPart part, int flowDepth)
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
            Depth = flowDepth,
        });

        _ = SendUpdate(Info);
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

                nodeParameters?.Logger?.ELog("Error in runner: " + ex.Message + Environment.NewLine + ex.StackTrace);
                
                if (string.IsNullOrWhiteSpace(nodeParameters.FailureReason))
                    nodeParameters.FailureReason = "Error in runner: " + ex.Message;
                if (Info.LibraryFile?.Status == FileStatus.Processing)
                    SetStatus(FileStatus.ProcessingFailed);
                    //Info.LibraryFile.Status = FileStatus.ProcessingFailed;

                //throw;
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
            Info.LibraryFile.FinalFingerprint = FileHelper.CalculateFingerprint(Info.LibraryFile.OutputPath);
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
    /// <param name="partName">the step part name</param>
    /// <param name="dontCountTowardsTotal">if this step should not count towards the total number of steps allowed</param>
    internal Result<bool> StepChanged(string partName, bool dontCountTowardsTotal = false)
    {
        ++ExecutedSteps;
        if (dontCountTowardsTotal == false)
            ++ExecutedStepsCountedTowardsTotal;
        
        if (ExecutedStepsCountedTowardsTotal > Program.Config.MaxNodes)
             return Result<bool>.Fail("Exceeded maximum number of flow elements to process");

        Info.AdditionalInfos.Clear(); // clear current additional info, may need to change this in the future
        Info.CurrentPartName = partName;
        Info.CurrentPart = ExecutedSteps;
        Info.CurrentPartPercent = 0;
        try
        {
            _ = SendUpdate(Info, waitMilliseconds: 1000);
        }
        catch (Exception ex) 
        { 
            // silently fail, not a big deal, just incremental progress update
            Program.Logger.WLog("Failed to record step change: " + ExecutedSteps + " : " + partName);
        }

        return true;
    }

    /// <summary>
    /// Updates the currently steps completed percentage
    /// </summary>
    /// <param name="percentage">the percentage</param>
    private void UpdatePartPercentage(float percentage)
    {
        Info.CurrentPartPercent = percentage;
        try
        {
            _ = SendUpdate(Info);
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
    private async Task SendUpdate(FlowExecutorInfo info, int waitMilliseconds = 50)
    {
        if (await UpdateSemaphore.WaitAsync(waitMilliseconds) == false)
        {
            // Program.Logger.DLog("Failed to wait for SendUpdate semaphore");
            return;
        }

        try
        {
            if(waitMilliseconds != 1000) // 1000 is the delay for finishing / step changes
                await Task.Delay(500);
            LastUpdate = DateTime.Now;
            var service = FlowRunnerService.Load();
            await service.Update(info);
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
                SendUpdate(Info, waitMilliseconds: 1000).Wait();
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
        // string initialFile = null;
        // int additionalPartsForTotal = 0;
        // if (Info.IsRemote)
        // {
        //     if (Program.Config.AllowRemote)
        //     {
        //         Info.TotalParts = Flow.Parts.Count + 1;
        //         Info.CurrentPart = 1;
        //         Info.CurrentPartName = "Downloading";
        //         logger.ILog("Downloading Parts: " + Info.TotalParts);
        //         initialFile = DownloadFile(logger, Info.LibraryFile, Service.ServiceBaseUrl, WorkingDir);
        //         additionalPartsForTotal = 1;
        //     }
        //
        //     if (string.IsNullOrEmpty(initialFile))
        //     {
        //         Info.LibraryFile.Status = FileStatus.MappingIssue;
        //         SendUpdate(Info, waitMilliseconds: 1000).Wait();
        //         return;
        //     }
        // }
        // else
        // {
        //     initialFile = Info.LibraryFile.Name;
        // }
        
        nodeParameters = new NodeParameters(Info.LibraryFile.Name, logger,
            Info.IsDirectory, Info.LibraryPath, fileService: FileService.Instance)
        {
            Enterprise = Program.Config.Enterprise,
            LibraryFileName = Info.LibraryFile.Name,
            IsRemote = Info.IsRemote
        };

        // set the method to replace variables
        // this way any path can have variables and will just automatically get replaced
        FileService.Instance.ReplaceVariables = nodeParameters.ReplaceVariables;
        FileService.Instance.Logger = logger;
        
        nodeParameters.HasPluginActual = (name) =>
        {
            var normalizedSearchName = Regex.Replace(name.ToLower(), "[^a-z]", string.Empty);
            return Program.Config.PluginNames?.Any(x =>
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
            SharedDirectory = Path.Combine(Program.ConfigDirectory, "Scripts", "Shared"),
            FileFlowsUrl = Service.ServiceBaseUrl,
            PluginMethodInvoker = (plugin, method, methodArgs) 
                => Helpers.PluginHelper.PluginMethodInvoker(nodeParameters, plugin, method, methodArgs)
        };
        foreach (var variable in Program.Config.Variables)
        {
            object value = variable.Value;
            if (value == null)
                continue;
            if (variable.Value?.Trim()?.ToLowerInvariant() == "true")
                value = true;
            else if (variable.Value?.Trim()?.ToLowerInvariant() == "false")
                value = false;
            else if (Regex.IsMatch(variable.Value?.Trim(), @"^[\d](\.[\d]+)?$"))
                value = variable.Value.IndexOf(".", StringComparison.Ordinal) > 0 ? float.Parse(variable.Value) : int.Parse(variable.Value);
            
            nodeParameters.Variables.TryAdd(variable.Key, value);
        }
        
        Plugin.Helpers.FileHelper.DontChangeOwner = Node.DontChangeOwner;
        Plugin.Helpers.FileHelper.DontSetPermissions = Node.DontSetPermissions;
        Plugin.Helpers.FileHelper.Permissions = Node.Permissions;

        nodeParameters.RunnerUid = Info.Uid;
        nodeParameters.TempPath = WorkingDir;
        nodeParameters.TempPathName = new DirectoryInfo(WorkingDir).Name;
        nodeParameters.RelativeFile = Info.LibraryFile.RelativePath;
        nodeParameters.PartPercentageUpdate = UpdatePartPercentage;
        Shared.Helpers.HttpHelper.Logger = nodeParameters.Logger;

        nodeParameters.Result = NodeResult.Success;
        nodeParameters.GetToolPathActual = (name) =>
        {
            var variable = Program.Config.Variables.Where(x => x.Key.ToLowerInvariant() == name.ToLowerInvariant())
                .Select(x => x.Value).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(variable))
                return variable;
            return Node.Map(variable);
        };
        nodeParameters.GetPluginSettingsJson = (pluginSettingsType) =>
        {
            string? json = null;
            Program.Config.PluginSettings?.TryGetValue(pluginSettingsType, out json);
            return json;
        };
        var statService = StatisticService.Load();
        nodeParameters.StatisticRecorder = (name, value) =>
            statService.Record(name, value);
        nodeParameters.AdditionalInfoRecorder = RecordAdditionalInfo;

        var flow = FlowHelper.GetStartupFlow(Info.IsRemote, Flow);

        var flowExecutor = new ExecuteFlow()
        {
            Flow = flow,
            Runner = this
        };

        int result = flowExecutor.Execute(nodeParameters);
        
        if(result == RunnerCodes.Completed)
            SetStatus(FileStatus.Processed);
        else if(result is RunnerCodes.Failure or RunnerCodes.TerminalExit)
            SetStatus(FileStatus.ProcessingFailed);
        else if(result == RunnerCodes.RunCanceled)
            SetStatus(FileStatus.ProcessingFailed);
        else if(result == RunnerCodes.MappingIssue)
            SetStatus(FileStatus.MappingIssue);
        else
        {
            nodeParameters.Logger.WLog("Safety caught flow execution unexpected result code: " + result);
            SetStatus(FileStatus.ProcessingFailed); // safety catch, shouldn't happen
        }
    }
    
    private void RecordAdditionalInfo(string name, object value, TimeSpan? expiry)
    {
        if (value == null)
        {
            if (Info.AdditionalInfos.ContainsKey(name) == false)
                return; // nothing to do

            Info.AdditionalInfos.Remove(name);
        }
        else
        {
            if (value is TimeSpan ts)
                value = Plugin.Helpers.TimeHelper.ToHumanReadableString(ts);
            
            Info.AdditionalInfos[name] = new()
            {
                Value = value,
                Expiry = expiry ?? new TimeSpan(0, 1, 0)
            };
        }
        _ = SendUpdate(Info);
    }
}
