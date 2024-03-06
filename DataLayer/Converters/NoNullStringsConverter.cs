using System.Reflection;
using NPoco;

namespace FileFlows.DataLayer.Converters;

/// <summary>
/// Convert that replaces nulls with non null values
/// </summary>
public class NoNullsConverter:DefaultMapper
{
    /// <summary>
    /// One instance for better memory management
    /// </summary>
    public static readonly NoNullsConverter Instance = new();
    
    /// <summary>
    /// Get the convert for converting an object to the object actually stored in the database
    /// </summary>
    /// <param name="destType">the type we are trying to convert to</param>
    /// <param name="sourceMemberInfo">the information about the type being converted</param>
    /// <returns>the converter function to use</returns>
    public override Func<object, object> GetToDbConverter(Type destType, MemberInfo sourceMemberInfo)
    {
        if (destType == typeof(string))
            return x => x ?? string.Empty;
        if (destType == typeof(Dictionary<string, object>))
            return x => x ?? new ();
        return base.GetToDbConverter(destType, sourceMemberInfo);
    }
}