using System.Numerics;
using FileFlows.DataLayer.Reports.Charts;
using FileFlows.DataLayer.Reports.Helpers;
using FileFlows.Plugin;
using FileFlows.Shared.Formatters;
using FileFlows.Shared.Models;

namespace FileFlows.DataLayer.Reports;

/// <summary>
/// Processing Summary Report
/// </summary>
public class ProcessingSummary: Report
{
    /// <inheritdoc />
    public override Guid Uid => new Guid("c0ca274b-b651-489e-a709-bee0e7d3870f");
    /// <inheritdoc />
    public override string Name => "Processing Summary";
    /// <inheritdoc />
    public override string Description => "Detailed summary of files processed by library, node, time of day";
    /// <inheritdoc />
    public override string Icon => "fas fa-chart-area";
    /// <inheritdoc />
    public override ReportSelection LibrarySelection => ReportSelection.AnyOrAll;
    /// <inheritdoc />
    public override ReportSelection NodeSelection => ReportSelection.AnyOrAll;
    /// <inheritdoc />
    public override ReportSelection FlowSelection  => ReportSelection.Any;

    /// <inheritdoc />
    public override async Task<Result<string>> Generate(Dictionary<string, object> model, bool emailing)
    {
        //var statistic = GetEnumValue<ProcessedStatistic>(model, nameof(Statistic));

        using var db = await GetDb();
        string sql =
            $"select {Wrap("NodeUid")}, {Wrap("NodeName")}, {Wrap("OriginalSize")}, " +
            $"{Wrap("FinalSize")}, {Wrap("ProcessingStarted")}, {Wrap("ProcessingEnded")}, " +
            $"{Wrap("LibraryUid")}, {Wrap("LibraryName")}, {Wrap("FlowUid")}, {Wrap("FlowName")} " +
            $"from {Wrap("LibraryFile")} where {Wrap("Status")} = 1";

        AddPeriodToSql(model, ref sql);
        AddLibrariesToSql(model, ref sql);
        AddFlowsToSql(model, ref sql);
        AddNodesToSql(model, ref sql);

        var nodeUids = GetUids("Node", model).Where(x => x != null).ToList();
        var libraryUids = GetUids("Library", model).Where(x => x != null).ToList();
        var flowUids = GetUids("Flow", model).Where(x => x != null).ToList();
        
        var files = await db.Db.FetchAsync<FileData>(sql);
        if (files.Count < 1)
            return string.Empty; // no data
        
        (DateTime? minDateUtc, DateTime? maxDateUtc) = GetPeriod(model);
        minDateUtc ??= files.Min(x => x.ProcessingStarted);
        maxDateUtc ??= files.Max(x => x.ProcessingStarted);
        
        (bool hourly, var labels) = DateTimeLabelHelper.GenerateDates(minDateUtc.Value, maxDateUtc.Value);
        
        Dictionary<string, Dictionary<DateTime, long>> nodeDataCount = new();
        Dictionary<string, Dictionary<DateTime, long>> nodeDataSize = new();
        Dictionary<string, Dictionary<DateTime, long>> nodeDataTime = new();
        Dictionary<string, Dictionary<DateTime, long>> libDataCount = new();
        Dictionary<string, Dictionary<DateTime, long>> libDataSize = new();
        Dictionary<string, Dictionary<DateTime, long>> libDataTime = new();
        foreach (var file in files)
        {
            var date = file.ProcessingStarted.ToLocalTime();
            date = hourly ?  new DateTime(date.Year, date.Month, date.Day, date.Hour, 0, 0) :  new DateTime(date.Year, date.Month, date.Day);
            
            string nodeName;
            if (nodeUids.Count > 0)
                nodeName = file.NodeName == "FileFlowsServer" ? "Internal Processing Node" : file.NodeName;
            else
                nodeName = string.Empty;
            

            nodeDataCount.TryAdd(nodeName, new Dictionary<DateTime, long>());
            var ndCount = nodeDataCount[nodeName];
            ndCount.TryAdd(date, 0);
            ndCount[date] += 1;
            
            nodeDataSize.TryAdd(nodeName, new Dictionary<DateTime, long>());
            var ndSize = nodeDataSize[nodeName];
            ndSize.TryAdd(date, 0);
            ndSize[date] += file.OriginalSize;
            
            nodeDataTime.TryAdd(nodeName, new Dictionary<DateTime, long>());
            var ndTime = nodeDataTime[nodeName];
            ndTime.TryAdd(date, 0);
            ndTime[date] += (int)(file.ProcessingEnded - file.ProcessingStarted).TotalSeconds;
            
            

            string libraryName = libraryUids.Count > 0 ? file.LibraryName : string.Empty;
            libDataCount.TryAdd(libraryName, new Dictionary<DateTime, long>());
            var ldCount = libDataCount[libraryName];
            ldCount.TryAdd(date, 0);
            ldCount[date] += 1;
            
            libDataSize.TryAdd(libraryName, new Dictionary<DateTime, long>());
            var ldSize = libDataSize[libraryName];
            ldSize.TryAdd(date, 0);
            ldSize[date] += file.OriginalSize;
            
            libDataTime.TryAdd(libraryName, new Dictionary<DateTime, long>());
            var ldTime = libDataTime[libraryName];
            ldTime.TryAdd(date, 0);
            ldTime[date] += (int)(file.ProcessingEnded - file.ProcessingStarted).TotalSeconds;
        }

        string output = "";
        foreach (var chart in new []
                 {
                     ("Node Files", nodeDataCount, ""), 
                     ("Node Size", nodeDataSize, "filesize"), 
                     ("Node Time", nodeDataTime, ""),
                     
                     ("Library Files", libDataCount, ""), 
                     ("Library Size", libDataSize, "filesize"), 
                     ("Library Time", libDataTime, ""),
                 })
        {
            var chartHtml = MultiLineChart.Generate(new MultilineChartData
            {
                Labels = labels,
                YAxisFormatter = chart.Item3,
                Series = chart.Item2.Select(seriesItem => new ChartSeries
                {
                    Name = seriesItem.Key,
                    Data = labels.Select(label => (double)seriesItem.Value.GetValueOrDefault(label, 0)).ToArray()
                }).ToArray()
            }, generateSvg: emailing);

            output += $"<h2>{chart.Item1}</h2>" + chartHtml;
        }

        return output;
    }
    
    /// <summary>
    /// Represents the data for a node in the processing system.
    /// </summary>
    /// <param name="NodeUid">The unique identifier of the node.</param>
    /// <param name="NodeName">The name of the node.</param>
    /// <param name="OriginalSize">The original size of the data before processing.</param>
    /// <param name="FinalSize">The final size of the data after processing.</param>
    /// <param name="LibraryUid">The unique identifier of the library.</param>
    /// <param name="LibraryName">The name of the library.</param>
    /// <param name="FlowUid">The unique identifier of the flow.</param>
    /// <param name="FlowName">The name of the flow.</param>
    /// <param name="ProcessingStarted">The date and time when processing started.</param>
    /// <param name="ProcessingEnded">The date and time when processing ended.</param>
    public record FileData(Guid NodeUid, string NodeName, long OriginalSize, long FinalSize,
        Guid LibraryUid, string LibraryName, Guid FlowUid, string FlowName,
        DateTime ProcessingStarted, DateTime ProcessingEnded);
}