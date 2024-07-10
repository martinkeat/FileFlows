using FileFlows.DataLayer.Reports.Helpers;
using FileFlows.Plugin;
using FileFlows.ServerShared;
using FileFlows.Shared.Formatters;
using FileFlows.Shared.Models;
using Humanizer;

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
    
    /// <inheritdoc />
    public override ReportSelection LibrarySelection => ReportSelection.Any;

    /// <inheritdoc />
    public override async Task<Result<string>> Generate(Dictionary<string, object> model, bool emailing)
    {
        using var db = await GetDb();
        string sql =
            $"select {Wrap("Name")}, {Wrap("NodeUid")}, {Wrap("NodeName")}, {Wrap("OriginalSize")}, " +
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
        
        double totalSeconds = 0, totalBytes = 0, totalSavedBytes = 0;
        int totalFiles = 0;

        Dictionary<string, Dictionary<DateTime, long>> dataCount = new();
        Dictionary<string, Dictionary<DateTime, long>> dataSize = new();
        Dictionary<string, Dictionary<DateTime, long>> dataTime = new();
        foreach (var file in files)
        {
            totalFiles++;
            totalBytes += file.OriginalSize;
            totalSavedBytes += (file.OriginalSize - file.FinalSize);
            totalSeconds += (int)(file.ProcessingEnded - file.ProcessingStarted).TotalSeconds;
            
            var date = file.ProcessingStarted.ToLocalTime();
            date = new DateTime(date.Year, date.Month, date.Day);
            string name;
            if (nodeUids.Count > 0)
                name = file.NodeName == "FileFlowsServer" ? "Internal Processing Node" : file.NodeName;
            else
                name = string.Empty;

            dataCount.TryAdd(name, new Dictionary<DateTime, long>());
            var dCount = dataCount[name];
            dCount.TryAdd(date, 0);
            dCount[date] += 1;
            
            dataSize.TryAdd(name, new Dictionary<DateTime, long>());
            var dSize = dataSize[name];
            dSize.TryAdd(date, 0);
            dSize[date] += file.OriginalSize;
            
            dataTime.TryAdd(name, new Dictionary<DateTime, long>());
            var dTime = dataTime[name];
            dTime.TryAdd(date, 0);
            dTime[date] += (int)(file.ProcessingEnded - file.ProcessingStarted).TotalSeconds;
        }

        if (dataCount.Count == 0)
            return string.Empty;

        ReportBuilder builder = new(emailing);
        
        builder.StartRow(4);
        builder.AddPeriodSummaryBox(minDateUtc.Value, maxDateUtc.Value);
        builder.AddSummaryBox("Total Files", totalFiles, ReportSummaryBox.IconType.File, ReportSummaryBox.BoxColor.Info);
        builder.AddSummaryBox("Total Size", FileSizeFormatter.Format(totalBytes), ReportSummaryBox.IconType.HardDrive, ReportSummaryBox.BoxColor.Info);
        builder.AddSummaryBox("Total Time", TimeSpan.FromSeconds(totalSeconds).Humanize(2), ReportSummaryBox.IconType.Clock, ReportSummaryBox.BoxColor.Info);
        builder.EndRow();
        
            
        builder.StartChartTableRow();
        builder.AddRowItem(DateBasedChartHelper.Generate(minDateUtc.Value, maxDateUtc.Value, dataCount, emailing, generateTable: false));
        builder.AddRowItem(TableGenerator.GenerateMinimumTable("Most Files", ["Node", "Count"],
            files.GroupBy(x => x.NodeName).Select(x => new { Node = x.Key == Globals.InternalNodeName ? "Internal Processing Node" : x.Key, Count = x.Count()})
                .OrderByDescending(x => x.Count)
                .Select(x => new object[] { x.Node, x.Count})
                .Take(TableGenerator.MIN_TABLE_ROWS).ToArray()
            , emailing: emailing));
        builder.EndRow();
        
        builder.StartChartTableRow();
        builder.AddRowItem(DateBasedChartHelper.Generate(minDateUtc.Value, maxDateUtc.Value, dataSize, emailing,
            tableDataFormatter: (dbl) => FileSizeFormatter.Format(dbl, 2), yAxisFormatter: "filesize", generateTable: false));
        builder.AddRowItem(TableGenerator.GenerateMinimumTable("Largest Files", ["Name", "Node", "Size"],
            files.OrderByDescending(x => x.OriginalSize).Select(x => new object[] { 
                    FileNameFormatter.Format(x.Name), 
                    x.NodeName == Globals.InternalNodeName ? "Internal Node" : x.NodeName,
                    FileSizeFormatter.Format(x.OriginalSize)})
                .Take(TableGenerator.MIN_TABLE_ROWS).ToArray()
            , widths: ["", "10rem", ""], emailing: emailing));
        builder.EndRow();
        
        builder.StartChartTableRow();
        builder.AddRowItem(DateBasedChartHelper.Generate(minDateUtc.Value, maxDateUtc.Value, dataTime, emailing,
            tableDataFormatter: TimeFormatter.Format, generateTable: false));
        builder.AddRowItem(TableGenerator.GenerateMinimumTable("Longest Time Taken", ["Name", "Node", "Size"],
            files.OrderByDescending(x => x.OriginalSize).Select(x => new object[]
                {
                    FileNameFormatter.Format(x.Name), 
                    x.NodeName == Globals.InternalNodeName ? "Internal Node" : x.NodeName,
                    FileSizeFormatter.Format(x.OriginalSize)
                })
                .Take(TableGenerator.MIN_TABLE_ROWS).ToArray()
        , widths: ["", "10rem", ""], emailing: emailing));
        builder.EndRow();

        return builder.ToString();
    }
    
    /// <summary>
    /// Represents the data for a node in the processing system.
    /// </summary>
    /// <param name="Name">The name of the file.</param>
    /// <param name="NodeUid">The unique identifier of the node.</param>
    /// <param name="NodeName">The name of the node.</param>
    /// <param name="OriginalSize">The original size of the data before processing.</param>
    /// <param name="FinalSize">The final size of the data after processing.</param>
    /// <param name="ProcessingStarted">The date and time when processing started.</param>
    /// <param name="ProcessingEnded">The date and time when processing ended.</param>
    public record NodeData(string Name, Guid NodeUid, string NodeName, long OriginalSize, long FinalSize, DateTime ProcessingStarted, DateTime ProcessingEnded);
}
