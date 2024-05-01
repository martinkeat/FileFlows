using System.Collections;
using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;
using NPoco;

namespace FileFlows.DataLayer.Converters;

/// <summary>
/// JSON Column serializer
/// </summary>
public class JsonColumnSerializer: IColumnSerializer
{
    public string Serialize(object value)
    {
        if (value == null) 
            return string.Empty;
        if (value is IList list && list.Count == 0)
            return string.Empty;
        if (value is IDictionary dict && dict.Count == 0)
            return string.Empty;
        return System.Text.Json.JsonSerializer.Serialize(value, CustomDbMapper.JsonOptions);
    }

    public object? Deserialize(string value, Type targetType)
    {
        if (value == null)
        {
            if (targetType == typeof(List<ExecutedNode>))
                return new List<ExecutedNode>();
            if (targetType == typeof(Dictionary<string,object>))
                return new Dictionary<string,object>();
            return null;
        }
        return System.Text.Json.JsonSerializer.Deserialize(value, targetType, CustomDbMapper.JsonOptions);
    }
}