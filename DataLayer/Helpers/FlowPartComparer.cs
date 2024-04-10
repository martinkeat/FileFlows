using System.Text;
using System.Text.RegularExpressions;
using FileFlows.Shared;
using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;
using Mysqlx.Crud;

namespace FileFlows.DataLayer.Helpers;

/// <summary>
/// Provides methods for comparing lists of FlowPart objects and generating change summaries.
/// </summary>
public static class FlowPartComparer
{
    /// <summary>
    /// Compares two lists of FlowPart objects and generates a summary of changes.
    /// </summary>
    /// <param name="newList">The new list of FlowPart objects.</param>
    /// <param name="originalList">The original list of FlowPart objects.</param>
    /// <param name="changes">A string containing the summary of changes.</param>
    /// <returns>True if changes were found, otherwise false.</returns>
    public static bool Compare(List<FlowPart> newList, List<FlowPart>? originalList, out string changes)
    {
        changes = string.Empty;
        StringBuilder builder = new StringBuilder();

        if (originalList == null || originalList.Count == 0)
            return false; // No original, don't record the changes

        var newListDict = newList.ToDictionary(part => part.Uid);
        var originalListDict = originalList.ToDictionary(part => part.Uid);

        var additions = newList.Where(part => !originalListDict.ContainsKey(part.Uid)).Select(FlowHelper.GetFlowPartName)
            .ToList();
        var changesList = newList.Where(part =>
            originalListDict.ContainsKey(part.Uid) && AreFlowPartsEqual(newList, originalList, part, originalListDict[part.Uid]) == false).ToList();
        var deletions = originalList.Where(part => !newListDict.ContainsKey(part.Uid)).Select(FlowHelper.GetFlowPartName)
            .ToList();

        // Record additions
        foreach (var name in additions)
        {
            builder.AppendLine($"+ {name}");
        }

        // Record changes
        foreach (var part in changesList)
        {
            var originalPart = originalListDict[part.Uid];
            var changedProperties = GetChangedProperties(originalList, newList, part, originalPart);
            builder.AppendLine($"{FlowHelper.GetFlowPartName(part)}\n{changedProperties}");
        }

        // Record deletions
        foreach (var name in deletions)
        {
            builder.AppendLine($"- {name}");
        }

        changes = builder.ToString();
        return true;
    }

    /// <summary>
    /// Determines if two FlowPart objects are equal.
    /// </summary>
    /// <param name="part1">The first FlowPart object.</param>
    /// <param name="part2">The second FlowPart object.</param>
    /// <returns>True if the objects are equal, otherwise false.</returns>
    private static bool AreFlowPartsEqual(List<FlowPart> list1, List<FlowPart> list2, FlowPart part1, FlowPart part2)
    {
        var yaml1 = new YamlConverter(list1).DoConvert(part1);
        var yaml2 = new YamlConverter(list2).DoConvert(part2);
        return yaml1 == yaml2;
    }

