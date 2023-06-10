using System;

namespace FileFlowTests;

/// <summary>
/// Extensions for the random class
/// </summary>
public static class RandomExtensions
{
    /// <summary>
    /// Gets a random enum value
    /// </summary>
    /// <param name="random">the random instance</param>
    /// <typeparam name="T">the type of enum</typeparam>
    /// <returns>the next random enum value</returns>
    /// <exception cref="InvalidOperationException">if the type is not an enum</exception>
    public static T NextEnum<T>(this System.Random random)
        where T : struct
    {
        Type type = typeof(T);
        if (type.IsEnum == false) throw new InvalidOperationException();

        var array = Enum.GetValues(type);
        var index = random.Next(array.GetLowerBound(0), array.GetUpperBound(0) + 1);
        return (T)array.GetValue(index)!;
    }
}