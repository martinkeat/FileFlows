using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using FileFlows.Shared.Models;

namespace FileFlows.DataLayer.Helpers;

/// <summary>
/// Converts a value that is being audited to a string
/// </summary>
public class AuditValueHelper
{
    public const int INDENT_SPACES = 4;

    /// <summary>
    /// Gets the converter if known
    /// </summary>
    /// <param name="t">the type</param>
    /// <param name="newSource">the overall source new object</param>
    /// <param name="oldSource">the overall source old object</param>
    /// <returns>the converter if available</returns>
    public static IAuditValueConverter GetConverter(Type t, object? newSource, object? oldSource)
    {
        if (PrimitiveConverter.CanConvert(t))
            return new PrimitiveConverter();
        if (OutputConnectionConverter.CanConvert(t))
            return new OutputConnectionConverter(newSource, oldSource);
        if (FlowPartsConverter.CanConvert(t))
            return new FlowPartsConverter(newSource, oldSource);
        if (IDictionaryConverter.CanConvert(t) || newSource is IDictionary<string, object> || oldSource is IDictionary<string, object>)
            return new IDictionaryConverter(newSource, oldSource);
        if (EnumerableConverter.CanConvert(t))
            return new EnumerableConverter(t);
        return new GenericObjectConverter(t, newSource, oldSource);
    }
    
    public static Dictionary<string, string> Convert(Type type, object newValue, object oldValue, int indent)
    {
        Dictionary<string, string>  changedProperties = new ();

        var properties = type.GetProperties();

        foreach (var property in properties)
        {
            if (property.CanRead == false || property.CanWrite == false || property.Name is "Capacity")
                continue;
            try
            {
                var ov = oldValue == null ? null : property.GetValue(oldValue);
                var nv = newValue == null ? null : property.GetValue(newValue);

                var converter = GetConverter(property.PropertyType, newValue, oldValue);
                var diff = converter.Convert(nv, ov);
                if (string.IsNullOrWhiteSpace(diff))
                    continue;
                changedProperties[property.Name] = diff;
            }
            catch (Exception)
            {
            }
        }

        return changedProperties;
    }
    /// <summary>
    /// Determines if a type implements IEnumerable interface.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type implements IEnumerable, otherwise false.</returns>
    private static bool IsEnumerable(Type type)
    {
        return typeof(IEnumerable).IsAssignableFrom(type);
    }
}