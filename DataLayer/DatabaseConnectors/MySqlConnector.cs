using FileFlows.Plugin;
using MySqlConnectorFactory = MySqlConnector.MySqlConnectorFactory;

namespace FileFlows.DataLayer.DatabaseConnectors;

/// <summary>
/// Connector for MySQL/MariaDB
/// </summary>
public class MySqlConnector : IDatabaseConnector
{
    /// <summary>
    /// The connection string to the database
    /// </summary>
    private string ConnectionString { get; init; }
    /// <summary>
    /// Pool of database connections
    /// </summary>
    private DatabaseConnectionPool connectionPool;

    /// <summary>
    /// Logger used for logging
    /// </summary>
    private ILogger Logger;
    
    /// <summary>
    /// Initialises a MySQL Connector
    /// </summary>
    /// <param name="logger">the logger to use by this connector</param>
    /// <param name="connectionString">the connection string for this connector</param>
    public MySqlConnector(ILogger logger, string connectionString)
    {
        Logger = logger;
        ConnectionString = connectionString;
        connectionPool = new(CreateConnection, 20, connectionLifetime: new TimeSpan(0, 10, 0));
    }
    
    /// <summary>
    /// Create a new database connection
    /// </summary>
    /// <returns>the new connection</returns>
    private DatabaseConnection CreateConnection()
    {
        var db = new NPoco.Database(ConnectionString, null, MySqlConnectorFactory.Instance);
        return new DatabaseConnection(db, false);
    }

    /// <inheritdoc />
    public async Task<DatabaseConnection> GetDb(bool write)
    {
        return await connectionPool.AcquireConnectionAsync();
    }


    /// <inheritdoc />
    public string WrapFieldName(string name) => name;
}