using FileFlows.DataLayer.Reports.Charts;

namespace FileFlows.DataLayer.Reports.Helpers;

/// <summary>
/// Date Based Chart Helper
/// </summary>
public static class DateBasedChartHelper
{
    private const int MaxDaysForDaily = 35;
    private const int MaxDaysForWeekly = 180; // Approximately 6 months

    /// <summary>
    /// Generates the HTML for a table and a chart based on the specified date range and data series.
    /// </summary>
    /// <param name="minDateUtc">The minimum date for the range.</param>
    /// <param name="maxDateUtc">The maximum date for the range.</param>
    /// <param name="data">The dictionary containing the data series.</param>
    /// <param name="emailing">if the report is being emailed and should generate SVG instead of javascript chart</param>
    /// <param name="tableDataFormatter">Optional formatter to use in the table data</param>
    /// <param name="yAxisFormatter">Optional formatter to use on the client for the y-axis value</param>
    /// <param name="generateTable">If the table should be generated</param>
    /// <param name="generateChart">If the chart should be generated</param>
    /// <returns>A string containing the HTML for the table and chart.</returns>
    public static string Generate(DateTime minDateUtc, DateTime maxDateUtc, 
        Dictionary<string, Dictionary<DateTime, long>> data, bool emailing,
        Func<double, string>? tableDataFormatter = null,
        string? yAxisFormatter = null, bool generateTable = true, bool generateChart = true)
    {
        var (labels, tableData) = GenerateTableData(minDateUtc, maxDateUtc, data, tableDataFormatter);

        // Ensure line chart labels are at daily intervals
        var dailyLabels = DateTimeLabelHelper.Generate(minDateUtc, maxDateUtc);

        string result = "";
        if(generateTable)
            result += TableGenerator.Generate(new[] { "Date" }.Union(data.Keys).ToArray(), tableData.ToArray()) ?? string.Empty;
        if(generateChart)
        result += MultiLineChart.Generate(new LineChartData
        {
            //Labels = dailyLabels.Select(label => label.ToString("yyyy-MM-dd")).ToArray(), // Convert DateTime to string here
            Labels = dailyLabels, // Convert DateTime to string here
            YAxisFormatter = yAxisFormatter,
            Series = data.Select(seriesItem => new ChartSeries
            {
                Name = seriesItem.Key,
                Data = dailyLabels.Select(label => (double)seriesItem.Value.GetValueOrDefault(label, 0)).ToArray()
            }).ToArray()
        }, emailing: emailing) ?? string.Empty;

        return result;
    }


    /// <summary>
    /// Generates hourly labels between the min and max dates.
    /// </summary>
    /// <param name="minDateUtc">The minimum date.</param>
    /// <param name="maxDateUtc">The maximum date.</param>
    /// <returns>An array of hourly labels.</returns>
    private static DateTime[] GenerateHourlyLabels(DateTime minDateUtc, DateTime maxDateUtc)
    {
        List<DateTime> labels = new List<DateTime>();
        DateTime currentDate = minDateUtc;

        while (currentDate <= maxDateUtc)
        {
            labels.Add(currentDate);
            currentDate = currentDate.AddHours(1);
        }

        return labels.ToArray();
    }

