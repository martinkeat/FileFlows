namespace FileFlows.DataLayer.Helpers;

/// <summary>
/// Primitive converter
/// </summary>
public class PrimitiveConverter : IAuditValueConverter
{

    /// <inheritdoc />
    public static bool CanConvert(Type type)
    {
        return type.IsPrimitive || type.IsValueType || type == typeof(string) || type == typeof(decimal) ||
               type == typeof(DateTime);
    }
    
    /// <inheritdoc />
    public string? Convert(object? newValue, object? oldValue)
    {
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