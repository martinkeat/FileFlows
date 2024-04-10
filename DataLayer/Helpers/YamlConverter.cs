using System.Collections;
using System.Reflection;
using System.Text;
using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.DataLayer.Helpers;

/// <summary>
/// Provides methods for converting objects to YAML format.
/// </summary>
public class YamlConverter
{
    private readonly object ParentObject; 
    /// <summary>
    /// Constructs a new instance of the converter
    /// </summary>
    /// <param name="parentObject">the parent Object</param>
    /// <param name="indentLevel">The current indentation level.</param>
    public YamlConverter(object parentObject, int indentLevel = 0)
    {
        ParentObject = parentObject;
    }

    /// <summary>
    /// Does the actual conversion
    /// </summary>
    /// <param name="obj">The object to convert.</param>
    /// <param name="indentLevel">The current indentation level.</param>
    /// <returns>the converted string</returns>
    public string DoConvert(object obj, int indentLevel = 0)
    {
        if (obj == null)
            return string.Empty;
        if (IsPrimitive(obj.GetType()))
            return new string(' ', indentLevel * 4) + obj;
        StringBuilder yaml = new StringBuilder();
        ConvertObjectToYaml(obj, yaml, indentLevel);
        return yaml.ToString();
    }

    /// <summary>
    /// Converts an object to YAML format with indentation.
    /// </summary>
    /// <param name="obj">The object to convert.</param>
    /// <param name="yaml">The StringBuilder to append the YAML representation to.</param>
    /// <param name="indentLevel">The current indentation level.</param>
    private void ConvertObjectToYaml(object obj, StringBuilder yaml, int indentLevel)
    {
        if (obj == null)
            return;

        Type type = obj.GetType();

        if (obj is IDictionary<string, object> dict)
        {
            foreach (var item in dict)
            {
                object value = item.Value;
                if (value == null)
                    continue;
                
                if (IsPrimitive(value.GetType()))
                {
                    AppendIndentedLine($"{item.Key}: {value}", indentLevel, yaml);
                }
                else
                {
                    AppendIndentedLine($"{item.Key}:", indentLevel, yaml);
                    ConvertObjectToYaml(value, yaml, indentLevel + 1);
                }
            }
            
        }
        else if (IsEnumerable(type))
        {
            IEnumerable enumerable = (IEnumerable)obj;
            foreach (object item in enumerable)
            {
                ConvertObjectToYaml(item, yaml, indentLevel);
            }
        }
        else
        {
            if(obj is FlowConnection connection)
            {
                string inputName = connection.InputNode.ToString();
                if (ParentObject is List<FlowPart> parts)
                {
                    var other = parts.FirstOrDefault(x => x.Uid == connection.InputNode);
                    if (other != null)
                        inputName = FlowHelper.GetFlowPartName(other);
                }

                AppendIndentedLine($"Output {connection.Output}: {inputName}", indentLevel, yaml);
                return;
            }
            PropertyInfo[] properties = type.GetProperties();
            foreach (PropertyInfo property in properties)
            {
                if (property.CanWrite == false || property.CanRead == false || property.Name is "Capacity" or "Icon")
                    continue;
                object value;
                try
                {
                    value = property.GetValue(obj);
                }
                catch (Exception)
                {
                    continue;
                }

                if (value == null)
                    continue;
                
                if (IsPrimitive(property.PropertyType))
                {
                    AppendIndentedLine($"{property.Name}: {value}", indentLevel, yaml);
                }
                else
                {
                    AppendIndentedLine($"{property.Name}:", indentLevel, yaml);
                    ConvertObjectToYaml(value, yaml, indentLevel + 1);
                }
            }
        }
    }

    /// <summary>
    /// Appends a line to the StringBuilder with indentation.
    /// </summary>
    /// <param name="line">The line to append.</param>
    /// <param name="indentLevel">The current indentation level.</param>
    /// <param name="sb">The StringBuilder to append the line to.</param>
    private static void AppendIndentedLine(string line, int indentLevel, StringBuilder sb)
    {
        if (!string.IsNullOrWhiteSpace(line))
            sb.Append(' ', indentLevel * 4).AppendLine(line);
    }

    /// <summary>
    /// Determines if a type is a primitive or value type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is a primitive or value type, otherwise false.</returns>
    private static bool IsPrimitive(Type type)
    {
        return type.IsPrimitive || type.IsValueType || type == typeof(string) || type == typeof(decimal) ||
               type == typeof(DateTime);
    }

    /// <summary>
    /// Determines if a type implements IEnumerable interface.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type implements IEnumerable, otherwise false.</returns>
    private static bool IsEnumerable(Type type)
    {
        return typeof(IEnumerable).IsAssignableFrom(type);
    }
}