using System.Data.Common;
using System.Reflection;
using NPoco;

namespace FileFlows.DataLayer.Converters;

/// <summary>
/// UID Converter that converts GUIDs to string when saving to the database
/// this is used by the sqlite db to avoid writing GUIDs as blobs
/// </summary>
public class GuidConverter:DefaultMapper
{
    /// <summary>
    /// Get the convert for converting an object to the object actually stored in the database
    /// </summary>
    /// <param name="destType">the type we are trying to convert to</param>
    /// <param name="sourceMemberInfo">the information about the type being converted</param>
    /// <returns>the converter function to use</returns>
    public override Func<object, object> GetToDbConverter(Type destType, MemberInfo sourceMemberInfo)
    {
        if (destType == typeof(Guid))
            return x => x.ToString();
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
        if (sourceType == typeof(Guid))
            return x => x.ToString();
        return base.GetParameterConverter(dbCommand, sourceType);
    }
}
