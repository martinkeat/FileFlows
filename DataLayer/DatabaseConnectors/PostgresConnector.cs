using FileFlows.DataLayer.Converters;
using FileFlows.Plugin;
using NPoco;
using DatabaseType = FileFlows.Shared.Models.DatabaseType;

namespace FileFlows.DataLayer.DatabaseConnectors;

/// <summary>
/// Connector for Postgres
/// </summary>
public class PostgresConnector : IDatabaseConnector
{
    /// <inheritdoc />
    public DatabaseType Type => DatabaseType.Postgres;
    
    /// <summary>
    /// The connection string to the database
    /// </summary>
    /// 
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
    /// Instance of CustomDbMapper for this server connection
    /// </summary>
    private readonly CustomDbMapper CustomDbMapperInstance;

    /// <inheritdoc />
    public string FormatDateQuoted(DateTime date)
        => "'" + date.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ") + "'::timestamp";

    /// <summary>
    /// Initialises a Postgres Connector
    /// </summary>
    /// <param name="logger">the logger to use by this connector</param>
    /// <param name="connectionString">the connection string for this connector</param>
    public PostgresConnector(ILogger logger, string connectionString)
    {
        Logger = logger;
        ConnectionString = connectionString;
        CustomDbMapperInstance = new ();
        connectionPool = new(CreateConnection, 20, connectionLifetime: new TimeSpan(0, 10, 0));
    }

    /// <summary>
    /// Create a new database connection
    /// </summary>
    /// <returns>the new connection</returns>
    private DatabaseConnection CreateConnection()
    {
        var db = new Database(ConnectionString, null,  Npgsql.NpgsqlFactory.Instance);
        db.Mappers.Add(Converters.GuidNullableConverter.Instance);
        db.Mappers.Add(Converters.NoNullsConverter.Instance);;
        db.Mappers.Add(Converters.CustomDbMapper.Instance);
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
    public string WrapFieldName(string name) => "\"" + name + "\"";
}