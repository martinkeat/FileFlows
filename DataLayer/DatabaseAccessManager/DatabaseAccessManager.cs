using FileFlows.DataLayer.DatabaseConnectors;
using FileFlows.Plugin;

namespace FileFlows.DataLayer;

/// <summary>
/// Manages data access operations by serving as a proxy for database commands.
/// </summary>
public class DatabaseAccessManager
{
    /// <summary>
    /// The connector being used
    /// </summary>
    private IDatabaseConnector DbConnector;
    
    /// <summary>
    /// Gets or sets the singleton instance of the Database Access Manager
    /// </summary>
    public static DatabaseAccessManager Instance { get; set; } 
    
    /// <summary>
    /// Initializes a new instance of the DatabaseAccessManager class.
    /// </summary>
    /// <param name="logger">The logger used for logging</param>
    /// <param name="type">The type of database (e.g., SQLite, SQL Server).</param>
    /// <param name="connectionString">The connection string used to connect to the database.</param>
    public DatabaseAccessManager(ILogger logger, DatabaseType type, string connectionString)
    {
        DbConnector = LoadConnector(logger, type, connectionString);
        this.DbObjectManager = new (type, DbConnector);
        this.FileFlowsObjectManager = new(this.DbObjectManager);
    }

    /// <summary>
    /// Loads a database connector 
    /// </summary>
    /// <param name="logger">The logger to used for logging</param>
    /// <param name="type">The type of connector to load</param>
    /// <param name="connectionString">The connection string of the database</param>
    /// <returns>The initialized connector</returns>
    private IDatabaseConnector LoadConnector(ILogger logger, DatabaseType type, string connectionString)
    {
        switch (type)
        {
            case DatabaseType.MySql:
                return new FileFlows.DataLayer.DatabaseConnectors.MySqlConnector(logger, connectionString);
            case DatabaseType.SqlServer:
                return new SqlServerConnector(logger, connectionString);
            case DatabaseType.Postgres:
                return new PostgresConnector(logger, connectionString);
            default:
                return new SQLiteConnector(logger, connectionString);
        }
    }

    /// <summary>
    /// Gets the DbObject manager to manage the database operations for the DbObject table
    /// </summary>
    public DbObjectManager DbObjectManager { get; init; }

    /// <summary>
    /// Gets the FileFlowsObject manager to manage the database operations for the FileFlowsObjects that are saved in the DbObject table
    /// </summary>
    public FileFlowsObjectManager FileFlowsObjectManager { get; init; }
}