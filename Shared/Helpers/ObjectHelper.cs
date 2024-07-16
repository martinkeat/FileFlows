using System.Reflection;
using System.Text.Json;

namespace FileFlows.Shared.Helpers;

using FileFlows.Plugin;

/// <summary>
/// Generic helper methods for objecst
/// </summary>
public class ObjectHelper
{
    
    /// <summary>
    /// Tests if two objects are logically the same
    /// </summary>
    /// <param name="a">The first object to test</param>
    /// <param name="b">The second object to test</param>
    /// <returns>true if the objects are logically the same</returns>
    public static bool ObjectsAreSame(object a, object b)
    {

        if (a == null && b == null)
            return true;

        if (a != null && b != null && a.Equals(b)) return true;

        if (a is ObjectReference objA && b is ObjectReference objB)
            return objA.Uid == objB.Uid;

        bool areEqual = System.Text.Json.JsonSerializer.Serialize(a) == System.Text.Json.JsonSerializer.Serialize(b);
        if (areEqual)
            return true;

        return false;
    }
    
    /// <summary>
    /// Compares two string arrays for equality, considering they may be null.
    /// </summary>
    /// <param name="array1">The first string array.</param>
    /// <param name="array2">The second string array.</param>
    /// <returns>True if the arrays are equal, otherwise false.</returns>
    public static bool AreEqual(string[] array1, string[] array2)
    {
        // Check if both are null
        if (array1 == null && array2 == null)
            return true;

        // Check if only one of them is null
        if (array1 == null || array2 == null)
            return false;

        // Compare lengths
        if (array1.Length != array2.Length)
            return false;

        // Compare elements
        return array1.SequenceEqual(array2);
    }
    
    /// <summary>
    /// Copies all properties from the source object to the destination object, excluding ignored properties.
    /// </summary>
    /// <typeparam name="T">The type of objects being copied.</typeparam>
    /// <param name="source">The source object.</param>
    /// <param name="destination">The destination object.</param>
    /// <param name="ignoredProperties">Names of properties to be ignored during copying.</param>
    public static void CopyProperties<T>(T source, T destination, params string[] ignoredProperties)
    {
        PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (PropertyInfo property in properties)
        {
            if (property.CanRead && property.CanWrite && Array.IndexOf(ignoredProperties, property.Name) == -1)
            {
                var value = property.GetValue(source);
                property.SetValue(destination, value);
            }
        }
    }
    
    
    /// <summary>
    /// Gets the length of the enumerable or array.
    /// </summary>
    /// <param name="obj">The object to check.</param>
    /// <returns>The length of the enumerable or array. Returns -1 if the object is not enumerable.</returns>
    public static int GetArrayLength(object obj)
    {
        if (obj is IEnumerable enumerable)
        {
            if (obj.GetType().IsArray)
            {
                // If it's an array, use Length property
                var array = obj as Array;
                return array?.Length ?? 0;
            }
            
            return enumerable.Cast<object>().Count();
        }
        if (obj is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
        {
            // If it's a JsonValueKind.Array, use GetArrayLength() method
            return jsonElement.GetArrayLength();
        }
        
        // If not enumerable, return 0
        return 0;
    }
}