    /// <summary>
    /// Generates table data based on the specified date range and data series.
    /// </summary>
    /// <param name="minDateUtc">The minimum date for the range.</param>
    /// <param name="maxDateUtc">The maximum date for the range.</param>
    /// <param name="data">The dictionary containing the data series.</param>
    /// <param name="tableDataFormatter">Optional formatter to use in the table data</param>
    /// <returns>A tuple containing the labels and the table data.</returns>
    private static (string[], List<object[]>) GenerateTableData(DateTime minDateUtc, DateTime maxDateUtc,
        Dictionary<string, Dictionary<DateTime, long>> data, Func<double, string>? tableDataFormatter)
    {
        List<object[]> tableData = new();
        List<string> labels = new() { "Date" };
        DateTime current = minDateUtc;
        var totalDays = (maxDateUtc - minDateUtc).Days;

        if (totalDays <= 1)
        {
            // Group by hour
            while (current <= maxDateUtc)
            {
                AddRowToTable(tableData, data, current, current.AddHours(1), "{0:HH}:00", tableDataFormatter);
                labels.Add(current.ToString("yyyy-MM-dd HH:00"));
                current = current.AddHours(1);

                // Ensure the line data contains an entry for every single day between minDateUtc and maxDateUtc
                EnsureLineDataHasHourlyEntries(data, minDateUtc, maxDateUtc);
            }
        }
        else if (totalDays <= MaxDaysForDaily)
        {
            // Group by day
            while (current <= maxDateUtc)
            {
                AddRowToTable(tableData, data, current, current.AddDays(1), "{0:yyyy}-{0:MM}-{0:dd}", tableDataFormatter);
                labels.Add(current.ToString("yyyy-MM-dd"));
                current = current.AddDays(1);
            }
        }
        else if (totalDays <= MaxDaysForWeekly)
        {
            // Group by week
            while (current <= maxDateUtc)
            {
                AddRowToTable(tableData, data, current, current.AddDays(7), "Week of {0:yyyy}-{0:MM}-{0:dd}", tableDataFormatter);
                labels.Add($"Week of {current:yyyy-MM-dd}");
                current = current.AddDays(7);
            }
        }
        else
        {
            // Group by month
            while (current <= maxDateUtc)
            {
                var startOfNextMonth = new DateTime(current.Year, current.Month, 1).AddMonths(1);
                AddRowToTable(tableData, data, current, startOfNextMonth, "{0:MMM} '{0:yy}", tableDataFormatter);
                labels.Add(current.ToString("yyyy-MM"));
                current = startOfNextMonth;
            }
        }

        // Ensure the line data contains an entry for every single day between minDateUtc and maxDateUtc
        EnsureLineDataHasDailyEntries(data, minDateUtc, maxDateUtc);

        return (labels.ToArray(), tableData);
    }

    /// <summary>
    /// Adds a row to the table data by summing the values in the specified date range for each key in the data dictionary.
    /// </summary>
    /// <param name="tableData">The list of table data rows to which the new row will be added.</param>
    /// <param name="data">The dictionary containing the data series with dates as keys and values to be summed.</param>
    /// <param name="start">The start date of the range for the row.</param>
    /// <param name="end">The end date of the range for the row.</param>
    /// <param name="tableDataFormatter">Optional formatter to use in the table data</param>
    /// <param name="dateFormat">The date format string for the first column label.</param>
    private static void AddRowToTable(List<object[]> tableData, Dictionary<string, Dictionary<DateTime, long>> data,
        DateTime start, DateTime end, string dateFormat, Func<double, string>? tableDataFormatter)
    {
        var row = new List<object> { string.Format(dateFormat, start.ToLocalTime()) };
        foreach (var key in data.Keys)
        {
            var total = data[key]
                .Where(d => d.Key >= start && d.Key < end)
                .Sum(d => d.Value);
            if(tableDataFormatter != null)
                row.Add(tableDataFormatter(total));
            else
                row.Add(total);
        }

        tableData.Add(row.ToArray());
    }

    /// <summary>
    /// Ensures that the line data has an entry for every single hour between the specified minimum and maximum dates.
    /// </summary>
    /// <param name="data">The dictionary containing the data series.</param>
    /// <param name="minDate">The minimum date for the range.</param>
    /// <param name="maxDate">The maximum date for the range.</param>
    private static void EnsureLineDataHasHourlyEntries(Dictionary<string, Dictionary<DateTime, long>> data,
        DateTime minDate, DateTime maxDate)
    {
        DateTime current = minDate;
        while (current <= maxDate)
        {
            foreach (var series in data.Values)
            {
                series.TryAdd(current, 0);
            }

            current = current.AddHours(1);
        }
    }

    /// <summary>
    /// Ensures that the line data has an entry for every single day between the specified minimum and maximum dates.
    /// </summary>
    /// <param name="data">The dictionary containing the data series.</param>
    /// <param name="minDate">The minimum date for the range.</param>
    /// <param name="maxDate">The maximum date for the range.</param>
    private static void EnsureLineDataHasDailyEntries(Dictionary<string, Dictionary<DateTime, long>> data,
        DateTime minDate, DateTime maxDate)
    {
        DateTime current = minDate;
        while (current <= maxDate)
        {
            foreach (var series in data.Values)
            {
                series.TryAdd(current, 0);
            }

            current = current.AddDays(1);
        }
    }
}
