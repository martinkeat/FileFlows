namespace FileFlows.Server.Helpers;

/// <summary>
/// Class for helping with Version objects
/// </summary>
public class VersionHelper
{
    /// <summary>
    /// Formats a version string according to the "yy.MM.w.build" format.
    /// </summary>
    /// <param name="version">The version string to be formatted.</param>
    /// <returns>
    /// The formatted version string. If the input cannot be parsed as a version, the original version string is returned.
    /// </returns>
    public static string VersionDateString(string version)
    {
        if (string.IsNullOrEmpty(version))
            return version;
        
        if (Version.TryParse(version, out Version? _v) == false)
            return version;

        // Format version in "yy.MM.w.build"
        return $"{_v.Major:D2}.{_v.Minor:D2}.{_v.Build}.{_v.Revision}";
    }
}