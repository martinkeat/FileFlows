using System.Reflection;

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
}