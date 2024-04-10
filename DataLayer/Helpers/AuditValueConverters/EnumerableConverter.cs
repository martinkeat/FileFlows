using System.Collections;
using Type = System.Type;

namespace FileFlows.DataLayer.Helpers;

/// <summary>
/// Provides functionality to convert enumerable objects to a string representation highlighting additions and deletions.
/// </summary>
public class EnumerableConverter : IAuditValueConverter
{
    private readonly Type objectType;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnumerableConverter"/> class with the specified object type.
    /// </summary>
    /// <param name="objectType">The type of the object.</param>
    public EnumerableConverter(Type objectType)
    {
        this.objectType = objectType;
    }

    /// <summary>
    /// Determines whether the specified type can be converted by this converter.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns><c>true</c> if the type is an enumerable; otherwise, <c>false</c>.</returns>
    public static bool CanConvert(Type type) => typeof(IEnumerable).IsAssignableFrom(type);

    /// <inheritdoc/>
    public string? Convert(object newValue, object oldValue)
    {
        var oldList = GetValues(oldValue);
        var newList = GetValues(newValue);

        var additions = newList.Except(oldList, new ValueComparer()).Select(item => $"+ {item}").ToList();
        var deletions = oldList.Except(newList, new ValueComparer()).Select(item => $"- {item}").ToList();

        List<string> diff = new List<string>();
        diff.AddRange(additions);
        diff.AddRange(deletions);

        return string.Join("\n", diff).TrimEnd();
    }

    /// <summary>
    /// Retrieves the values from an enumerable object.
    /// </summary>
    /// <param name="obj">The object from which to retrieve values.</param>
    /// <returns>A list of values contained in the object.</returns>
    private List<object> GetValues(object obj)
    {
        if (obj == null)
            return new List<object>();

        try
        {
            List<object> values = new List<object>();
            IEnumerable enumerable = (IEnumerable)obj;
            foreach (object item in enumerable)
            {
                values.Add(item);
            }

            return values;
        }
        catch (Exception)
        {
            return new List<object>();
        }
    }
}

/// <summary>
/// Provides custom value comparison for objects.
/// </summary>
public class ValueComparer : IEqualityComparer<object>
{
    /// <inheritdoc/>
    public new bool Equals(object x, object y)
    {
        return x != null && y != null && x.Equals(y);
    }

    /// <inheritdoc/>
    public int GetHashCode(object obj)
    {
        return obj?.GetHashCode() ?? 0;
    }
}