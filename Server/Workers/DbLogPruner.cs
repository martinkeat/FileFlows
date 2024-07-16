// using FileFlows.Managers;
// using FileFlows.Server.Services;
// using FileFlows.ServerShared.Workers;
// using FileFlows.Shared.Models;
//
// namespace FileFlows.Server.Workers;
//
// /// <summary>
// /// Worker that prunes logs from an external database
// /// </summary>
// public class DbLogPruner:ServerWorker
// {
//     /// <summary>
//     /// Constructor for the log pruner
//     /// </summary>
//     public DbLogPruner() : base(ScheduleType.Daily, 5)
//     {
//         Execute();
//     }
//
//     /// <summary>
//     /// Executes the log pruner, Run calls this 
//     /// </summary>
//     protected override void ExecuteActual(Settings settings)
//     {
//         var retention = settings.LogDatabaseRetention;
//         new DatabaseLogManager().PruneOldLogs(Math.Max(1000, retention)).Wait();
//     }
// }