using System.Text.RegularExpressions;
using FileFlows.DataLayer.Converters;
using FileFlows.Plugin;
using Microsoft.Data.SqlClient;
using NPoco;
using DatabaseType = FileFlows.Shared.Models.DatabaseType;
using MySqlConnectorFactory = MySqlConnector.MySqlConnectorFactory;

namespace FileFlows.DataLayer.DatabaseConnectors;

/// <summary>
/// Connector for SQL Server
/// </summary>
public class SqlServerConnector : IDatabaseConnector
{
    /// <inheritdoc />
    public DatabaseType Type => DatabaseType.SqlServer;
    
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
        => "'" + date.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") + "'";
    
    /// <summary>
    /// Initialises a SQL Server Connector
    /// </summary>
    /// <param name="logger">the logger to use by this connector</param>
    /// <param name="connectionString">the connection string for this connector</param>
    public SqlServerConnector(ILogger logger, string connectionString)
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
        var db = new Database(ConnectionString, null, SqlClientFactory.Instance);
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
        //var db = new NPoco.Database(ConnectionString, null, SqlClientFactory.Instance);
        //return Task.FromResult(new DatabaseConnection(db, true));
        
        return await connectionPool.AcquireConnectionAsync();
    }


    /// <inheritdoc />
    public string WrapFieldName(string name) => name;

}