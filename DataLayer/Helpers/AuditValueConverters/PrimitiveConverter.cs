using FileFlows.Shared;

namespace FileFlows.DataLayer.Helpers;

/// <summary>
/// Primitive converter
/// </summary>
public class PrimitiveConverter : IAuditValueConverter
{
    /// <summary>
    /// the type it is converter
    /// </summary>
    private Type _type;
    
    /// <summary>
    /// Constructs a new instance of the converter
    /// </summary>
    /// <param name="type">the type it is converter</param>
    public PrimitiveConverter(Type type)
    {
        _type = type;
    }

    /// <inheritdoc />
    public static bool CanConvert(Type type)
    {
        return type.IsPrimitive || type.IsValueType || type == typeof(string) || type == typeof(decimal) ||
               type == typeof(DateTime);
    }
    
    /// <inheritdoc />
    public string? Convert(object? newValue, object? oldValue)
    {
        if (_type == typeof(string))
        {
            newValue = (newValue as string)?.EmptyAsNull();
            oldValue = (oldValue as string)?.EmptyAsNull();
        }
        if (newValue == null && oldValue == null)
            return null;
        if (newValue == null)
            return $"'{oldValue}' removed";
        if (oldValue == null)
            return $"'{newValue}' added";

        if (newValue.Equals(oldValue))
            return null;
        
        return $"'{oldValue}' to '{newValue}'";
    }
}