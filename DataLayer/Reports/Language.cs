using System.Text.Json;
using FileFlows.Plugin;
using FileFlows.Shared.Models;

namespace FileFlows.DataLayer.Reports;

/// <summary>
/// Report for Languages
/// </summary>
public class Language : Report
{
    /// <inheritdoc />
    public override Guid Uid => new Guid("1e7b9a5f-44d6-40e9-bb4b-b1b9e9677fac");
    /// <inheritdoc />
    public override string Name => "Language";
    /// <inheritdoc />
    public override string Description => "Shows the different languages processed through FileFlows.";
    /// <inheritdoc />
    public override string Icon => "fas fa-comments";
    
    /// <inheritdoc />
    public override bool PeriodSelection => true;
    
    /// <summary>
    /// Gets or sets the stream type
    /// </summary>
    public StreamType Type { get; set; }
    
    /// <summary>
    /// Gets or sets the direction
    /// </summary>
    public IODirection Direction { get; set; }
    
    /// <inheritdoc />
    public override ReportSelection LibrarySelection => ReportSelection.Any;

    /// <inheritdoc />
    public override async Task<Result<string>> Generate(Dictionary<string, object> model)
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

        Dictionary<string, int> languages = new();
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
                if (key.ToLowerInvariant().EndsWith(" language") == false)
                    continue;
                if (prefix != null && key.ToLowerInvariant().StartsWith(prefix) == false)
                    continue;
                var value = dict[key];
                if (value is JsonElement je && je.ValueKind == JsonValueKind.String)
                    value = je.GetString();
                if (value is string lang == false)
                    continue;
                lang = LanguageHelper.GetEnglishFor(lang);
                if (languages.TryAdd(lang, 1) == false)
                    languages[lang] += 1;
            }
        }

        if (languages.Count == 0)
            return string.Empty;
        
        var data = languages.OrderByDescending(x => x.Value)
            .Select(x => new { Language = x.Key, Count = x.Value })
            .ToList();

        var table = GenerateHtmlTable(data) ?? string.Empty;

        var chart = GenerateSvgPieChart(data.ToDictionary(x => x.Language, x=> x.Count)) ?? string.Empty;

        return table + chart;
    }
}