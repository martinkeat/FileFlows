using System.Text;
using System.Text.Json;
using FileFlows.DataLayer.Reports.Charts;
using FileFlows.DataLayer.Reports.Helpers;
using FileFlows.Plugin;

namespace FileFlows.DataLayer.Reports;

/// <summary>
/// Report for Codecs
/// </summary>
public class Codecs : Report
{
    /// <inheritdoc />
    public override Guid Uid => new Guid("052bf1b7-9912-4e7e-90c7-c2e7ba7fcea3");
    /// <inheritdoc />
    public override string Name => "Codecs";
    /// <inheritdoc />
    public override string Description => "Shows the different codecs processed through FileFlows.";
    /// <inheritdoc />
    public override string Icon => "fas fa-photo-video";
    
    /// <summary>
    /// Gets or sets the stream type
    /// </summary>
    public StreamType Type { get; set; }
    
    /// <summary>
    /// Gets or sets the direction
    /// </summary>
    public IODirection Direction { get; set; }

    /// <inheritdoc />
    public override async Task<Result<string>> Generate(Dictionary<string, object> model, bool emailing)
    {
        var streamType = GetEnumValue<StreamType>(model, nameof(Type)); 
        var direction = GetEnumValue<IODirection>(model, nameof(Direction)); 

        using var db = await GetDb();
        string sql =
            $"select {Wrap(direction == IODirection.Input ? "OriginalMetadata" : "FinalMetadata")} as {Wrap("Metadata")} from {Wrap("LibraryFile")} where {Wrap("Status")} = 1";
        AddLibrariesToSql(model, ref sql);
        AddPeriodToSql(model, ref sql);

        var metadataString = await db.Db.FetchAsync<string>(sql);
        var metadata = metadataString.Where(x => string.IsNullOrWhiteSpace(x) == false)
            .Select(x => JsonSerializer.Deserialize<Dictionary<string, object>>(x, DbLibraryFileManager.JsonOptions)!)
            .ToList();

        Dictionary<string, int> codecs = new();
        var prefix = streamType switch
        {
            StreamType.Audio => "audio",
            StreamType.Subtitle => "subtitle",
            StreamType.Video => "video",
            _ => null
        };
        foreach (var dict in metadata)
        {
            foreach (var key in dict.Keys)
            {
                if (key.ToLowerInvariant().EndsWith(" codec") == false)
                    continue;
                if (prefix != null && key.ToLowerInvariant().StartsWith(prefix) == false)
                    continue;
                var value = dict[key];
                if (value is JsonElement je && je.ValueKind == JsonValueKind.String)
                    value = je.GetString();
                if (value is string codec == false)
                    continue;
                if (codecs.TryAdd(codec, 1) == false)
                    codecs[codec] += 1;
            }
        }

        var data = codecs.OrderByDescending(x => x.Value)
            .Select(x => new { Codec = x.Key, Count = x.Value })
            .ToList();

        if (data.Count == 0)
            return string.Empty;

        var builder = new StringBuilder();
        var fewest = data.OrderBy(kv => kv.Count).First().Codec;
        var top = data.OrderByDescending(kv => kv.Count).First().Codec;
        int averageCount = (int)Math.Round(data.Average(x => x.Count));
        
        builder.AppendLine("<div class=\"report-row report-row-3\">");
        builder.AppendLine(ReportSummaryBox.Generate("Top Codec", top, ReportSummaryBox.IconType.ArrowAltCircleUp,
            ReportSummaryBox.BoxColor.Info));
        builder.AppendLine(ReportSummaryBox.Generate("Least Codec", fewest, ReportSummaryBox.IconType.ArrowAltCircleDown,
            ReportSummaryBox.BoxColor.Warning));
        builder.AppendLine(ReportSummaryBox.Generate("Average", averageCount.ToString("N0"), ReportSummaryBox.IconType.BalanceScale,
            ReportSummaryBox.BoxColor.Error));
        builder.AppendLine("</div>");

        builder.AppendLine("<div class=\"report-row report-row-3\">");

        builder.AppendLine(TreeMap.Generate(new()
        {
            Data = data.ToDictionary(x => x.Codec, x => x.Count),
        }, true));
        
        // html += PieChart.Generate(new ()
        // {
        //     Data = data.ToDictionary(x => x.Codec, x => x.Count),
        // }, emailing);

        builder.AppendLine("<div class=\"report-span-2\">" + TableGenerator.Generate(data) + "</div>");
        builder.AppendLine("</div>");
        
        return builder.ToString();

    }

    private class CodecData
    {
        public string Name { get; set; } = null!;
        public int Count { get; set; }
    }
}
