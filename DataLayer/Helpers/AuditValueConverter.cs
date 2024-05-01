using System.Reflection;
using FileFlows.Shared.Attributes;
using NPoco;

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
    /// <param name="propertyInfo">Optional property info</param>
    /// <returns>the converter if available</returns>
    public static IAuditValueConverter GetConverter(Type t, object? newSource, object? oldSource, PropertyInfo? propertyInfo = null)
    {
        if (propertyInfo?.GetCustomAttribute<EncryptedAttribute>() != null)
            return new EncryptedValueConverter();
        if (PrimitiveConverter.CanConvert(t))
            return new PrimitiveConverter(t);
        if (ObjectReferenceConverter.CanConvert(t))
            return new ObjectReferenceConverter();
        if (OutputConnectionConverter.CanConvert(t))
            return new OutputConnectionConverter(newSource, oldSource);
        if (FlowPartsConverter.CanConvert(t))
            return new FlowPartsConverter(newSource, oldSource);
        if (FlowFieldsConverter.CanConvert(t))
            return new FlowFieldsConverter(newSource, oldSource);
        if (IDictionaryConverter.CanConvert(t) || newSource is IDictionary<string, object> || oldSource is IDictionary<string, object>)
            return new IDictionaryConverter(newSource, oldSource);
        if (EnumerableConverter.CanConvert(t))
            return new EnumerableConverter();
        return new GenericObjectConverter(t, newSource, oldSource);
    }
    
    /// <summary>
    /// Audit the changes in an object
    /// </summary>
    /// <param name="type">the type of object being audited</param>
    /// <param name="newValue">the new value</param>
    /// <param name="oldValue">the old value</param>
    /// <returns>any changes in the object</returns>
    public static Dictionary<string, string> Audit(Type type, object newValue, object? oldValue)
    {
        Dictionary<string, string>  changedProperties = new ();

        var properties = type.GetProperties();

        foreach (var property in properties)
        {
            if (ShouldAudit(property) == false)
                continue;
            try
            {
                var ov = oldValue == null ? null : property.GetValue(oldValue);
                var nv = newValue == null ? null : property.GetValue(newValue);

                var converter = GetConverter(property.PropertyType, newValue, oldValue, property);
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
    /// Checks if a property should be audited
    /// </summary>
    /// <param name="property">The property to check.</param>
    /// <returns>True if the property should be auditted.</returns>
    public static bool ShouldAudit(PropertyInfo property)
    {
        if (property.CanWrite == false || property.CanRead == false)
            return false;
        if (property.Name is "Capacity")
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
}