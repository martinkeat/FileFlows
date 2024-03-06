using System.Data.Common;
using System.Reflection;
using NPoco;

namespace FileFlows.DataLayer.Converters;

/// <summary>
/// UID Converter that converts GUIDs to string when saving to the database
/// this is used by the sqlite db to avoid writing GUIDs as blobs
/// </summary>
public class GuidNullableConverter:DefaultMapper
{
    /// <summary>
    /// One instance for better memory management
    /// </summary>
    public static readonly GuidNullableConverter Instance = new();
    
    /// <summary>
    /// Get the convert for converting an object to the object actually stored in the database
    /// </summary>
    /// <param name="destType">the type we are trying to convert to</param>
    /// <param name="sourceMemberInfo">the information about the type being converted</param>
    /// <returns>the converter function to use</returns>
    public override Func<object, object> GetToDbConverter(Type destType, MemberInfo sourceMemberInfo)
    {
        if (destType == typeof(Guid?))
            return x => x == null ? string.Empty : x.ToString();
        return base.GetToDbConverter(destType, sourceMemberInfo);
    }

    /// <summary>
    /// Get the convert for converting an command arguments when executing against a database
    /// </summary>
    /// <param name="sourceType">the type of the object being used</param>
    /// <param name="dbCommand">the command being executed</param>
    /// <returns>the converter function to use</returns>
    public override Func<object, object> GetParameterConverter(DbCommand dbCommand, Type sourceType)
    {
        if (sourceType == typeof(Guid?))
            return x => Guid.TryParse(x?.ToString() ?? string.Empty, out Guid guid) ? guid : null;
        return base.GetParameterConverter(dbCommand, sourceType);
    }
}
