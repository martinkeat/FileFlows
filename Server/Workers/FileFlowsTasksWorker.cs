using System.Text;
using FileFlows.Plugin;
using FileFlows.ScriptExecution;
using FileFlows.Server.Controllers;
using FileFlows.Server.Helpers;
using FileFlows.Server.Services;
using FileFlows.ServerShared.Models;
using FileFlows.ServerShared.Workers;
using FileFlows.Shared.Models;
using Logger = FileFlows.Shared.Logger;

namespace FileFlows.Server.Workers;

/// <summary>
/// A worker that runs FileFlows Tasks
/// </summary>
public class FileFlowsTasksWorker: ServerWorker
{
    /// <summary>
    /// Gets the instance of the tasks worker
    /// </summary>
    internal static FileFlowsTasksWorker Instance { get;private set; } = null!;

    /// <summary>
    /// A list of tasks and the quarter they last ran in
    /// </summary>
    private Dictionary<Guid, int> TaskLastRun = new ();
    
    
    /// <summary>
    /// Creates a new instance of the Scheduled Task Worker
    /// </summary>
    public FileFlowsTasksWorker() : base(ScheduleType.Minute, 1)
    {
        Instance = this;
        
        SystemEvents.OnLibraryFileAdd += SystemEventsOnOnLibraryFileAdd;
        SystemEvents.OnLibraryFileProcessed += SystemEventsOnOnLibraryFileProcessed;
        SystemEvents.OnLibraryFileProcessedFailed += SystemEventsOnOnLibraryFileProcessedFailed;
        SystemEvents.OnLibraryFileProcessedSuceess += SystemEventsOnOnLibraryFileProcessedSuceess;
        SystemEvents.OnLibraryFileProcessingStarted += SystemEventsOnOnLibraryFileProcessingStarted;
        SystemEvents.OnServerUpdating += SystemEventsOnOnServerUpdating;
        SystemEvents.OnServerUpdateAvailable += SystemEventsOnOnServerUpdateAvailable;
    }
    
    /// <summary>
    /// Gets the variables in a dictionary
    /// </summary>
    /// <returns>a dictionary of variables</returns>
    public static Dictionary<string, object> GetVariables()
    {
        var list = ServiceLoader.Load<VariableService>().GetAllAsync().Result ?? new ();
        var dict = new Dictionary<string, object>();
        foreach (var var in list)
        {
            dict.Add(var.Name, var.Value);
        }
        
        dict.TryAdd("FileFlows.Url", Application.ServerUrl);
        dict["FileFlows.AccessToken"] = ServiceLoader.Load<SettingsService>().Get()?.Result?.AccessToken;
        return dict;
    }

    /// <summary>
    /// Executes any tasks
    /// </summary>
    protected override void ExecuteActual(Settings settings)
    {
        if (LicenseHelper.IsLicensed(LicenseFlags.Tasks) == false)
            return;
        
        int quarter = TimeHelper.GetCurrentQuarter();
        var tasks = ServiceLoader.Load<TaskService>().GetAllAsync().Result;
        // 0, 1, 2, 3, 4
        foreach (var task in tasks)
        {
            if (task.Type != TaskType.Schedule)
                continue;
            if (task.Schedule[quarter] != '1')
                continue;
            if (TaskLastRun.ContainsKey(task.Uid) && TaskLastRun[task.Uid] == quarter)
                continue;
            _ = RunTask(task);
            TaskLastRun[task.Uid] = quarter;
        }
    }

    /// <summary>
    /// Runs a task by its UID
    /// </summary>
    /// <param name="uid">The UID of the task to run</param>
    /// <returns>the result of the executed task</returns>
    internal async Task<FileFlowsTaskRun> RunByUid(Guid uid)
    {
        if (LicenseHelper.IsLicensed(LicenseFlags.Tasks) == false) 
            return new() { Success = false, Log = "Not licensed" };
        var task = await ServiceLoader.Load<TaskService>().GetByUidAsync(uid);
        if (task == null)
            return new() { Success = false, Log = "Task not found" };
        return await RunTask(task);
    } 

