using System.Text.RegularExpressions;
using FileFlows.Plugin;
using FileFlows.ServerShared.Helpers;
using DatabaseType = FileFlows.Shared.Models.DatabaseType;

namespace FileFlows.DataLayer.DatabaseConnectors;

/// <summary>
/// Connector for SQLite
/// </summary>
public class SQLiteConnector : IDatabaseConnector
{
    private DatabaseConnection dbConnectionWrite;
    private FairSemaphore writeSemaphore = new(1);
    // private DatabaseConnectionPool readPool;
    
    /// <summary>
    /// Logger used for logging
    /// </summary>
    private ILogger Logger;

    /// <inheritdoc />
    public string FormatDateQuoted(DateTime date)
        => "datetime('" + date.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") + "', 'utc')";
    
    /// <inheritdoc />
    public string TimestampDiffSeconds(string start, string end, string asColumn)
       => $" timestampdiff(second, {start}, {end}) AS {asColumn} ";
    
    /// <inheritdoc />
    public string DayOfWeek(string column, string asColumn = null)
        => $"(strftime('%w', {column}) + 1)" + (string.IsNullOrEmpty(asColumn) ? string.Empty : $" as {asColumn}");
    
    /// <inheritdoc />
    public string Hour(string column, string asColumn = null)
        => $"strftime('%H', {column})" + (string.IsNullOrEmpty(asColumn) ? string.Empty : $" as {asColumn}");
    
    
    public SQLiteConnector(ILogger logger, string connectionString)
    {
        Logger = logger;

        // if connection string is using relative file, update with full path
        connectionString = connectionString.Replace($"Data Source=FileFlows.sqlite",
            $"Data Source={Path.Combine(DirectoryHelper.DatabaseDirectory, "FileFlows.sqlite")}");
        
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
        var db = new NPoco.Database(connectionString, null,
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
        //if (write)
        {
            await writeSemaphore.WaitAsync();
            return dbConnectionWrite;
        }
        
        

        //return await readPool.AcquireConnectionAsync();
    }

    /// <inheritdoc />
    public string WrapFieldName(string name) => name;
    
    
    
    
    
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