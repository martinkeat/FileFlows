using System.Globalization;

namespace FileFlows.DataLayer.Reports.Helpers;


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
    /// <returns>A string containing the HTML for the table and chart.</returns>
    public static string Generate(DateTime minDateUtc, DateTime maxDateUtc, Dictionary<string, Dictionary<DateTime, long>> data)
    {
        var (labels, tableData) = GenerateTableData(minDateUtc, maxDateUtc, data);

        // Ensure line chart labels are at daily intervals
        var dailyLabels = GenerateDailyLabels(minDateUtc, maxDateUtc);

        var table = TableGenerator.Generate(new[] { "Date" }.Union(data.Keys).ToArray(), tableData.ToArray());
        var chart = MultiLineChart.Generate(new
        {
            labels = dailyLabels.Select(label => label.ToString("yyyy-MM-dd")), // Convert DateTime to string here
            series = data.Select(seriesItem => new
            {
                name = seriesItem.Key,
                data = dailyLabels.Select(label => seriesItem.Value.GetValueOrDefault(label, 0)).ToList()
            }).ToList()
        });

        return (table ?? string.Empty) + (chart ?? string.Empty);
    }

    /// <summary>
    /// Generates daily labels between the min and max dates.
    /// </summary>
    /// <param name="minDateUtc">The minimum date.</param>
    /// <param name="maxDateUtc">The maximum date.</param>
    /// <returns>An array of daily labels.</returns>
    private static DateTime[] GenerateDailyLabels(DateTime minDateUtc, DateTime maxDateUtc)
    {
        List<DateTime> labels = new List<DateTime>();
        DateTime currentDate = minDateUtc.Date;

        while (currentDate <= maxDateUtc)
        {
            labels.Add(currentDate);
            currentDate = currentDate.AddDays(1);
        }

        return labels.ToArray();
    }

    /// <summary>
    /// Parses a date string to DateTime, handling the format "yyyy-MM-dd", "yyyy-MM", etc.
    /// </summary>
    /// <param name="dateString">The date string to parse.</param>
    /// <returns>The parsed DateTime if successful, otherwise DateTime.MinValue.</returns>
    private static DateTime ParseDate(string dateString)
    {
        if (DateTime.TryParseExact(dateString, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
        {
            return parsedDate;
        }
        else if (DateTime.TryParseExact(dateString, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
        {
            return parsedDate;
        }
        // Add more formats if needed

        return DateTime.MinValue; // Or throw exception if parsing fails
    }
    
    /// <summary>
    /// Generates table data based on the specified date range and data series.
    /// </summary>
    /// <param name="minDateUtc">The minimum date for the range.</param>
    /// <param name="maxDateUtc">The maximum date for the range.</param>
    /// <param name="data">The dictionary containing the data series.</param>
    /// <returns>A tuple containing the labels and the table data.</returns>
    private static (string[], List<object[]>) GenerateTableData(DateTime minDateUtc, DateTime maxDateUtc,
        Dictionary<string, Dictionary<DateTime, long>> data)
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
                AddRowToTable(tableData, data, current, current.AddHours(1), "yyyy-MM-dd HH:00");
                labels.Add(current.ToString("yyyy-MM-dd HH:00"));
                current = current.AddHours(1);
            }
        }
        else if (totalDays <= MaxDaysForDaily)
        {
            // Group by day
            while (current <= maxDateUtc)
            {
                AddRowToTable(tableData, data, current, current.AddDays(1), "yyyy-MM-dd");
                labels.Add(current.ToString("yyyy-MM-dd"));
                current = current.AddDays(1);
            }
        }
        else if (totalDays <= MaxDaysForWeekly)
        {
            // Group by week
            while (current <= maxDateUtc)
            {
                AddRowToTable(tableData, data, current, current.AddDays(7), "Week of yyyy-MM-dd");
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
                AddRowToTable(tableData, data, current, startOfNextMonth, "yyyy-MM");
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
    /// <param name="dateFormat">The date format string for the first column label.</param>
    private static void AddRowToTable(List<object[]> tableData, Dictionary<string, Dictionary<DateTime, long>> data,
        DateTime start, DateTime end, string dateFormat)
    {
        var row = new List<object> { start.ToString(dateFormat) };
        foreach (var key in data.Keys)
        {
            var total = data[key]
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
    private static void EnsureLineDataHasDailyEntries(Dictionary<string, Dictionary<DateTime, long>> data,
        DateTime minDate, DateTime maxDate)
    {
        DateTime current = minDate;
        while (current <= maxDate)
        {
            foreach (var series in data.Values)
            {
                if (!series.ContainsKey(current))
                {
                    series[current] = 0;
                }
            }

            current = current.AddDays(1);
        }
    }
}