    /// <summary>
    /// Runs a task
    /// </summary>
    /// <param name="task">the task to run</param>
    /// <param name="additionalVariables">any additional variables</param>
    private async Task<FileFlowsTaskRun> RunTask(FileFlowsTask task, Dictionary<string, object>? additionalVariables = null)
    {
        var code = (await ServiceLoader.Load<ScriptService>().Get(task.Script))?.Code;
        if (string.IsNullOrWhiteSpace(code))
        {
            var msg = $"No code found for Task '{task.Name}' using script: {task.Script}";
            Logger.Instance.WLog(msg);
            return new() { Success = false, Log = msg };
        }
        Logger.Instance.ILog("Executing task: " + task.Name);
        DateTime dtStart = DateTime.UtcNow;

        var variables = GetVariables();
        if (additionalVariables?.Any() == true)
        {
            foreach (var variable in additionalVariables)
            {
                variables[variable.Key] = variable.Value;
            }
        }

        var scriptService = ServiceLoader.Load<ScriptService>();
        string sharedDirectory = await scriptService.GetSharedDirectory();

        var result = ScriptExecutor.Execute(code, variables, sharedDirectory: sharedDirectory, dontLogCode: true);
        if (result.Success)
        {
            Logger.Instance.ILog($"Task '{task.Name}' completed in: " + (DateTime.UtcNow.Subtract(dtStart)) + "\n" +
                                 result.Log);

            _ = ServiceLoader.Load<NotificationService>().Record(NotificationSeverity.Information,
                $"Task executed successfully '{task.Name}'");
        }
        else
        {
            Logger.Instance.ELog($"Error executing task '{task.Name}: " + result.ReturnValue + "\n" + result.Log);
            
            _ = ServiceLoader.Load<NotificationService>().Record(NotificationSeverity.Warning,
                $"Error executing task '{task.Name}': " + result.ReturnValue, result.Log);
        }

        task.LastRun = DateTime.UtcNow;
        task.RunHistory ??= new Queue<FileFlowsTaskRun>(10);
        lock (task.RunHistory)
        {
            task.RunHistory.Enqueue(result);
            while (task.RunHistory.Count > 10 && task.RunHistory.TryDequeue(out _));
        }

        await ServiceLoader.Load<TaskService>().Update(task, auditDetails: AuditDetails.ForServer());
        return result;
    }
    
    private void TriggerTaskType(TaskType type, Dictionary<string, object> variables)
    {
        if (LicenseHelper.IsLicensed(LicenseFlags.Tasks) == false)
            return;
        var tasks = ServiceLoader.Load<TaskService>().GetAllAsync().Result.Where(x => x.Type == type).ToArray();
        foreach (var task in tasks)
        {
            _ = RunTask(task, variables);
        }
    }

    private void UpdateEventTriggered(TaskType type, SystemEvents.UpdateEventArgs args)
    {
        if (LicenseHelper.IsLicensed(LicenseFlags.Tasks) == false)
            return;
        TriggerTaskType(type, new Dictionary<string, object>
        {
            { nameof(args.Version), args.Version },
            { nameof(args.CurrentVersion), args.CurrentVersion },
        });
    }

    private void SystemEventsOnOnServerUpdateAvailable(SystemEvents.UpdateEventArgs args)
        => UpdateEventTriggered(TaskType.FileFlowsServerUpdateAvailable, args);
    private void SystemEventsOnOnServerUpdating(SystemEvents.UpdateEventArgs args)
        => UpdateEventTriggered(TaskType.FileFlowsServerUpdating, args);

    private void LibraryFileEventTriggered(TaskType type, SystemEvents.LibraryFileEventArgs args)
    {
        if (LicenseHelper.IsLicensed(LicenseFlags.Tasks) == false)
            return;
        TriggerTaskType(type, new Dictionary<string, object>
        {
            { "FileName", args.File.Name },
            { "LibraryFile", args.File },
            { "Library", args.Library }
        });
    }

    private void SystemEventsOnOnLibraryFileAdd(SystemEvents.LibraryFileEventArgs args) =>
        LibraryFileEventTriggered(TaskType.FileAdded, args);
    private void SystemEventsOnOnLibraryFileProcessingStarted(SystemEvents.LibraryFileEventArgs args)
        => LibraryFileEventTriggered(TaskType.FileProcessing, args);
    private void SystemEventsOnOnLibraryFileProcessed(SystemEvents.LibraryFileEventArgs args)
        => LibraryFileEventTriggered(TaskType.FileProcessed, args);
    private void SystemEventsOnOnLibraryFileProcessedSuceess(SystemEvents.LibraryFileEventArgs args)
        => LibraryFileEventTriggered(TaskType.FileProcessSuccess, args);
    private void SystemEventsOnOnLibraryFileProcessedFailed(SystemEvents.LibraryFileEventArgs args)
        => LibraryFileEventTriggered(TaskType.FileProcessFailed, args);

}