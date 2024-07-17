using FileFlows.Server.Controllers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// Status service
/// </summary>
public class StatusService
{
    /// <summary>
    /// Gets the current system status
    /// </summary>
    /// <returns>the status</returns>
    public async Task<StatusModel> Get()
    {
        {
            var status = new StatusModel();
            var lfOverview = await new Server.Services.LibraryFileService().GetStatus();
            status.queue = lfOverview.FirstOrDefault(x => x.Status == FileStatus.Unprocessed)?.Count ?? 0;
            status.processed = lfOverview.FirstOrDefault(x => x.Status == FileStatus.Processed)?.Count ?? 0;

            var workerController = new WorkerController(null);
            var executors = (await workerController.GetAll())?.ToList() ?? new List<FlowExecutorInfo>();
            status.processing = executors.Count;

            if (executors.Any())
            {
                foreach (var exec in executors)
                {
                    status.processingFiles.Add(new()
                    {
                        name = exec.LibraryFile.Name,
                        step = exec.CurrentPartName,
                        library = exec.LibraryFile.LibraryName,
                        relativePath = exec.LibraryFile.RelativePath,
                        stepPercent = exec.CurrentPartPercent,
                    });
                }

                var time = executors.OrderByDescending(x => x.ProcessingTime).First().ProcessingTime;
                if (time.Hours > 0)
                    status.time = time.ToString(@"h\:mm\:ss");
                else
                    status.time = time.ToString(@"m\:ss");
            }
            else
            {
                status.time = string.Empty;
            }

            return status;
        }
    }
    
    
    /// <summary>
    /// Gets if an update is available
    /// </summary>
    /// <returns>True if there is an update</returns>
    public async Task<object> UpdateAvailable()
    {
        var settings = await ServiceLoader.Load<ISettingsService>().Get();
        if (settings?.DisableTelemetry != false)
            return new { UpdateAvailable = false };
        var result = Workers.ServerUpdater.Instance.GetLatestOnlineVersion();
        return new { UpdateAvailable = result.updateAvailable };
    }
}

/// <summary>
/// The current status
/// </summary>
public class StatusModel 
{
    /// <summary>
    /// Gets the number of items in the queue
    /// </summary>
    public int queue { get; set; }
    /// <summary>
    /// Gets the number of files being processed
    /// </summary>
    public int processing { get; set; }
    /// <summary>
    /// Gets the number of files that have been processed
    /// </summary>
    public int processed { get; set; }
    /// <summary>
    /// Gets the processing time of the longest running item in the queue
    /// </summary>
    public string time { get; set; }

    /// <summary>
    /// Files currently processing
    /// </summary>
    public List<ProcessingFile> processingFiles { get; set; } = new List<ProcessingFile>();
}

/// <summary>
/// A processing file
/// </summary>
public class ProcessingFile
{
    /// <summary>
    /// the filename
    /// </summary>
    public string name { get; set; }

    /// <summary>
    /// the relative path to the library path
    /// </summary>
    public string relativePath { get; set; }
        
    /// <summary>
    /// the library name
    /// </summary>
    public string library { get; set; }
        
    /// <summary>
    /// the step currently processing
    /// </summary>
    public string step { get; set; }
    /// <summary>
    /// the percent the current step is at
    /// </summary>
    public float stepPercent { get; set; }
}