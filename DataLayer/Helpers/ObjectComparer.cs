using System.Collections;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Text.Json;
using FileFlows.Plugin;
using FileFlows.Shared.Attributes;
using FileFlows.Shared.Models;
using NPoco;

namespace FileFlows.DataLayer.Helpers;

/// <summary>
/// Helper class for comparing objects and retrieving changes between them.
/// </summary>
public static class ObjectComparer
{
    /// <summary>
    /// Retrieves the changes between two objects of the same type.
    /// </summary>
    /// <typeparam name="T">The type of the objects to compare.</typeparam>
    /// <param name="newObj">The new object.</param>
    /// <param name="original">The original object.</param>
    /// <returns>A dictionary containing the changes between the two objects.</returns>
    public static Dictionary<string, object> GetChanges<T>(T newObj, T? original) where T : class
    {
        if (newObj == null)
        {
            throw new ArgumentException("newObj must be non-null");
        }

        Dictionary<string, object> changes = new ();

        var properties = typeof(T).GetProperties();
        foreach (var property in properties)
        {
            if (ShouldAudit(property) == false)
                continue;
            
            var newValue = property.GetValue(newObj);
            var originalValue = original != null ? property.GetValue(original) : GetDefaultValue(property);

            if (newValue is FlowProperties fp1)
            {
                var fpChanges = GetChanges<FlowProperties>(fp1, originalValue as FlowProperties);
                foreach (var key in fpChanges.Keys)
                    changes[property.Name + "." + key] = fpChanges[key];
                continue;
            }
            if (newValue is List<FlowPart> list)
            {
                if(FlowPartComparer.Compare(list, originalValue as List<FlowPart>, out string diff))
                    changes.Add(property.Name, diff.Trim());
                continue;
            }

            if (AreValuesEqual(newValue, originalValue) == false)
            {
                if (newValue is ObjectReference oRef)
                    newValue = oRef.Name;
                changes.Add(property.Name, newValue ?? string.Empty);
            }
        }
        
        return changes;
    }

    /// <summary>
    /// Checks if a property should be audited
    /// </summary>
    /// <param name="property">The property to check.</param>
    /// <returns>True if the property should be auditted.</returns>
    private static bool ShouldAudit(PropertyInfo property)
    {
        if (property.CanWrite == false)
            return false;
        if (property.PropertyType == typeof(DateTime))
            return false;
        if (property.PropertyType == typeof(TimeSpan))
            return false;
        if(property.GetCustomAttribute<DbIgnoreAttribute>() != null)
            return false;
        if (property.GetCustomAttribute<IgnoreAttribute>() != null)
            return false;
        if (property.GetCustomAttribute<DontAuditAttribute>() != null)
            return false;
        return true;
    }
    /// <summary>
    /// Compares two object values considering null values, reference equality, and equality for different types.
    /// </summary>
    /// <param name="value1">The first value to compare.</param>
    /// <param name="value2">The second value to compare.</param>
    /// <returns>True if the values are equal, otherwise false.</returns>
    private static bool AreValuesEqual(object? value1, object? value2)
    {
        if (value1 == null && value2 == null)
            return true;

        if (value1 == null || value2 == null)
            return false;

        if (value1 is DateTime or TimeSpan)
            return true; // we dont audit dates/times

        // If the value is a collection, compare its count
        if (value1 is ICollection collection1 && value2 is ICollection collection2)
            return collection1.Count == collection2.Count;

        if (value1 is ObjectReference or1 && value2 is ObjectReference or2)
            return or1.Uid == or2.Uid;

        // Compare values for different types (e.g., string, int, etc.)
        return value1.Equals(value2);
    }

    /// <summary>
    /// Gets the default value for a given property.
    /// </summary>
    /// <param name="property">The property to get the default value for.</param>
    /// <returns>The default value for the specified property.</returns>
    private static object? GetDefaultValue(PropertyInfo property)
    {
        var defaultAttribute = property.GetCustomAttribute<DefaultValueAttribute>();
        if (defaultAttribute != null)
        {
            return defaultAttribute.Value;
        }

        return property.PropertyType.IsValueType ? Activator.CreateInstance(property.PropertyType) : null;
    }
}
