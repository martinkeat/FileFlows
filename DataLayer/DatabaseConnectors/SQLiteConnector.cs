using System.Text.RegularExpressions;
using FileFlows.Plugin;
using FileFlows.ServerShared.Helpers;
using FileFlows.Shared;
using NPoco.DatabaseTypes;
using DatabaseType = FileFlows.Shared.Models.DatabaseType;

namespace FileFlows.DataLayer.DatabaseConnectors;

/// <summary>
/// Connector for SQLite
/// </summary>
public class SQLiteConnector : IDatabaseConnector
{
    private DatabaseConnection dbConnectionWrite;
    private FairSemaphore writeSemaphore = new(1);
    
    /// <summary>
    /// Logger used for logging
    /// </summary>
    private ILogger Logger;

    /// <inheritdoc />
    public string FormatDateQuoted(DateTime date)
        => "datetime('" + date.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") + "', 'utc')"; 
    // ^^ this worked for all but one, that one user had many other issues, reverting to this 
    
    //     // if Z is added to the end here, it causes the timezone bias to be applied twice
    //     string dateString = date.ToString("yyyy-MM-ddTHH:mm:ss.fff");
    //     return "'" + dateString + "'";
    // }

    /// <inheritdoc />
    public string TimestampDiffSeconds(string start, string end, string asColumn)
       => $"(strftime('%s', {end}) - strftime('%s', {start})) AS {asColumn}";
    
    /// <inheritdoc />
    public int GetOpenedConnections()
        => writeSemaphore.CurrentInUse;
    
    
    public SQLiteConnector(ILogger logger, string connectionString)
    {
        Logger = logger;

        if (string.IsNullOrWhiteSpace(DirectoryHelper.DatabaseDirectory) == false)
        {
            // database directory can be null during unit testing
            // if connection string is using relative file, update with full path
            connectionString = connectionString.Replace($"Data Source=FileFlows.sqlite",
                $"Data Source={Path.Combine(DirectoryHelper.DatabaseDirectory, "FileFlows.sqlite")}");
        }

        dbConnectionWrite = CreateConnection(connectionString);
        dbConnectionWrite.OnDispose += Dispose;
        // readPool = new(() => CreateConnection(connectionString), 5);
    }

    private void Dispose(object? sender, EventArgs e)
    {
        writeSemaphore.Release();
    }

    private DatabaseConnection CreateConnection(string connectionString)
    {
        var db = new NPoco.Database(connectionString, new SQLiteDatabaseType(),
            PlatformHelper.IsArm ? Microsoft.Data.Sqlite.SqliteFactory.Instance : System.Data.SQLite.SQLiteFactory.Instance);

        db.Mappers = new()
        {
            Converters.GuidConverter.UseInstance(),
            Converters.CustomDbMapper.UseInstance(),
            // Converters.UtcDateConverter.UseInstance()
        };

        return new DatabaseConnection(db, false);
    }


    /// <inheritdoc />
    public DatabaseType Type => DatabaseType.Sqlite;

    /// <inheritdoc />
    public async Task<DatabaseConnection> GetDb(bool write)
    {
        await writeSemaphore.WaitAsync();
        return dbConnectionWrite;
    }

    /// <inheritdoc />
    public string WrapFieldName(string name) => name;
    
    /// <inheritdoc />
    public async Task<bool> ColumnExists(string table, string column)
    {
        using var db = await GetDb(false);
        bool exists = db.Db.ExecuteScalar<int>("SELECT COUNT(*) AS CNTREC FROM pragma_table_info(@0) WHERE name=@1", table, column) > 0;
        return exists;
    }

    /// <inheritdoc />
    public async Task<bool> TableExists(string table, DatabaseConnection? db = null)
    {
        const string sql = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@0";
        if (db == null)
        {
            using var db2 = await GetDb(false);
            bool tableExists = db2.Db.ExecuteScalar<int>(sql, table) > 0;
            return tableExists;
        }
        else
        {
            bool tableExists = db.Db.ExecuteScalar<int>(sql, table) > 0;
            return tableExists;
        }
    }

    /// <inheritdoc />
    public async Task CreateColumn(string table, string column, string type, string defaultValue)
    {
        string sql = $@"ALTER TABLE {table} ADD COLUMN {column} {type}" + (string.IsNullOrWhiteSpace(defaultValue) ? "" : $" DEFAULT {defaultValue}");
        using var db = await GetDb(false);
        await db.Db.ExecuteAsync(sql);
    }
    
    /// <summary>
    /// Looks to see if the file in the specified connection string exists, and if so, moves it
    /// </summary>
    /// <param name="connectionString">The connection string</param>
    internal static void MoveFileFromConnectionString(string connectionString)
    {
        string filename = GetFilenameFromConnectionString(connectionString);
        if (string.IsNullOrWhiteSpace(filename))
            return;
        
        if (File.Exists(filename) == false)
            return;
        
        string dest = filename + ".backup";
        File.Move(filename, dest, true);
    }

    /// <summary>
    /// Gets the filename from a connection string
    /// </summary>
    /// <param name="connectionString">the connection string</param>
    /// <returns>the filename</returns>
    private static string GetFilenameFromConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return string.Empty;
        return Regex.Match(connectionString, @"(?<=(Data Source=))[^;]+")?.Value ?? string.Empty;
    }
}