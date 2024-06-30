using System.Text.Json;
using Esprima.Ast;
using FileFlows.Plugin;
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
    public override bool PeriodSelection => true;
    
    /// <summary>
    /// Gets or sets the statistic to report on
    /// </summary>
    public ProcessedStatistic Statistic { get; set; }
    
    /// <inheritdoc />
    public override ReportSelection LibrarySelection => ReportSelection.Any;

    /// <inheritdoc />
    public override async Task<Result<string>> Generate(Dictionary<string, object> model)
    {
        var statistic = GetEnumValue<ProcessedStatistic>(model, nameof(Statistic)); 

        using var db = await GetDb();

        string sql =
            $"select {Wrap("ProcessingStarted")}, {Wrap("ProcessingEnded")}, {Wrap("OriginalSize")} " +
            $"from {Wrap("LibraryFile")} where {Wrap("Status")} = 1 ";
        AddLibrariesToSql(model, ref sql);
        AddPeriodToSql(model, ref sql);
        sql += $" order by {Wrap("ProcessingStarted")}";

        var files = await db.Db.FetchAsync<FilesProcessedData>(sql);
        
        (DateTime? minDateUtc, DateTime? maxDateUtc) = GetPeriod(model);
        minDateUtc ??= files.Min(x => x.ProcessingStarted);
        maxDateUtc ??= files.Max(x => x.ProcessingStarted);

        SortedDictionary<DateTime, double> data = new();
        foreach (var file in files)
        {
            var date = file.ProcessingStarted.ToLocalTime();
            date = new DateTime(date.Year, date.Month, date.Day);
            data.TryAdd(date, 0);
            
            switch (statistic)
            {
                case ProcessedStatistic.Count:
                    data[date] += 1;
                    break;
                case ProcessedStatistic.Size:
                    data[date] += file.OriginalSize;
                    break;
                case ProcessedStatistic.Duration:
                    data[date] += (int)(file.ProcessingEnded - file.ProcessingStarted).TotalSeconds;
                    break;
            }
        }
        DateTime current = minDateUtc.Value;
        while (current < maxDateUtc.Value)
        {
            var date = current.ToLocalTime();
            date = new DateTime(date.Year, date.Month, date.Day);
            data.TryAdd(date, 0);
            current = current.AddDays(1);
        }
        
        var table = GenerateHtmlTable(data.Select(x => new { Date = x.Key.ToString("d MMMM"), x.Value})) ?? string.Empty;

        var chart = GenerateSvgBarChart(data.ToDictionary(x => (object)x.Key, x=> x.Value)) ?? string.Empty;

        return table + chart;
    }

    public enum ProcessedStatistic
    {
        Count,
        Size,
        Duration
    }

    public class FilesProcessedData
    {
        public DateTime ProcessingStarted { get; set; }
        public DateTime ProcessingEnded { get; set; }
        public long OriginalSize { get; set; }
    }
}
