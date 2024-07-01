using System.Text.Json.Serialization;

namespace FileFlows.ServerShared.Models.StatisticModels;

/// <summary>
/// Heat map data
/// </summary>
public class Heatmap
{
    /// <summary>
    /// Gets or sets the data that is bucketed into 15 minute intervals
    /// </summary>
    public SortedDictionary<int, int> Data { get; set; } = new();

    /// <summary>
    /// Converts the data to heatmap data
    /// </summary>
    /// <returns>the converted data</returns>
    public List<HeatmapData> ConvertData()
    {
        var data = Data ?? new();

        // ensure all 672 buckets are included
        for (int i = 0; i <= 672; i++)
            data.TryAdd(i, 0);

        Dictionary<string, HeatmapData> results = new();
        var days = new[] { "MON", "TUE", "WED", "THU", "FRI", "SAT", "SUN" };
        foreach(var day in days.Reverse())
            results.Add(day, new () { Day = day, Data = new () });

        int offset = -96; // 0-96 is sunday, we want to start monday
        
        for (int index = 0; index < 672; index++)
        {
            int i = index + offset;
            if (i < 0)
                i = 672 + i;
            else if (i >= 672)
                i -= 672;
                
            string day = days[(int)Math.Floor(i / 96f)];
            int time = i % 96;
            int hour = (int)Math.Floor(time / 4f);
            string x = hour == 0 ? "12am" : hour == 12 ? "12pm" : hour > 12 ? (hour - 12) + "pm" : hour + "am";
            var existing = results[day].Data.FirstOrDefault(y => y.Time == x);
            if (existing != null)
                existing.Total += data[index];
            else
                results[day].Data.Add(new () { Time = x, Total = data[index] });
        }

        return results.Values.ToList();
    }
}

/// <summary>
/// Heatmap data
/// </summary>
public class HeatmapData
{
    /// <summary>
    /// Gets or sets the day name, should be 3 digits
    /// </summary>
    [JsonPropertyName("name")]
    public string Day { get; set; } = null!;

    /// <summary>
    /// Gets or sets the data for this day
    /// </summary>
    [JsonPropertyName("data")]
    public List<HeatmapDayData> Data { get; set; } = new();
}

/// <summary>
/// Heatmap day data
/// </summary>
public class HeatmapDayData
{
    /// <summary>
    /// Gets or sets the hour name, with the am/pm
    /// </summary>
    [JsonPropertyName("x")]
    public string Time { get; set; } = null!;
    /// <summary>
    /// Gets or sets the total for this hour
    /// </summary>
    [JsonPropertyName("y")]
    public int Total { get; set; }
}