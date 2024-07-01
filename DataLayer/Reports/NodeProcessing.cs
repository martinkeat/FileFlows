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
        const int MaxDaysForDaily = 35;
        const int MaxDaysForWeekly = 180; // Approximately 6 months

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
        var totalDays = (maxDateUtc.Value - minDateUtc.Value).Days;

        if (totalDays <= 1)
        {
            // Group by hour
            while (current <= maxDateUtc.Value)
            {
                AddRowToTable(tableData, data, current, current.AddHours(1), "yyyy-MM-dd HH:00");
                current = current.AddHours(1);
            }
        }
        else if (totalDays <= MaxDaysForDaily)
        {
            // Group by day
            while (current <= maxDateUtc.Value)
            {
                AddRowToTable(tableData, data, current, current.AddDays(1), "yyyy-MM-dd");
                current = current.AddDays(1);
            }
        }
        else if (totalDays <= MaxDaysForWeekly)
        {
            // Group by week
            while (current <= maxDateUtc.Value)
            {
                AddRowToTable(tableData, data, current, current.AddDays(7), "Week of yyyy-MM-dd");
                current = current.AddDays(7);
            }
        }
        else
        {
            // Group by month
            while (current <= maxDateUtc.Value)
            {
                var startOfNextMonth = new DateTime(current.Year, current.Month, 1).AddMonths(1);
                AddRowToTable(tableData, data, current, startOfNextMonth, "yyyy-MM");
                current = startOfNextMonth;
            }
        }

        // Ensure the line data contains an entry for every single day between minDateUtc and maxDateUtc
        EnsureLineDataHasDailyEntries(data, minDateUtc.Value, maxDateUtc.Value);

        var formatter = new SizeFormatter();

        var table = GenerateHtmlTableFromArray(new[] { "Date" }.Union(data.Keys).ToArray(), tableData.ToArray());
        var chart = GeneratMultiLineChart(new
        {
            labels = data.First().Value.Data.Keys,
            series = data.Select(x => new
            {
                name = x.Key,
                data = x.Value.Data.Values
            })
        });

        return (table ?? string.Empty) + (chart ?? string.Empty);
    }

    /// <summary>
    /// Adds a row to the table data by summing the values in the specified date range for each key in the data dictionary.
    /// </summary>
    /// <param name="tableData">The list of table data rows to which the new row will be added.</param>
    /// <param name="data">The dictionary containing the data series with dates as keys and values to be summed.</param>
    /// <param name="start">The start date of the range for the row.</param>
    /// <param name="end">The end date of the range for the row.</param>
    /// <param name="dateFormat">The date format string for the first column label.</param>
    private void AddRowToTable(List<object[]> tableData, Dictionary<string, NodeSeries> data, DateTime start, DateTime end, string dateFormat)
    {
        var row = new List<object> { start.ToString(dateFormat) };
        foreach (var key in data.Keys)
        {
            var total = data[key].Data
                .Where(d => d.Key >= start && d.Key < end)
                .Sum(d => d.Value);
            row.Add(total);
        }
        tableData.Add(row.ToArray());
    }


    /// <summary>
    /// Ensures that the line data has an entry for every single day between the specified minimum and maximum dates.
    /// </summary>
    /// <param name="data">The dictionary containing the data series.</param>
    /// <param name="minDate">The minimum date for the range.</param>
    /// <param name="maxDate">The maximum date for the range.</param>
    private void EnsureLineDataHasDailyEntries(Dictionary<string, NodeSeries> data, DateTime minDate, DateTime maxDate)
    {
        DateTime current = minDate;
        while (current <= maxDate)
        {
            foreach (var series in data.Values)
            {
                if (!series.Data.ContainsKey(current))
                {
                    series.Data[current] = 0;
                }
            }
            current = current.AddDays(1);
        }
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