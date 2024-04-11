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
        string strNew = newValue as string;
        string strOld = oldValue as string;
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