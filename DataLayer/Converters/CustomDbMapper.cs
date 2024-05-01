using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using FileFlows.Shared.Json;
using FileFlows.Shared.Models;

namespace FileFlows.DataLayer.Converters;

using NPoco;

/// <summary>
/// Custom DB Mapper
/// </summary>
class CustomDbMapper : FileFlowsMapper<CustomDbMapper>
{
    /// <summary>
    /// The JSON options
    /// </summary>
    public static readonly JsonSerializerOptions JsonOptions;

    /// <summary>
    /// Static constructor for Custom DB Mapper
    /// </summary>
    static CustomDbMapper()
    {
        JsonOptions = new JsonSerializerOptions();
        JsonOptions.Converters.Add(new TimeSpanConverter());
        JsonOptions.Converters.Add(new DataConverter());
    }
    
    /// <summary>
    /// Function to convert a database object type
    /// </summary>
    /// <param name="destType">the type of object to create</param>
    /// <param name="sourceType">the type of object to create a new object from</param>
    /// <returns>a function to do a conversion</returns>
    public override Func<object, object?> GetFromDbConverter(Type destType, Type sourceType)
    {
        if (Enable)
        {
            if (destType == typeof(Guid?) && sourceType == typeof(string))
                return (value) => string.IsNullOrEmpty(value as string) ? null : Guid.Parse((string)value);
            if (destType == typeof(Guid) && sourceType == typeof(string))
                return (value) => string.IsNullOrEmpty(value as string) ? Guid.Empty : Guid.Parse((string)value);
            if (destType == typeof(Dictionary<string, object>) && sourceType == typeof(string))
                return (value) =>
                {
                    if (string.IsNullOrEmpty(value as string))
                        return new Dictionary<string, object>();
                    var result = JsonSerializer.Deserialize<Dictionary<string, object>>((string)value, JsonOptions);
                    return result;
                };
            if (destType == typeof(List<ExecutedNode>) && sourceType == typeof(string))
                return (value) =>
                {
                    if (value is string strValue == false)
                        return new List<ExecutedNode>();
                    try
                    {
                        if (string.IsNullOrEmpty(strValue) || strValue == "null")
                            return new List<ExecutedNode>();
                        var result = JsonSerializer.Deserialize<List<ExecutedNode>>(strValue, JsonOptions);
                        return result;
                    }
                    catch (Exception)
                    {
                        // Logger.Instance.ELog("Error parsing ExecutedNodes: " + ex.Message + " , string value: " + strValue);
                        return new List<ExecutedNode>();
                    }
                };
        }

        return base.GetFromDbConverter(destType, sourceType);
    }
    
    public override Func<object, object> GetToDbConverter(Type destType, MemberInfo sourceMemberInfo)
    {
        if (Enable)
        {
            // this break postgres
            // if (sourceMemberInfo.GetMemberInfoType() == typeof(Guid))
            //     return (value) =>
            //     {
            //         if (value == null)
            //             return string.Empty;
            //         if (value is Guid guid && guid == Guid.Empty)
            //             return string.Empty;
            //         return value.ToString() ?? string.Empty;
            //     };
            if (sourceMemberInfo.GetMemberInfoType() == typeof(Guid?))
                return (value) =>
                {
                    if (value == null)
                        return string.Empty;
                    if (value is Guid guid && guid == Guid.Empty)
                        return string.Empty;

                    return value.ToString() ?? string.Empty;
                };
            if (sourceMemberInfo.GetMemberInfoType() == typeof(string))
                return (value) => value?.ToString() ?? string.Empty;
            if (sourceMemberInfo.GetMemberInfoType() == typeof(Dictionary<string, object>))
                return (value) =>
                {
                    if (value is Dictionary<string, object> dict == false || dict.Any() == false)
                        return string.Empty;
                    return JsonSerializer.Serialize(dict, JsonOptions);
                };
            if (sourceMemberInfo.GetMemberInfoType() == typeof(List<ExecutedNode>))
                return (value) =>
                {
                    if (value is List<ExecutedNode> list == false || list.Any() == false)
                        return string.Empty;
                    return JsonSerializer.Serialize(list, JsonOptions);
                };


            var options = new JsonSerializerOptions();
            options.Converters.Add(new TimeSpanConverter());
        }

        return base.GetToDbConverter(destType, sourceMemberInfo);
    }
}