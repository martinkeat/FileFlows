namespace FileFlows.DataLayer.Helpers;

/// <summary>
/// Extension methods
/// </summary>
public static class ExtensionMethods
{
    /// <summary>
    /// Escapes a string so it is safe to be used in a sql command
    /// </summary>
    /// <param name="input">the string to escape</param>
    /// <returns>the escaped string</returns>
    public static string SqlEscape(this string input) =>
        input == null ? string.Empty : "'" + input.Replace("'", "''") + "'";
}