using System.ComponentModel;
using System.Runtime.InteropServices;
using FileFlows.DataLayer.Helpers;
using FileFlows.Plugin;
using NPoco;

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
        
        db.Mappers.Add(new Converters.GuidConverter());

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
}