using FileFlows.Managers;
using FileFlows.Server.Controllers;
using FileFlows.Server.Helpers;
using FileFlows.Server.Services;
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
        var retention = ServiceLoader.Load<SettingsService>().Get().Result?.LogDatabaseRetention ?? 1000;
        new DatabaseLogManager().PruneOldLogs(Math.Max(1000, retention)).Wait();
    }
}