    /// <summary>
    /// Gets the changed properties between two FlowPart objects.
    /// </summary>
    /// <param name="newPart">The new FlowPart object.</param>
    /// <param name="oldPart">The original FlowPart object.</param>
    /// <returns>A string containing the changed properties.</returns>
    private static string GetChangedProperties(List<FlowPart> list1, List<FlowPart> list2, FlowPart newPart, FlowPart oldPart)
    {
        StringBuilder changedProperties = new StringBuilder();

        var properties = typeof(FlowPart).GetProperties();
        YamlConverter converter1 = new YamlConverter(list1);
        YamlConverter converter2 = new YamlConverter(list2);

        foreach (var property in properties)
        {
            if (property.CanRead == false || property.CanWrite == false || property.Name is "Capacity" or "Icon")
                continue;
            
            var newValue = property.GetValue(newPart);
            var oldValue = property.GetValue(oldPart);
            
            if (ArePropertyValuesEqual(newValue, oldValue) == false)
            {
                string oldYaml = converter1.DoConvert(oldValue, 2).Replace("\r\n", "\n").TrimEnd();
                string newYaml = converter2.DoConvert(newValue, 2).Replace("\r\n", "\n").TrimEnd();
                if (oldYaml == newYaml || (string.IsNullOrWhiteSpace(oldYaml) && string.IsNullOrWhiteSpace(newYaml)))
                    continue;
                if (property.Name == nameof(FlowPart.Model) && newValue is IDictionary<string, object> newModel &&
                         oldValue is IDictionary<string, object> oldModel)
                {
                    var additions = newModel.Where(x => oldModel.ContainsKey(x.Key) == false)
                        .ToDictionary(x => x.Key, x => x.Value);
                    var changes = newModel.Where(x =>
                        oldModel.ContainsKey(x.Key) && ArePropertyValuesEqual(x.Value, oldModel[x.Key]) == false)
                        .ToDictionary(x => x.Key, x => x.Value);
                    var deletions = oldModel.Where(x => newModel.ContainsKey(x.Key) == false)
                        .ToDictionary(x => x.Key, x => x.Value);
                    foreach (var item in additions)
                    {
                        string yaml = converter1.DoConvert(item.Value);
                        if (string.IsNullOrWhiteSpace(yaml))
                            continue;
                        RecordValueChnage(changedProperties, item.Key, null, yaml);
                    }
                    foreach (var item in changes)
                    {
                        string yamlNew = converter1.DoConvert(item.Value);
                        string yamlOld = converter1.DoConvert(oldModel[item.Key]);
                        RecordValueChnage(changedProperties, item.Key, yamlOld, yamlNew);
                    }
                    foreach (var item in deletions)
                    {
                        string yaml = converter1.DoConvert(item.Value);
                        if (string.IsNullOrWhiteSpace(yaml))
                            continue;
                        RecordValueChnage(changedProperties, item.Key, yaml, null);
                    }
                }
                else
                {
                    if (oldYaml.IndexOf("\n", StringComparison.Ordinal) > 0)
                        oldYaml = "\n" + oldYaml;
                    else
                        oldYaml = oldYaml.Trim();
                    if (newYaml.IndexOf("\n", StringComparison.Ordinal) > 0)
                        newYaml = "\n" + newYaml;
                    else
                        newYaml = newYaml.Trim();
                    if (property.Name == nameof(FlowPart.OutputConnections))
                    {
                        var oldParts = oldYaml?.Split(':');
                        var newParts = newYaml?.Split(':');
                        if (oldParts?.Length != 2 && newParts?.Length != 2)
                            continue; // shouldn't happen
                        
                        string name = oldParts?.FirstOrDefault() ?? newParts?.FirstOrDefault() ?? string.Empty;
                        RecordValueChnage(changedProperties, name, oldParts?.LastOrDefault()?.Trim() ?? string.Empty, newParts?.LastOrDefault()?.Trim() ?? string.Empty);
                    }
                    else
                    {
                        RecordValueChnage(changedProperties, property.Name, oldYaml, newYaml);
                    }
                }
            }
        }

        return changedProperties.ToString();
    }

    private static void RecordValueChnage(StringBuilder changedProperties, string name, string oldValue, string newValue, int indent = 2)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;
        if (oldValue?.EmptyAsNull() == newValue?.EmptyAsNull())
            return;
        if (string.IsNullOrWhiteSpace(oldValue) == false && string.IsNullOrWhiteSpace(newValue) == false)
        {
            if (oldValue.IndexOf("\n", StringComparison.Ordinal) < 0 && newValue.IndexOf("\n", StringComparison.Ordinal) < 0)
            {
                // basic value change
                changedProperties.AppendLine(new string(' ', indent * 4) + $"{name}: '{oldValue}' to '{newValue}'");
                return;
            }
            changedProperties.AppendLine(new string(' ', indent * 4) + $"{name}");
            changedProperties.AppendLine(new string(' ', indent * 4) + $"  {oldValue.TrimEnd()}");
            changedProperties.AppendLine(new string(' ', indent * 4) + $"  {newValue.TrimEnd()}");
        }
        else if (string.IsNullOrWhiteSpace(oldValue) == false)
            changedProperties.AppendLine(new string(' ', indent * 4) + $"{name}: {oldValue.TrimEnd()}");
        else if (string.IsNullOrWhiteSpace(newValue) == false)
            changedProperties.AppendLine(new string(' ', indent * 4) + $"{name}: {newValue.TrimEnd()}");
        
    }

    /// <summary>
    /// Determines if two property values are equal.
    /// </summary>
    /// <param name="value1">The first property value.</param>
    /// <param name="value2">The second property value.</param>
    /// <returns>True if the values are equal, otherwise false.</returns>
    private static bool ArePropertyValuesEqual(object value1, object value2)
    {
        if (value1 == null && value2 == null)
            return true;
        if (value1 == null || value2 == null)
            return false;
        return value1.Equals(value2);
    }
}