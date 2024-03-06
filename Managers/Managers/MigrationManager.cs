using FileFlows.Plugin;
using FileFlows.ServerShared.Models;

namespace FileFlows.Managers;

/// <summary>
/// Manager used to migrate one database to another
/// </summary>
public class MigrationManager
{
    /// <summary>
    /// Migrates a database
    /// </summary>
    /// <param name="logger">the logger to use</param>
    /// <param name="source">the source database information</param>
    /// <param name="destination">the destination database information</param>
    /// <returns>the result of the migration</returns>
    public static Result<bool> Migrate(ILogger logger, DatabaseInfo source, DatabaseInfo destination)
    {
        DbMigrator migrator = new(logger);
        return migrator.Migrate(source, destination);
    }
}