namespace FileFlows.Shared.Helpers;

/// <summary>
/// Helper to get a unique name
/// </summary>
public class UniqueNameHelper
{
    /// <summary>
    /// Gets a unique name
    /// </summary>
    /// <param name="name">the name to make unique</param>
    /// <param name="names">the existing names</param>
    /// <returns>a unique name</returns>
    /// <exception cref="Exception">exception if no unique name could be made</exception>
    public static string GetUnique(string name, List<string> names)
    {
        names = names?.Select(x => x.ToLower())?.ToList() ?? new();
        string newName = name.Trim();
        int count = 2;
        while (names.Contains(newName.ToLower()))
        {
            newName = name + " (" + count + ")";
            ++count;
            if (count > 100)
                throw new Exception("Could not find unique name, aborting.");
        }
        return newName;
    }
}