namespace FileFlows.ServerShared.Helpers;

/// <summary>
/// Small helper for Sqlite
/// </summary>
public class SqliteHelper
{
    /// <summary>
    /// Gets a sqlite connection string for a db file
    /// </summary>
    /// <param name="dbFile">the filename of the sqlite db file</param>
    /// <returns>a sqlite connection string</returns>
    public static string GetConnectionString(string? dbFile = null)
    {
        dbFile ??= "FileFlows.sqlite";
        if (PlatformHelper.IsArm)
            return $"Data Source={dbFile}";
        return $"Data Source={dbFile};Version=3;PRAGMA journal_mode=WAL;";
    }
}