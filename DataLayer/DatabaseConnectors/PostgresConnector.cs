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
        => "'" + date.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") + "'::timestamp";

    /// <inheritdoc />
    public string TimestampDiffSeconds(string start, string end, string asColumn)
        => $" EXTRACT(epoch FROM {end} - {start}) AS {asColumn} ";
    
    /// <inheritdoc />
    public string DayOfWeek(string column, string asColumn = null)
        => $"EXTRACT(ISODOW FROM {WrapFieldName(column)})" + (string.IsNullOrEmpty(asColumn) ? string.Empty : $" as {WrapFieldName(asColumn)}");
    
    /// <inheritdoc />
    public string Hour(string column, string asColumn = null)
        => $"EXTRACT(HOUR FROM {WrapFieldName(column)})" + (string.IsNullOrEmpty(asColumn) ? string.Empty : $" as {WrapFieldName(asColumn)}");

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
    public string WrapFieldName(string name) => "\"" + name + "\"";

    /// <inheritdoc />
    public async Task<bool> ColumnExists(string table, string column)
    {
        using var db = await GetDb(false);
        var result = db.Db.ExecuteScalar<int>(@"
        SELECT COUNT(*)
        FROM information_schema.columns
        WHERE
            table_name = @0
        AND column_name = @1", table, column);
        return result > 0;
    }

    /// <inheritdoc />
    public async Task CreateColumn(string table, string column, string type, string defaultValue)
    {
        string sql = $@"ALTER TABLE {WrapFieldName(table)} ADD COLUMN {WrapFieldName(column)} {type}" + (string.IsNullOrWhiteSpace(defaultValue) ? "" : $" DEFAULT {defaultValue}");
        using var db = await GetDb(false);
        await db.Db.ExecuteAsync(sql);
    }
}