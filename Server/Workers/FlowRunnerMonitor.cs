using FileFlows.Server.Controllers;
using FileFlows.Server.Services;
using FileFlows.ServerShared.Workers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Workers;

/// <summary>
/// A worker that monitors FlowRunners and will cancel
/// any "dead" runners
/// </summary>
public class FlowRunnerMonitor:Worker
{
    private WorkerController Controller;

    private List<Guid> StartUpRunningFiles;
    private DateTime StartedAt = DateTime.Now;

    /// <summary>
    /// Constructs a Flow Runner Monitor worker
    /// </summary>
    public FlowRunnerMonitor() : base(ScheduleType.Second, 10)
    {
        Controller = new(null);
        StartUpRunningFiles = WorkerController.ExecutingLibraryFiles().ToList();
    }

    protected override void Execute()
    {
        Controller.AbortDisconnectedRunners();
        if (StartUpRunningFiles?.Any() == true)
        {
            var array = StartUpRunningFiles.ToArray();
            var service = new LibraryFileService();
            foreach (var lf in array)
            {
                if (Controller.IsLibraryFileRunning(lf))
                    StartUpRunningFiles.Remove(lf);
                else if(DateTime.Now > StartedAt.AddMinutes(2))
                {
                    // no update in 2minutes, kill it
                    try
                    {
                        var status = service.GetFileStatus(lf);
                        if (status != FileStatus.Processing)
                        {
                            StartUpRunningFiles.Remove(lf);
                            continue;
                        }
                    }
                    catch (Exception)
                    {
                        // not known, silently ignore this file then
                        StartUpRunningFiles.Remove(lf);
                        continue;
                    }
                    
                    // abort the file
                    service.Abort(lf).Wait();
                    StartUpRunningFiles.Remove(lf);
                }
            }
        }
    }
}