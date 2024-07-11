using System.Text.Json;
using FileFlows.DataLayer.Reports.Charts;
using FileFlows.DataLayer.Reports.Helpers;
using FileFlows.Plugin;
using FileFlows.Shared.Models;

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
    /// <inheritdoc />
    public override bool Direction => true;

    /// <inheritdoc />
    public override async Task<Result<string>> Generate(Dictionary<string, object> model, bool emailing)
    {
        var direction = GetDirection(model); 

        (DateTime? minDateUtc, DateTime? maxDateUtc) = GetPeriod(model);
        minDateUtc ??= DateTime.MinValue;
        maxDateUtc ??= DateTime.MaxValue;
        
        using var db = await GetDb();
        string sql =
            $"select {Wrap(direction == ReportDirection.Input ? "OriginalMetadata" : "FinalMetadata")} as {Wrap("Metadata")} from {Wrap("LibraryFile")} where {Wrap("Status")} = 1";
        AddLibrariesToSql(model, ref sql);
        AddPeriodToSql(model, ref sql);

        var metadataString = await db.Db.FetchAsync<string>(sql);
        var metadata = metadataString.Where(x => string.IsNullOrWhiteSpace(x) == false)
            .Select(x => JsonSerializer.Deserialize<Dictionary<string, object>>(x, DbLibraryFileManager.JsonOptions)!)
            .ToList();

        Dictionary<string, int> videoCodecs = new();
        Dictionary<string, int> audioCodecs = new();
        Dictionary<string, int> subtitleCodecs = new();
        // var prefix = streamType switch
        // {
        //     StreamType.Audio => "audio",
        //     StreamType.Subtitle => "subtitle",
        //     StreamType.Video => "video",
        //     _ => null
        // };
        foreach (var dict in metadata)
        {
            foreach (var key in dict.Keys)
            {
                if (key.ToLowerInvariant().EndsWith(" codec") == false)
                    continue;
                var value = dict[key];
                if (value is JsonElement je && je.ValueKind == JsonValueKind.String)
                    value = je.GetString();
                if (value is string codec == false)
                    continue;
                var codecs = key.ToLowerInvariant().StartsWith("video") ? videoCodecs :
                    key.ToLowerInvariant().StartsWith("audio") ? audioCodecs :
                    key.ToLowerInvariant().StartsWith("subtitle") ? subtitleCodecs :
                    null;
                if (codecs == null)
                    continue;
                    
                if (codecs.TryAdd(codec, 1) == false)
                    codecs[codec] += 1;
            }
        }

        var dataVideo = videoCodecs.OrderByDescending(x => x.Value)
            .Select(x => new { Codec = x.Key, Count = x.Value })
            .ToList();
        var dataAudio = audioCodecs.OrderByDescending(x => x.Value)
            .Select(x => new { Codec = x.Key, Count = x.Value })
            .ToList();
        var dataSubtitle = subtitleCodecs.OrderByDescending(x => x.Value)
            .Select(x => new { Codec = x.Key, Count = x.Value })
            .ToList();

        var builder = new ReportBuilder(emailing);
        builder.StartRow(4);
        builder.AddPeriodSummaryBox(minDateUtc.Value, maxDateUtc.Value);
        builder.AddSummaryBox("Video Codecs", dataVideo.Count.ToString("N0"), ReportSummaryBox.IconType.Video, ReportSummaryBox.BoxColor.Info);
        builder.AddSummaryBox("Audio Codecs", dataAudio.Count.ToString("N0"), ReportSummaryBox.IconType.VolumeUp, ReportSummaryBox.BoxColor.Info);
        builder.AddSummaryBox("Subtitle Codecs", dataSubtitle.Count.ToString("N0"), ReportSummaryBox.IconType.ClosedCaptioning, ReportSummaryBox.BoxColor.Info);
        builder.EndRow();
        
        foreach (var codec in new[]
                 {
                     ("Video", dataVideo, ReportSummaryBox.IconType.Video),
                     ("Audio", dataAudio, ReportSummaryBox.IconType.VolumeUp),
                     ("Subtitle", dataSubtitle, ReportSummaryBox.IconType.ClosedCaptioning),
                 })
        {
            if (codec.Item2.Count < 2)
                continue;
            
            var fewest = codec.Item2.OrderBy(kv => kv.Count).First().Codec;
            var top = codec.Item2.OrderByDescending(kv => kv.Count).First().Codec;
            int averageCount = (int)Math.Round(codec.Item2.Average(x => x.Count));
        
            builder.StartRow(3);
            //builder.AppendLine(ReportSummaryBox.Generate(codec.Item1 + " Codecs", codec.Item2.Count.ToString("N0"), codec.Item3, ReportSummaryBox.BoxColor.Info));
            builder.AddSummaryBox($"Top {codec.Item1} Codec", top, ReportSummaryBox.IconType.ArrowAltCircleUp,
                ReportSummaryBox.BoxColor.Success);
            builder.AddSummaryBox($"Least {codec.Item1} Codec", fewest, ReportSummaryBox.IconType.ArrowAltCircleDown,
                ReportSummaryBox.BoxColor.Warning);
            builder.AddSummaryBox("Average Times", averageCount.ToString("N0"), ReportSummaryBox.IconType.BalanceScale,
                ReportSummaryBox.BoxColor.Error);
            builder.EndRow();
            
            builder.StartChartTableRow();
            
            builder.AddRowItem(TreeMap.Generate(new()
            {
                Data = codec.Item2.ToDictionary(x => x.Codec, x => x.Count),
                Title = "Codecs"
            }, emailing: emailing));
            builder.AddRowItem(TableGenerator.GenerateMinimumTable("Top Codecs", ["Codec", "Count"], codec.Item2
                .OrderByDescending(x => x.Count).Select(x => new object[]
                {
                    x.Codec,
                    x.Count.ToString("N0")
                }).Take(TableGenerator.MIN_TABLE_ROWS).ToArray(), emailing: emailing));
            
            builder.EndRow();
        }
        
        return builder.ToString();

    }

    private class CodecData
    {
        public string Name { get; set; } = null!;
        public int Count { get; set; }
    }
}
