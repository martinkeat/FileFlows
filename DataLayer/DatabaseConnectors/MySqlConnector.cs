using FileFlows.DataLayer.Converters;
using FileFlows.Plugin;
using FileFlows.Shared.Models;
using MySqlConnectorFactory = MySqlConnector.MySqlConnectorFactory;

namespace FileFlows.DataLayer.DatabaseConnectors;

/// <summary>
/// Connector for MySQL/MariaDB
/// </summary>
public class MySqlConnector : IDatabaseConnector
{
    /// <inheritdoc />
    public DatabaseType Type => DatabaseType.MySql;

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

    /// <inheritdoc />
    public string FormatDateQuoted(DateTime date)
        => "'" + date.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'";

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
        db.Mappers = new()
        {
            GuidNullableConverter.UseInstance(),
            NoNullsConverter.UseInstance(),
            CustomDbMapper.UseInstance()
        };
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