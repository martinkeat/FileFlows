using FileFlows.Plugin;
using FileFlows.ServerShared.Models;

namespace FileFlows.Managers.InitializationManagers;

/// <summary>
/// Manager used to migrate one database to another
/// </summary>
public class MigrationManager
{
    /// <summary>
    /// the logger to use
    /// </summary>
    private readonly ILogger Logger;
    /// <summary>
    /// the source database information
    /// </summary>
    private readonly DatabaseInfo Source;
    /// <summary>
    /// The destination database information
    /// </summary>
    private readonly DatabaseInfo Destination;
    
    /// <summary>
    /// Initializes a new instance of the migration manager
    /// </summary>
    /// <param name="logger">the logger to use</param>
    /// <param name="source">the source database information</param>
    /// <param name="destination">the destination database information</param>
    public MigrationManager(ILogger logger, DatabaseInfo source, DatabaseInfo destination)
    {
        Logger = logger;
        Source = source;
        Destination = destination;
    }
    
    /// <summary>
    /// Migrates a database
    /// </summary>
    /// <returns>the result of the migration</returns>
    public Result<bool> Migrate()
    {
        DbMigrator migrator = new(Logger);
        return migrator.Migrate(Source, Destination);
    }

    /// <summary>
    /// Checks if the destination database is an external database, and that it exists
    /// </summary>
    /// <returns>true if its an external database, and it exists</returns>
    public Result<bool> ExternalDatabaseExists()
    {
        if (Destination.Type == DatabaseType.Sqlite)
            return false;
        
        DbMigrator migrator = new(Logger);
        return migrator.DatabaseExists(Destination.Type, Destination.ConnectionString);
    }

    /// <summary>
    /// Tests a connection to a database
    /// </summary>
    /// <param name="type">the type of the database</param>
    /// <param name="connectionString">the connection string to the database</param>
    /// <returns>true if successfully connected, otherwise false</returns>
    public static Result<bool> CanConnect(DatabaseType type, string connectionString)
        => DatabaseAccessManager.CanConnect(type, connectionString);
}