using System.Text;
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
                if (property.Name is nameof(FlowPart.xPos) or nameof(FlowPart.yPos))
                {
                    // just record the new value, no need to record the old for these basic changes
                    changedProperties.AppendLine($"    {property.Name}: {newYaml.Trim()}");
                }
                else if (property.Name == nameof(FlowPart.Model) && newValue is IDictionary<string, object> newModel &&
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
                        changedProperties.AppendLine($"  + {item.Key}: {yaml}");
                    }
                    foreach (var item in changes)
                    {
                        string yamlNew = converter1.DoConvert(item.Value);
                        string yamlOld = converter1.DoConvert(oldModel[item.Key]);
                        if (yamlNew?.EmptyAsNull() == yamlOld?.EmptyAsNull())
                            continue;
                        if (string.IsNullOrWhiteSpace(yamlOld) == false && string.IsNullOrWhiteSpace(yamlNew) == false)
                        {
                            changedProperties.AppendLine($"    {item.Key}");
                            changedProperties.AppendLine($"      - {yamlOld}");
                            changedProperties.AppendLine($"      + {yamlNew}");
                        }
                        else if(string.IsNullOrWhiteSpace(yamlOld))
                            changedProperties.AppendLine($"  - {item.Key}: {yamlOld}");
                        else if(string.IsNullOrWhiteSpace(yamlNew))
                            changedProperties.AppendLine($"  + {item.Key}: {yamlNew}");
                    }
                    foreach (var item in deletions)
                    {
                        string yaml = converter1.DoConvert(item.Value);
                        if (string.IsNullOrWhiteSpace(yaml))
                            continue;
                        changedProperties.AppendLine($"  - {item.Key}: {yaml}");
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
                        // the output connections have been converted to 'Output [num]: [Input]'
                        if (string.IsNullOrWhiteSpace(oldYaml) == false)
                        {
                            foreach (string output in oldYaml.Trim().Split('\n'))
                            {
                                if (string.IsNullOrWhiteSpace(output) == false)
                                    changedProperties.AppendLine("  - " + output.Trim());
                            }
                        }
                        if (string.IsNullOrWhiteSpace(newYaml) == false)
                        {
                            foreach (string output in newYaml.Trim().Split('\n'))
                            {
                                if (string.IsNullOrWhiteSpace(output) == false)
                                    changedProperties.AppendLine("  + " + output.Trim());
                            }
                        }
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(oldYaml) == false && string.IsNullOrWhiteSpace(newYaml) == false)
                        {
                            changedProperties.AppendLine($"    {property.Name}");
                            changedProperties.AppendLine($"      - {oldYaml.TrimEnd()}");
                            changedProperties.AppendLine($"      + {newYaml.TrimEnd()}");
                        }
                        else if (string.IsNullOrWhiteSpace(oldYaml) == false)
                            changedProperties.AppendLine($"  - {property.Name}: {oldYaml.TrimEnd()}");
                        else if (string.IsNullOrWhiteSpace(newYaml) == false)
                            changedProperties.AppendLine($"  + {property.Name}: {newYaml.TrimEnd()}");
                    }
                }
            }
        }

        return changedProperties.ToString();
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