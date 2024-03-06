using FileFlows.Shared.Models;

namespace FileFlows.ServerShared.Models;

/// <summary>
/// Information regarding a database connection
/// </summary>
public class DatabaseInfo
{
    /// <summary>
    /// The database type
    /// </summary>
    public DatabaseType Type { get; init; }
    /// <summary>
    /// The connection string
    /// </summary>
    public string ConnectionString { get; init; }
}