using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using FileFlows.DataLayer.Helpers;
using FileFlows.Plugin;
using NPoco;
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
        => "'" + date.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ") + "'";
    
    public SQLiteConnector(ILogger logger, string connectionString)
    {
        Logger = logger;
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

    
    /// <summary>
    /// Gets a sqlite connection string for a db file
    /// </summary>
    /// <param name="dbFile">the filename of the sqlite db file</param>
    /// <returns>a sqlite connection string</returns>
    public static string GetConnectionString(string dbFile)
    {
        if (PlatformHelper.IsArm)
            return $"Data Source={dbFile}";
        return $"Data Source={dbFile};Version=3;PRAGMA journal_mode=WAL;";
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