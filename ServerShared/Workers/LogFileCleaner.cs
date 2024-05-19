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
        // var settings = ServiceLoader.Load<SettingsService>().Get().Result;
        // if (settings == null || settings.LogFileRetention < 1)
        //     return; // not yet ready
        // var dir = DirectoryHelper.LoggingDirectory;
        // int count = 0;
        // foreach (var file in new DirectoryInfo(dir).GetFiles("FileFlows*")
        //              .OrderByDescending(x => x.LastWriteTime))
        // {
        //     if (string.IsNullOrEmpty(file.Extension) == false && file.Extension != ".log")
        //         continue;
        //     
        //     if (++count > settings.LogFileRetention)
        //     {
        //         try
        //         {
        //             file.Delete();
        //             Logger.Instance.ILog("Deleted log file: " + file.Name);
        //         }
        //         catch (Exception)
        //         {
        //             // ignored
        //         }
        //     }
        // }
    }
}