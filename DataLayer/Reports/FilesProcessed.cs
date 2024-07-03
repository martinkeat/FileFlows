using FileFlows.DataLayer.Reports.Helpers;
using FileFlows.Plugin;
using FileFlows.Shared.Formatters;
using FileFlows.Shared.Models;

namespace FileFlows.DataLayer.Reports;

/// <summary>
/// Report for Files Processed
/// </summary>
public class FilesProcessed : Report
{
    /// <inheritdoc />
    public override Guid Uid => new Guid("7da09d69-5e19-49c1-8054-4205fb0376d4");
    /// <inheritdoc />
    public override string Name => "Files Processed";
    /// <inheritdoc />
    public override string Description => "Reports on the files processed over a given period.";
    /// <inheritdoc />
    public override string Icon => "fas fa-file-powerpoint";

    /// <inheritdoc />
    public override ReportPeriod? DefaultReportPeriod => ReportPeriod.Last31Days;

    /// <summary>
    /// Gets or sets the statistic to report on
    /// </summary>
    public ProcessedStatistic Statistic { get; set; }
    
    /// <inheritdoc />
    public override ReportSelection LibrarySelection => ReportSelection.Any;
    
    /// <inheritdoc />
    public override ReportSelection NodeSelection => ReportSelection.AnyOrAll;


    /// <inheritdoc />
    public override async Task<Result<string>> Generate(Dictionary<string, object> model, bool emailing)
    {
        var statistic = GetEnumValue<ProcessedStatistic>(model, nameof(Statistic));

        using var db = await GetDb();
        string sql =
            $"select {Wrap("NodeUid")}, {Wrap("NodeName")}, {Wrap("OriginalSize")}, " +
            $"{Wrap("FinalSize")}, {Wrap("ProcessingStarted")}, {Wrap("ProcessingEnded")} " +
            $"from {Wrap("LibraryFile")} where {Wrap("Status")} = 1";

        AddPeriodToSql(model, ref sql);
        AddLibrariesToSql(model, ref sql);
        AddNodesToSql(model, ref sql);

        var nodeUids = GetUids("Node", model).Where(x => x != null).ToList();
        
        var files = await db.Db.FetchAsync<NodeData>(sql);
        
        (DateTime? minDateUtc, DateTime? maxDateUtc) = GetPeriod(model);
        minDateUtc ??= files.Min(x => x.ProcessingStarted);
        maxDateUtc ??= files.Max(x => x.ProcessingStarted);
        
        Func<double, string>? formatter = statistic switch
        {
            ProcessedStatistic.Size => FileSizeFormatter.Format,
            ProcessedStatistic.Duration => TimeFormatter.Format,
            _ => null
        };
        var yAxisFormatter = statistic switch
        {
            ProcessedStatistic.Size => "filesize",
            _ => null
        };

        Dictionary<string, Dictionary<DateTime, long>> data = new();
        foreach (var file in files)
        {
            var date = file.ProcessingStarted.ToLocalTime();
            date = new DateTime(date.Year, date.Month, date.Day);
            string name;
            if (nodeUids.Count > 0)
                name = file.NodeName == "FileFlowsServer" ? "Internal Processing Node" : file.NodeName;
            else
                name = string.Empty;

            data.TryAdd(name, new Dictionary<DateTime, long>());
            var d = data[name];
            d.TryAdd(date, 0);
            
            switch (statistic)
            {
                case ProcessedStatistic.Count:
                    d[date] += 1;
                    break;
                case ProcessedStatistic.Size:
                    d[date] += file.OriginalSize;
                    break;
                case ProcessedStatistic.Duration:
                    d[date] += (int)(file.ProcessingEnded - file.ProcessingStarted).TotalSeconds;
                    break;
            }
        }

        if (data.Count == 0)
            return string.Empty;
        
        string html = DateBasedChartHelper.Generate(minDateUtc.Value, maxDateUtc.Value, data, emailing,
            tableDataFormatter: formatter, yAxisFormatter: yAxisFormatter);

        return html;
    }
    
    /// <summary>
    /// Represents the data for a node in the processing system.
    /// </summary>
    /// <param name="NodeUid">The unique identifier of the node.</param>
    /// <param name="NodeName">The name of the node.</param>
    /// <param name="OriginalSize">The original size of the data before processing.</param>
    /// <param name="FinalSize">The final size of the data after processing.</param>
    /// <param name="ProcessingStarted">The date and time when processing started.</param>
    /// <param name="ProcessingEnded">The date and time when processing ended.</param>
    public record NodeData(Guid NodeUid, string NodeName, long OriginalSize, long FinalSize, DateTime ProcessingStarted, DateTime ProcessingEnded);
}
