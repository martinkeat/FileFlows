namespace FileFlows.DataLayer.Helpers;

public interface IAuditValueConverter
{
    /// <summary>
    /// Converts to a string
    /// </summary>
    /// <param name="newValue">the new value</param>
    /// <param name="oldValue">the old value</param>
    /// <returns>the string diff</returns>
    string? Convert(object? newValue, object? oldValue);

    /// <summary>
    /// Gets if this converter can convert the type
    /// </summary>
    /// <param name="type">the type</param>
    /// <returns>true if it can convert, otherwise false</returns>
    static abstract bool CanConvert(Type type);
    
}