using FileFlows.Plugin;

namespace FileFlows.DataLayer.Helpers;

/// <summary>
/// Object reference converter
/// </summary>
public class ObjectReferenceConverter : IAuditValueConverter
{

    /// <inheritdoc />
    public static bool CanConvert(Type type)
        => type == typeof(ObjectReference);
    
    /// <inheritdoc />
    public string? Convert(object? newValue, object? oldValue)
    {
        var orNew = newValue as ObjectReference;
        var orOld = oldValue as ObjectReference;
        
        if (orNew == null && orOld == null)
            return null;
        if (orNew == null)
            return $"'{orOld!.Name}' removed";
        if (orOld == null)
            return $"'{orNew.Name}' added";

        if (orNew.Uid == orOld.Uid)
            return null;
        
        return $"'{orOld.Name}' to '{orNew.Name}'";
    }
}