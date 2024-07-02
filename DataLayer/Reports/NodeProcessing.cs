// using System.Security.Cryptography;
// using FileFlows.DataLayer.Reports.Helpers;
// using FileFlows.Plugin;
// using FileFlows.Plugin.Formatters;
//
// namespace FileFlows.DataLayer.Reports;
//
// /// <summary>
// /// Detailed summary of processing performed on each processing node.
// /// </summary>
// public class NodeProcessing : Report
// {
//     /// <inheritdoc />
//     public override Guid Uid => new("2c4f5e2e-de4c-41fc-9a49-672e70b9b07b");
//
//     /// <inheritdoc />
//     public override string Name => "Node Processing";
//
//     /// <inheritdoc />
//     public override string Description => "Detailed summary of processing performed on each processing node.";
//
//     /// <inheritdoc />
//     public override string Icon => "fas fa-desktop";
//
//     /// <summary>
//     /// Gets or sets the statistic to report on
//     /// </summary>
//     public ProcessedStatistic Statistic { get; set; }
//
//     /// <inheritdoc />
//     public override async Task<Result<string>> Generate(Dictionary<string, object> model)
//     {
//         var statistic = GetEnumValue<ProcessedStatistic>(model, nameof(Statistic));
//
//         using var db = await GetDb();
//         string sql =
//             $"select {Wrap("NodeUid")}, {Wrap("NodeName")}, {Wrap("OriginalSize")}, " +
//             $"{Wrap("FinalSize")}, {Wrap("ProcessingStarted")}, {Wrap("ProcessingEnded")} " +
//             $"from {Wrap("LibraryFile")} where {Wrap("Status")} = 1";
//
//         AddPeriodToSql(model, ref sql);
//         
//         var files = await db.Db.FetchAsync<NodeData>(sql);
//         
//         (DateTime? minDateUtc, DateTime? maxDateUtc) = GetPeriod(model);
//         minDateUtc ??= files.Min(x => x.ProcessingStarted);
//         maxDateUtc ??= files.Max(x => x.ProcessingStarted);
//
//         Dictionary<string, Dictionary<DateTime, long>> data = new();
//         foreach (var file in files)
//         {
//             var date = file.ProcessingStarted.ToLocalTime();
//             date = new DateTime(date.Year, date.Month, date.Day);
//             string name = file.NodeName == "FileFlowsServer" ? "Internal Processing Node" : file.NodeName;
//
//             data.TryAdd(name, new Dictionary<DateTime, long>());
//             var d = data[name];
//             d.TryAdd(date, 0);
//             
//             switch (statistic)
//             {
//                 case ProcessedStatistic.Count:
//                     d[date] += 1;
//                     break;
//                 case ProcessedStatistic.Size:
//                     d[date] += file.OriginalSize;
//                     break;
//                 case ProcessedStatistic.Duration:
//                     d[date] += (int)(file.ProcessingEnded - file.ProcessingStarted).TotalSeconds;
//                     break;
//             }
//         }
//         
//         string html = DateBasedChartHelper.Generate(minDateUtc.Value, maxDateUtc.Value, data);
//
//         return html;
//     }
//     
//     /// <summary>
//     /// Represents the data for a node in the processing system.
//     /// </summary>
//     /// <param name="NodeUid">The unique identifier of the node.</param>
//     /// <param name="NodeName">The name of the node.</param>
//     /// <param name="OriginalSize">The original size of the data before processing.</param>
//     /// <param name="FinalSize">The final size of the data after processing.</param>
//     /// <param name="ProcessingStarted">The date and time when processing started.</param>
//     /// <param name="ProcessingEnded">The date and time when processing ended.</param>
//     public record NodeData(Guid NodeUid, string NodeName, long OriginalSize, long FinalSize, DateTime ProcessingStarted, DateTime ProcessingEnded);
// }