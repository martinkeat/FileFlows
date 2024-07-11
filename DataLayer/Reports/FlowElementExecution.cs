using System.Text.Json;
using FileFlows.DataLayer.Reports.Helpers;
using FileFlows.Plugin;
using FileFlows.Shared.Models;

namespace FileFlows.DataLayer.Reports;

/// <summary>
/// Report that shows the flow elements that have been executed
/// </summary>
public class FlowElementExecution : Report
{
    /// <inheritdoc />
    public override Guid Uid => new Guid("9eca7456-c974-4fec-a754-c584bc823fae");
    /// <inheritdoc />
    public override string Name => "Flow Element Execution";
    /// <inheritdoc />
    public override string Description => "Shows the flow elements that have been executed in the system.";
    /// <inheritdoc />
    public override string Icon => "fas fa-sitemap";
    /// <inheritdoc />
    public override ReportSelection LibrarySelection => ReportSelection.Any;

    /// <inheritdoc />
    public override async Task<Result<string>> Generate(Dictionary<string, object> model, bool emailing)
    {
        using var db = await GetDb();
        string sql = $"select {Wrap("ExecutedNodes")} from {Wrap("LibraryFile")} where {Wrap("Status")} = 1"; 
        AddLibrariesToSql(model, ref sql);
        AddPeriodToSql(model, ref sql);
        
        var nodesString = await db.Db.FetchAsync<string>(sql);
        var nodes = nodesString.Where(x => string.IsNullOrWhiteSpace(x) == false)
            .SelectMany(x => JsonSerializer.Deserialize<List<ExecutedNode>>(x, DbLibraryFileManager.JsonOptions)!)
            .ToList();
        
        var nodeCounts = nodes
            .GroupBy(node => node.NodeName)
            .Select(group => new { NodeName = group.Key, Count = group.Count() })
            .OrderByDescending(x => x.Count)
            .Select(x => new { Name = x.NodeName, x.Count});

        var builder = new ReportBuilder(emailing);
        
        builder.StartLargeTableRow();
        builder.AddRowItem(TableGenerator.Generate(nodeCounts, emailing: emailing));
        builder.EndRow();
        
        return builder.ToString();
    }
}