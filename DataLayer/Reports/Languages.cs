using System.Text.Json;
using FileFlows.DataLayer.Reports.Charts;
using FileFlows.DataLayer.Reports.Helpers;
using FileFlows.Plugin;
using FileFlows.Shared.Models;
using Humanizer;

namespace FileFlows.DataLayer.Reports;

/// <summary>
/// Report for Languages
/// </summary>
public class Languages : Report
{
    /// <inheritdoc />
    public override Guid Uid => new Guid("1e7b9a5f-44d6-40e9-bb4b-b1b9e9677fac");
    /// <inheritdoc />
    public override string Name => "Languages";
    /// <inheritdoc />
    public override string Description => "Shows the different languages processed through FileFlows.";
    /// <inheritdoc />
    public override string Icon => "fas fa-comments";
    
    /// <inheritdoc />
    public override ReportSelection LibrarySelection => ReportSelection.Any;
    
    /// <inheritdoc />
    public override bool Direction => true;

    /// <inheritdoc />
    public override async Task<Result<string>> Generate(Dictionary<string, object> model, bool emailing)
    {
        var direction = GetDirection(model); 

        using var db = await GetDb();
        string sql =
            $"select {Wrap(direction == ReportDirection.Input ? "OriginalMetadata" : "FinalMetadata")} as {Wrap("Metadata")} from {Wrap("LibraryFile")} where {Wrap("Status")} = 1";
        AddLibrariesToSql(model, ref sql);
        AddPeriodToSql(model, ref sql);

        var metadataString = await db.Db.FetchAsync<string>(sql);
        var metadata = metadataString.Where(x => string.IsNullOrWhiteSpace(x) == false)
            .Select(x => JsonSerializer.Deserialize<Dictionary<string, object>>(x, DbLibraryFileManager.JsonOptions)!)
            .ToList();

        (DateTime? minDateUtc, DateTime? maxDateUtc) = GetPeriod(model);
        
        Dictionary<string, int> dataVideo = new();
        Dictionary<string, int> dataAudio = new();
        Dictionary<string, int> dataSubtitle = new();
        foreach (var dict in metadata)
        {
            foreach (var key in dict.Keys)
            {
                if (key.ToLowerInvariant().EndsWith(" language") == false)
                    continue;
                var value = dict[key];
                if (value is JsonElement je && je.ValueKind == JsonValueKind.String)
                    value = je.GetString();
                if (value is string lang == false)
                    continue;
                lang = LanguageHelper.GetEnglishFor(lang);

                if (key.ToLowerInvariant().StartsWith("video"))
                {
                    if (dataVideo.TryAdd(lang, 1) == false)
                        dataVideo[lang] += 1;
                }
                else if (key.ToLowerInvariant().StartsWith("audio"))
                {
                    if (dataAudio.TryAdd(lang, 1) == false)
                        dataAudio[lang] += 1;
                }
                else if (key.ToLowerInvariant().StartsWith("subtitle"))
                {
                    if (dataSubtitle.TryAdd(lang, 1) == false)
                        dataSubtitle[lang] += 1;
                }
            }
        }

        if (dataAudio.Count == 0 && dataSubtitle.Count == 0 && dataVideo.Count == 0)
            return string.Empty;

        ReportBuilder builder = new(emailing);
        builder.StartRow(dataVideo.Count > 0 ? 4 : 3);
        builder.AddPeriodSummaryBox(minDateUtc ?? DateTime.MinValue, maxDateUtc ?? DateTime.MaxValue);
        if(dataVideo.Count > 0)
            builder.AddSummaryBox("Video Languages", dataVideo.Count, ReportSummaryBox.IconType.Video, ReportSummaryBox.BoxColor.Info);
        builder.AddSummaryBox("Audio Languages", dataAudio.Count, ReportSummaryBox.IconType.VolumeUp, ReportSummaryBox.BoxColor.Info);
        builder.AddSummaryBox("Subtitle Languages", dataSubtitle.Count, ReportSummaryBox.IconType.ClosedCaptioning, ReportSummaryBox.BoxColor.Info);
        builder.EndRow();

        if (dataVideo.Count > 0)
        {
            builder.StartChartTableRow();
            builder.AddRowItem(TreeMap.Generate(new ()
            {
                Title = "Video Languages",
                Data = dataVideo,
            }, emailing));
            builder.AddRowItem(TableGenerator.GenerateMinimumTable("Top Video Languages", ["Language", "Count"], 
                dataVideo.OrderByDescending(x => x.Value).Select(x => new object[] { x.Key, x.Value })
                    .Take(TableGenerator.MIN_TABLE_ROWS).ToArray()
                , emailing: emailing));
            builder.EndRow();
        }
            
        if (dataAudio.Count > 0)
        {
            builder.StartChartTableRow();
            builder.AddRowItem(TreeMap.Generate(new ()
            {
                Title = "Audio Languages",
                Data = dataAudio,
            }, emailing));
            builder.AddRowItem(TableGenerator.GenerateMinimumTable("Top Audio Languages", ["Language", "Count"], 
                dataAudio.OrderByDescending(x => x.Value).Select(x => new object[] { x.Key, x.Value })
                    .Take(TableGenerator.MIN_TABLE_ROWS).ToArray()
                , emailing: emailing));
            builder.EndRow();
        }
        
        if (dataSubtitle.Count > 0)
        {
            builder.StartChartTableRow();
            builder.AddRowItem(TreeMap.Generate(new ()
            {
                Title = "Subtitle Languages",
                Data = dataSubtitle,
            }, emailing));
            builder.AddRowItem(TableGenerator.GenerateMinimumTable("Top Subtitle Languages", ["Language", "Count"], 
                dataSubtitle.OrderByDescending(x => x.Value).Select(x => new object[] { x.Key, x.Value })
                    .Take(TableGenerator.MIN_TABLE_ROWS).ToArray()
                , emailing: emailing));
            builder.EndRow();
        }

        return builder.ToString();
        //
        // var data = languages.OrderByDescending(x => x.Value)
        //     .Select(x => new { Language = x.Key, Count = x.Value })
        //     .ToList();
        //
        // var table = TableGenerator.Generate(data) ?? string.Empty;
        //
        // var chart = PieChart.Generate(new PieChartData
        // {
        //     Data = data.ToDictionary(x => x.Language, x => x.Count)
        // }, emailing) ?? string.Empty;
        //
        // return table + chart;
    }

    /// <summary>
    /// Language Data
    /// </summary>
    class LanguageData
    {
        /// <summary>
        /// Gets or sets the language
        /// </summary>
        public string Name { get; set; } = null!;
        /// <summary>
        /// Gets or sets the number of times this language appears
        /// </summary>
        public int Count { get; set; }
    }
}