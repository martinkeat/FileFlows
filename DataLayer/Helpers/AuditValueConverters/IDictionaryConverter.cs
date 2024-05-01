using System.Dynamic;
using System.Text.Json;
using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.DataLayer.Helpers;

/// <summary>
/// IDictionaryconverter
/// </summary>
public class IDictionaryConverter : IAuditValueConverter
{
    private object? newSource;
    private object? oldSource;
    /// <summary>
    /// Constructs a new instance of the flow parts converter
    /// </summary>
    /// <param name="newSource">the overall source new object</param>
    /// <param name="oldSource">the overall source old object</param>
    public IDictionaryConverter(object? newSource, object? oldSource)
    {
        this.newSource = newSource;
        this.oldSource = oldSource;
    }
    
    /// <inheritdoc />
    public static bool CanConvert(Type type)
        => type == typeof(ExpandoObject);
    
    /// <inheritdoc />
    public string? Convert(object? newValue, object? oldValue)
    {
        var diff = GetDifferences(newValue, oldValue, newSource, oldSource);
        return diff?.Any() != true ? null : string.Join("\n", diff).TrimEnd(); 
    }
    
    
    /// <summary>
    /// Gets the differences from one value to another
    /// </summary>
    /// <param name="newValue">the new value</param>
    /// <param name="oldValue">the old value</param>
    /// <param name="newSource">the new source object</param>
    /// <param name="oldSource">the old source object</param>
    /// <returns>the differences</returns>
    public static List<string>? GetDifferences(object? newValue, object? oldValue, object? newSource, object? oldSource)
    {
        IDictionary<string, object>? newDict = newValue as IDictionary<string, object>;
        IDictionary<string, object>? oldDict = oldValue as IDictionary<string, object>;
        if (newDict?.Any() != true && oldDict?.Any() != true)
            return null;

        var additions = newDict?.Where(x => oldDict?.ContainsKey(x.Key) != true).ToDictionary(x => x.Key, x=> x.Value) ?? new ();
        var changes = newDict?.Where(x =>
        {
            if (oldDict?.TryGetValue(x.Key, out var odValue) != true)
                return false; // hasnt chagned was added
            if (x.Value == odValue)
                return false; // hasnt changed
            if (x.Value == null || odValue == null)
                return true;
            if (x.Value.Equals(odValue))
                return false; // same value
            string jsonOld = JsonSerializer.Serialize(odValue);
            string jsonNew = JsonSerializer.Serialize(x.Value);
            return jsonOld != jsonNew;
        })?.ToList() ?? new ();
        var deletions = oldDict?.Where(x => newDict?.ContainsKey(x.Key) != true).ToDictionary(x => x.Key, x=> x.Value) ?? new ();
        
        List<string> diff = new();
        
        foreach (var item in additions)
            diff.Add($"{item.Key}: {item.Value}");
        foreach (var item in deletions)
            diff.Add($"{item.Key}: Removed");
        foreach (var item in changes)
        {
            object? oldItem = null;
            if(oldDict?.TryGetValue(item.Key, out oldItem) == false)
                continue; // shouldn't happen
            if (item.Value == null && oldItem == null)
                continue; // shouldn't happen
            
            var converter = AuditValueHelper.GetConverter((item.Value ?? oldItem)!.GetType(), newSource, oldSource);
            var partDiff = converter.Convert(item.Value, oldItem);
            if (string.IsNullOrWhiteSpace(partDiff))
                continue;

            var lines = partDiff.Split("\n");
            if(lines.Length == 1)
                diff.Add($"{item.Key}: {lines[0]}");
            else
            {
                diff.Add($"{item.Key}:");
                foreach (var line in lines)
                {
                    diff.Add(new string(' ', AuditValueHelper.INDENT_SPACES) + line);
                }
            }
        }
        return diff;
    }
}