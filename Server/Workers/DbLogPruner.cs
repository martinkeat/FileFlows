using FileFlows.Server.Controllers;
using FileFlows.Server.Helpers;
using FileFlows.ServerShared.Workers;

namespace FileFlows.Server.Workers;

/// <summary>
/// Worker that prunes logs from an external database
/// </summary>
public class DbLogPruner:Worker
{
    /// <summary>
    /// Constructor for the log pruner
    /// </summary>
    public DbLogPruner() : base(ScheduleType.Daily, 5)
    {
        Execute();
    }

    /// <summary>
    /// Executes the log pruner, Run calls this 
    /// </summary>
    protected override void Execute()
    {
        // REFACTOR: re-look into this
        // if (DbHelper.UseMemoryCache)
        //     return; // nothing to do
        // var settings = new SettingsController().Get().Result;
        // if (settings == null)
        //     return;
        // DbHelper.PruneOldLogs(Math.Max(1000, settings.LogDatabaseRetention)).Wait();
    }
}