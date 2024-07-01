using System.Security.Cryptography;
using FileFlows.Plugin;
using FileFlows.Plugin.Formatters;

namespace FileFlows.DataLayer.Reports;

/// <summary>
/// Detailed summary of processing performed on each processing node.
/// </summary>
public class NodeProcessing : Report
{
    /// <inheritdoc />
    public override Guid Uid => new("2c4f5e2e-de4c-41fc-9a49-672e70b9b07b");

    /// <inheritdoc />
    public override string Name => "Node Processing";

    /// <inheritdoc />
    public override string Description => "Detailed summary of processing performed on each processing node.";

    /// <inheritdoc />
    public override string Icon => "fas fa-desktop";

    /// <summary>
    /// Gets or sets the statistic to report on
    /// </summary>
    public ProcessedStatistic Statistic { get; set; }

    /// <inheritdoc />
    public override async Task<Result<string>> Generate(Dictionary<string, object> model)
    {
        var statistic = GetEnumValue<ProcessedStatistic>(model, nameof(Statistic)); 
        
        using var db = await GetDb();
        string sql =
            $"select {Wrap("NodeUid")}, {Wrap("NodeName")}, {Wrap("OriginalSize")}, " +
            $"{Wrap("FinalSize")}, {Wrap("ProcessingStarted")}, {Wrap("ProcessingEnded")} " +
            $"from {Wrap("LibraryFile")} where {Wrap("Status")} = 1";
        
        AddPeriodToSql(model, ref sql);
        
        var files = await db.Db.FetchAsync<NodeData>(sql);
        
        (DateTime? minDateUtc, DateTime? maxDateUtc) = GetPeriod(model);
        minDateUtc ??= files.Min(x => x.ProcessingStarted);
        maxDateUtc ??= files.Max(x => x.ProcessingStarted);

        Dictionary<string, NodeSeries> data = new();
        foreach (var file in files)
        {
            var date = file.ProcessingStarted.ToLocalTime();
            date = new DateTime(date.Year, date.Month, date.Day);
            string name = file.NodeName == "FileFlowsServer" ? "Internal Processing Node" : file.NodeName;

            data.TryAdd(name, new());
            var d = data[name].Data;
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
        List<object[]> tableData = new();
        DateTime current = minDateUtc.Value;
        while (current < maxDateUtc.Value)
        {
            var date = current.ToLocalTime();
            date = new DateTime(date.Year, date.Month, date.Day);
            var row = new List<object>();
            row.Add(date);
            foreach (var k in data.Keys)
            {
                data[k].Data.TryAdd(date, 0);
                row.Add(data[k].Data[date]);
            }
            tableData.Add(row.ToArray());

            current = current.AddDays(1);
        }
        
        var formatter = new SizeFormatter();

        
        var table = GenerateHtmlTableFromArray(new [] {"Date"}.Union(data.Keys).ToArray(), tableData.ToArray());

        var chart = GeneratMultiLineChart(new
        {
            labels = data.First().Value.Data.Keys,
            series =
                data.Select(x => new
                {
                    name = x.Key,
                    data = x.Value.Data.Values
                })
        });

        return (table ?? string.Empty) + (chart ?? string.Empty);
    }

    public record NodeData(Guid NodeUid, string NodeName, long OriginalSize, long FinalSize, DateTime ProcessingStarted, DateTime ProcessingEnded);

    /// <summary>
    /// Series data
    /// </summary>
    public class NodeSeries
    {
        /// <summary>
        /// Gets or sets the series name
        /// </summary>
        public string Name { get; set; } = null!;
        /// <summary>
        /// Gets or sets the series data
        /// </summary>
        public SortedDictionary<DateTime, double> Data { get; set; } = new();
    }
}