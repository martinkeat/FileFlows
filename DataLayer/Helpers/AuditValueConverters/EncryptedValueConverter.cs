namespace FileFlows.DataLayer.Helpers;

/// <summary>
/// Primitive converter
/// </summary>
public class EncryptedValueConverter : IAuditValueConverter
{

    /// <inheritdoc />
    public static bool CanConvert(Type type)
    {
        return type == typeof(string);
    }
    
    /// <inheritdoc />
    public string? Convert(object? newValue, object? oldValue)
    {
        var strNew = newValue as string;
        var strOld = oldValue as string;
        if (strNew == null && strOld == null)
            return null;
        if (strNew == strOld)
            return null;
        if(strOld == null)
            return "Value set";
        if(strNew == null)
            return "Value cleared";
        
        return "Value updated";
    }
}