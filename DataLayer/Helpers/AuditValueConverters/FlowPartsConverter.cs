using System.Text.Json;
using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.DataLayer.Helpers;

/// <summary>
/// Flow Parts converter
/// </summary>
public class FlowPartsConverter : IAuditValueConverter
{
    private object? newSource;
    private object? oldSource;
    /// <summary>
    /// Constructs a new instance of the flow parts converter
    /// </summary>
    /// <param name="newSource">the overall source new object</param>
    /// <param name="oldSource">the overall source old object</param>
    public FlowPartsConverter(object? newSource, object? oldSource)
    {
        this.newSource = newSource;
        this.oldSource = oldSource;
    }
    
    /// <inheritdoc />
    public static bool CanConvert(Type type)
        => type == typeof(List<FlowPart>);
    
    /// <inheritdoc />
    public string? Convert(object? newValue, object? oldValue)
    {
        var newParts = newValue as List<FlowPart>;
        var oldParts = oldValue as List<FlowPart>;
        if (newParts?.Any() != true && oldParts?.Any() != true)
            return null;

        var additions = newParts?.Where(x => oldParts?.Any(y => y.Uid == x.Uid) != true)?.ToList() ?? new ();
        var changes = newParts?.Where(x =>
        {
            var oldConnection = oldParts?.FirstOrDefault(y => y.Uid == x.Uid);
            if (oldConnection == null)
                return false;
            var jsonOld = JsonSerializer.Serialize(oldConnection);
            var jsonNew = JsonSerializer.Serialize(x);
            return jsonOld != jsonNew;
        })?.ToList() ?? new ();
        var deletions = oldParts?.Where(x => newParts?.Any(y => y.Uid == x.Uid) != true)?.ToList() ??
                        new();
        
        List<string> diff = new();
        
        foreach (var part in additions)
            diff.Add($"'{FlowHelper.GetFlowPartName(part)}' added");
        foreach (var part in deletions)
            diff.Add($"'{FlowHelper.GetFlowPartName(part)}' deleted");
        foreach (var part in changes)
        {
            var oldPart = oldParts?.FirstOrDefault(x => x.Uid == part.Uid);
            if (oldPart == null) // shouldn't happen
                continue;
            var converter = AuditValueHelper.GetConverter(typeof(FlowPart), newSource, oldSource);
            var partDiff = converter.Convert(part, oldPart!);
            if (string.IsNullOrWhiteSpace(partDiff))
                continue;
            
            diff.Add("'" + FlowHelper.GetFlowPartName(part) + "' updated");
            foreach (var line in partDiff.Split("\n"))
            {
                diff.Add(new string(' ', AuditValueHelper.INDENT_SPACES) + line);
            }
        }

        return string.Join("\n", diff).TrimEnd(); 
    }
}