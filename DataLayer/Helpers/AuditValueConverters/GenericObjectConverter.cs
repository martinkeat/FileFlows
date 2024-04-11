using System.Text;
using System.Text.RegularExpressions;

namespace FileFlows.DataLayer.Helpers;

/// <summary>
/// Generic object converter
/// </summary>
public class GenericObjectConverter : IAuditValueConverter
{
    private readonly Type objectType;
    private object? newSource;
    private object? oldSource;
    
    /// <summary>
    /// Constructs a new instance of the generic object converter
    /// </summary>
    /// <param name="objectType">the object type this is converter</param>
    /// <param name="newSource">the overall source new object</param>
    /// <param name="oldSource">the overall source old object</param>
    public GenericObjectConverter(Type objectType, object? newSource, object? oldSource)
    {
        this.objectType = objectType;
        this.newSource = newSource;
        this.oldSource = oldSource;
    }
    
    /// <inheritdoc />
    public static bool CanConvert(Type type)
        => true;
    
    /// <inheritdoc />
    public string? Convert(object? newValue, object? oldValue)
    {
        StringBuilder  changedProperties = new ();

        var properties = objectType.GetProperties();

        foreach (var property in properties)
        {
            if (AuditValueHelper.ShouldAudit(property) == false)
                continue;
            try
            {
                var ov = oldValue == null ? null : property.GetValue(oldValue);
                var nv = newValue == null ? null : property.GetValue(newValue);

                var converter = AuditValueHelper.GetConverter(property.PropertyType, newSource, oldSource, property);
                var diff = converter.Convert(nv, ov);
                if (string.IsNullOrWhiteSpace(diff))
                    continue;
                
                var lines = diff.Split("\n");
                if(lines.Length == 1)
                    changedProperties.AppendLine($"{property.Name}: {lines[0]}");
                else if (property.Name == "Model")
                {
                    // special case, we just show the model properties directly, we dont indent them and put them under a "Model" heading
                    foreach (var line in lines)
                    {
                        changedProperties.AppendLine(line);
                    }
                }
                else
                {
                    changedProperties.AppendLine($"{property.Name}:");
                    foreach (var line in lines)
                    {
                        changedProperties.AppendLine(new string(' ', AuditValueHelper.INDENT_SPACES) + line);
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        return changedProperties.ToString();
    }
}