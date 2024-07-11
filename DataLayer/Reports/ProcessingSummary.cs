using FileFlows.DataLayer.Reports.Charts;
using FileFlows.DataLayer.Reports.Helpers;
using FileFlows.Plugin;
using FileFlows.Shared.Formatters;
using FileFlows.Shared.Models;
using Humanizer;

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
            $"select {Wrap("Name")}, {Wrap("NodeUid")}, {Wrap("NodeName")}, {Wrap("OriginalSize")}, " +
            $"{Wrap("FinalSize")}, {Wrap("ProcessingStarted")}, {Wrap("ProcessingEnded")}, " +
            $"{Wrap("LibraryUid")}, {Wrap("LibraryName")}, {Wrap("FlowUid")}, {Wrap("FlowName")} " +
            $"from {Wrap("LibraryFile")} where {Wrap("Status")} = 1";

        AddPeriodToSql(model, ref sql);
        AddLibrariesToSql(model, ref sql);
        AddFlowsToSql(model, ref sql);
        AddNodesToSql(model, ref sql);

        var nodeUids = GetUids("Node", model).Where(x => x != null).ToList();
        var libraryUids = GetUids("Library", model).Where(x => x != null).ToList();
        
        var files = await db.Db.FetchAsync<FileData>(sql);
        if (files.Count < 1)
            return string.Empty; // no data

        string failedSql = $"select count({Wrap("Uid")}) from {Wrap("LibraryFile")} where {Wrap("Status")} = 4 ";
        AddPeriodToSql(model, ref failedSql);
        var failedFiles = await db.Db.ExecuteScalarAsync<int>(failedSql);
        
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

        double totalSeconds = 0, totalBytes = 0, totalSavedBytes = 0;
        int totalFiles = 0;

        List<FileData> largestFiles = files.OrderByDescending(x => x.OriginalSize).Take(TableGenerator.MIN_TABLE_ROWS).ToList();
        List<FileData> mostSaved = files.Where(x => x.OriginalSize >= x.FinalSize)
            .OrderByDescending(x => x.OriginalSize - x.FinalSize)
            .Take(TableGenerator.MIN_TABLE_ROWS).ToList();
        List<FileData> longestRunning = files.OrderByDescending(x => x.ProcessingEnded - x.ProcessingStarted).Take(TableGenerator.MIN_TABLE_ROWS).ToList();

        foreach (var file in files)
        {
            totalFiles++;
            totalBytes += file.OriginalSize;
            totalSavedBytes += (file.OriginalSize - file.FinalSize);
            totalSeconds += (int)(file.ProcessingEnded - file.ProcessingStarted).TotalSeconds;
            var date = file.ProcessingStarted.ToLocalTime();
            date = hourly
                ? new DateTime(date.Year, date.Month, date.Day, date.Hour, 0, 0)
                : new DateTime(date.Year, date.Month, date.Day);

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


            string libraryName = libraryUids.Count > 0 ? file.LibraryName : "All Libraries";
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
        ReportBuilder builder = new(emailing);

        builder.StartRow(3);
        builder.AddPeriodSummaryBox(minDateUtc.Value, maxDateUtc.Value);
        foreach (var sum in new[]
                 {
                     ("Total Files", totalFiles.ToString("N0"), ReportSummaryBox.IconType.File, ReportSummaryBox.BoxColor.Info),
                     ("Failed Files", failedFiles.ToString("N0"), ReportSummaryBox.IconType.ExclamationCircle, ReportSummaryBox.BoxColor.Error),
                 })
        {
            builder.AddSummaryBox(sum.Item1, sum.Item2, sum.Item3, sum.Item4);
        }
        builder.EndRow();

        SummaryRow[] summaryRows =
        [
            new()
            {
                TableTitle = "Largest Files",
                TableUnitColumn = "Size",
                TableData = largestFiles.Select(x => new object[]
                {
                    FileNameFormatter.Format(x.Name),
                    FileSizeFormatter.Format(x.OriginalSize)
                }).ToArray(),
                ChartTitle = "Total Files",
                ChartData = nodeDataCount,
                ChartYAxisFormatter = ""
            }
        ];

        foreach (var sumRow in summaryRows)
        {
            AddSummaryRow(builder, sumRow, labels, emailing);
        }
        
        builder.StartRow(3);
        foreach (var sum in new[]
                 {
                     //("Processing Time", TimeSpan.FromSeconds(totalSeconds).Humanize(1), "far fa-clock", ""),
                     ("Bytes Processed", FileSizeFormatter.Format(totalBytes), ReportSummaryBox.IconType.HardDrive, ReportSummaryBox.BoxColor.Info),
                     ("Average Size", FileSizeFormatter.Format(totalBytes / Convert.ToDouble(totalFiles)), ReportSummaryBox.IconType.BalanceScale, ReportSummaryBox.BoxColor.Warning),
                     ("Storage Sized", FileSizeFormatter.Format(totalSavedBytes), ReportSummaryBox.IconType.HardDrive, totalSavedBytes > 0 ? ReportSummaryBox.BoxColor.Success : ReportSummaryBox.BoxColor.Error),
                 })
        {
            builder.AddSummaryBox(sum.Item1, sum.Item2, sum.Item3, sum.Item4);
        }
        builder.EndRow();
        
        AddSummaryRow(builder, new ()
        {
            TableTitle = "Biggest Savings",
            TableUnitColumn = "Savings",
            TableData = mostSaved.Select(x => new object[]
            {
                FileNameFormatter.Format(x.Name),
                FileSizeFormatter.Format(x.OriginalSize - x.FinalSize)
            }).ToArray(),

            ChartTitle = "File Size",
            ChartData = nodeDataSize,
            ChartYAxisFormatter = "filesize"
        }, labels, emailing);

        builder.StartRow(4);
        foreach (var sum in new[]
                 {
                     ("Processing Time", TimeSpan.FromSeconds(totalSeconds).Humanize(1), ReportSummaryBox.IconType.Clock, ReportSummaryBox.BoxColor.Info),
                     ("Shortest Time", files.Min(x =>x.ProcessingEnded - x.ProcessingStarted).Humanize(1), ReportSummaryBox.IconType.HourglassEnd, ReportSummaryBox.BoxColor.Success),
                     ("Average Time", TimeSpan.FromSeconds(totalSeconds / Convert.ToDouble(totalFiles)).Humanize(1), ReportSummaryBox.IconType.HourglassHalf, ReportSummaryBox.BoxColor.Warning),
                     ("Longest Time", files.Max(x =>x.ProcessingEnded - x.ProcessingStarted).Humanize(1), ReportSummaryBox.IconType.HourglassStart, ReportSummaryBox.BoxColor.Error),
                 })
        {
            builder.AddSummaryBox(sum.Item1, sum.Item2, sum.Item3, sum.Item4);
        }
        builder.EndRow();
        
        AddSummaryRow(builder, new()
        {
            TableTitle = "Longest Running",
            TableUnitColumn = "Time",
            TableData = longestRunning.Select(x => new object[]
            {
                FileNameFormatter.Format(x.Name),
                (x.ProcessingEnded - x.ProcessingStarted).Humanize(1)
            }).ToArray(),
            ChartTitle = "Processing Time",
            ChartData = nodeDataTime,
            ChartYAxisFormatter = ""
        }, labels, emailing);

        // foreach (var sumRow in summaryRows)
        // {
        //     AddSummaryRow(output, sumRow, labels, emailing);
        // }
        foreach (var group in new[]
                 {
                     // new[]
                     // {
                     //     ("Node Files", nodeDataCount, ""),
                     //     ("Node Size", nodeDataSize, "filesize"),
                     //     ("Node Time", nodeDataTime, "")
                     // },
                     new[]
                     {
                         //("Library Files", libDataCount, ""),
                         ("Library Size", libDataSize, "filesize"),
                         //("Library Time", libDataTime, "")

                     }
                 })
        {
            builder.StartRow(2);
            var t = libDataCount.ToDictionary(x => x.Key,
                x =>
                {
                    int count = 0;
                    foreach (var v in x.Value.Values)
                        count += (int)v;
                    return count;
                }).OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
            builder.AddRowItem(PieChart.Generate(new()
            {
                Title = "Libraries",
                Data = t
            }, emailing: emailing));
            foreach (var chart in group)
            {
                // output.AppendLine(MultiLineChart.Generate(new MultilineChartData
                // {
                //     Title = chart.Item1,
                //     Labels = labels,
                //     YAxisFormatter = chart.Item3,
                //     Series = chart.Item2.Select(seriesItem => new ChartSeries
                //     {
                //         Name = seriesItem.Key,
                //         Data = labels.Select(label => (double)seriesItem.Value.GetValueOrDefault(label, 0)).ToArray()
                //     }).ToArray()
                // }, generateSvg: emailing));
                var t2 = libDataSize.ToDictionary(x => x.Key,
                    x =>
                    {
                        double count = 0;
                        foreach (var v in x.Value.Values)
                            count += v;
                        return count;
                    }).OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
                
                builder.AddRowItem(BarChart.Generate(new BarChartData()
                {
                    Title = chart.Item1,
                    Data = t2,
                    YAxisFormatter = "filesize"
                }, emailing: emailing));
            }

            builder.EndRow();
        }

        return builder.ToString();
    }


    private void AddSummaryRow(ReportBuilder builder, SummaryRow sumRow, DateTime[] labels, bool emailing)
    {
        builder.StartChartTableRow();
            
        builder.AddRowItem(MultiLineChart.Generate(new LineChartData
        {
            Title = sumRow.ChartTitle,
            Labels = labels,
            YAxisFormatter = sumRow.ChartYAxisFormatter,
            Series = sumRow.ChartData.Select(seriesItem => new ChartSeries
            {
                Name = seriesItem.Key,
                Data = labels.Select(label => (double)seriesItem.Value.GetValueOrDefault(label, 0)).ToArray()
            }).ToArray()
        }, emailing: emailing));

        var table = TableGenerator.GenerateMinimumTable(sumRow.TableTitle,
            ["Name", sumRow.TableUnitColumn],
            sumRow.TableData, emailing: emailing
        );
            
        builder.AddRowItem(table);
            
        builder.EndRow();
    }


    /// <summary>
    /// Summary row
    /// </summary>
    private class SummaryRow
    {
        /// <summary>
        /// Gets or sets the table title
        /// </summary>
        public string TableTitle { get; set; } = null!;
        /// <summary>
        /// Gets or sets the table data
        /// </summary>
        public object[][] TableData { get; set; } = null!;
        /// <summary>
        /// Gets or sets the table unit column
        /// </summary>
        public string TableUnitColumn { get; set; } = null!;
        
        /// <summary>
        /// Gets or sets the chart title
        /// </summary>
        public string ChartTitle { get; set; } = null!;
        /// <summary>
        /// Gets or sets the chart data
        /// </summary>
        public Dictionary<string, Dictionary<DateTime, long>> ChartData { get; set; } = null!;
        /// <summary>
        /// Gets or sets the chart y-axis formatter
        /// </summary>
        public string ChartYAxisFormatter { get; set; } = null!;
    }

    /// <summary>
    /// Represents the data for a node in the processing system.
    /// </summary>
    public class FileData
    {
        /// <summary>
        /// Gets or sets the relative name of the file.
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// Gets or sets the unique identifier of the node.
        /// </summary>
        public Guid NodeUid { get; set; }

        /// <summary>
        /// Gets or sets the name of the node.
        /// </summary>
        public string NodeName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the original size of the data before processing.
        /// </summary>
        public long OriginalSize { get; set; }

        /// <summary>
        /// Gets or sets the final size of the data after processing.
        /// </summary>
        public long FinalSize { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the library.
        /// </summary>
        public Guid LibraryUid { get; set; }

        /// <summary>
        /// Gets or sets the name of the library.
        /// </summary>
        public string LibraryName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the unique identifier of the flow.
        /// </summary>
        public Guid FlowUid { get; set; }

        /// <summary>
        /// Gets or sets the name of the flow.
        /// </summary>
        public string FlowName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the date and time when processing started.
        /// </summary>
        public DateTime ProcessingStarted { get; set; }

        /// <summary>
        /// Gets or sets the date and time when processing ended.
        /// </summary>
        public DateTime ProcessingEnded { get; set; }
    }
}