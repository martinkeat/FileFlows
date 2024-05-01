using System.Text.Json;
using FileFlows.Shared.Models;

namespace FileFlows.DataLayer.Helpers;

/// <summary>
/// Flow Fields converter
/// </summary>
public class FlowFieldsConverter : IAuditValueConverter
{
    private object? newSource;
    private object? oldSource;
    /// <summary>
    /// Constructs a new instance of the flow fields converter
    /// </summary>
    /// <param name="newSource">the overall source new object</param>
    /// <param name="oldSource">the overall source old object</param>
    public FlowFieldsConverter(object? newSource, object? oldSource)
    {
        this.newSource = newSource;
        this.oldSource = oldSource;
    }
    
    /// <inheritdoc />
    public static bool CanConvert(Type type)
        => type == typeof(List<FlowField>);
    
    /// <inheritdoc />
    public string? Convert(object? newValue, object? oldValue)
    {
        var newParts = newValue as List<FlowField> ?? new ();
        var oldParts = oldValue as List<FlowField> ?? new ();
        if (newParts.Any() != true && oldParts.Any() != true)
            return null;

        var additions = newParts?.Where(x => oldParts?.Any(y => y.Name == x.Name) != true)?.ToList() ?? new ();
        var changes = newParts?.Where(x =>
        {
            var oldConnection = oldParts?.FirstOrDefault(y => y.Name == x.Name);
            if (oldConnection == null)
                return false;
            var jsonOld = JsonSerializer.Serialize(oldConnection);
            var jsonNew = JsonSerializer.Serialize(x);
            return jsonOld != jsonNew;
        })?.ToList() ?? new ();
        var deletions = oldParts?.Where(x => newParts?.Any(y => y.Name == x.Name) != true)?.ToList() ??
                        new();
        
        List<string> diff = additions.Select(part => $"'{part.Name}' added").ToList();
        diff.AddRange(deletions.Select(part => $"'{part.Name}' deleted"));

        foreach (var part in changes)
        {
            var oldPart = oldParts!.FirstOrDefault(x => x.Name == part.Name);
            if (oldPart == null) // shouldn't happen
                continue;
            var converter = AuditValueHelper.GetConverter(typeof(FlowField), newSource, oldSource);
            var partDiff = converter.Convert(part, oldPart!);
            if (string.IsNullOrWhiteSpace(partDiff))
                continue;
            
            diff.Add($"'{part.Name}' updated");
            foreach (var line in partDiff.Split("\n"))
            {
                diff.Add(new string(' ', AuditValueHelper.INDENT_SPACES) + line);
            }
        }

        return string.Join("\n", diff).TrimEnd(); 
    }
}