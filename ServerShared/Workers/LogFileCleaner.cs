using FileFlows.ServerShared.Helpers;
using FileFlows.ServerShared.Services;

namespace FileFlows.ServerShared.Workers;

/// <summary>
/// Worker to clean up old log files
/// </summary>
public class LogFileCleaner:Worker
{
    /// <summary>
    /// Constructs a log file cleaner
    /// </summary>
    public LogFileCleaner() : base(ScheduleType.Daily, 5)
    {
        Trigger();
    }

    /// <summary>
    /// Executes the cleaner
    /// </summary>
    protected sealed override void Execute()
    {
        var settings = ServiceLoader.Load<ISettingsService>().Get().Result;
        if (settings == null)
            return; // not yet ready
        var dir = DirectoryHelper.LoggingDirectory;
        var minDate = DateTime.Now.AddDays(-(settings.LogFileRetention < 1 ? 5 : settings.LogFileRetention));
        foreach (var file in new DirectoryInfo(dir).GetFiles("*.log")
                     .OrderByDescending(x => x.LastWriteTime))
        {
            if (file.LastWriteTime > minDate)
                continue;
            
            try
            {
                file.Delete();
                Logger.Instance.ILog("Deleted log file: " + file.Name);
            }
            catch (Exception)
            {
                // ignored
            }
            
        }
    }
}