using System.Text.Json.Serialization;
using FileFlows.Shared.Json;

namespace FileFlows.Shared.Models;


/// <summary>
/// Value used for statistical date
/// </summary>
/// <typeparam name="T">The type of value</typeparam>
public class SystemValue<T>
{
    /// <summary>
    /// Gets or sets the time of the value
    /// </summary>
    [JsonPropertyName("x")]
    [JsonConverter(typeof(DateTimeStringConverter))]
    public DateTime Time { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets or sets the actual value
    /// </summary>
    [JsonPropertyName("y")]
    public T Value { get; set; }
}