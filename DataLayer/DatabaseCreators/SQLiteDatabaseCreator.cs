using System.Data.Common;
using System.Text.RegularExpressions;
using FileFlows.DataLayer.Helpers;
using FileFlows.Plugin;
using FileFlows.ServerShared.Helpers;

namespace FileFlows.DataLayer.DatabaseCreators;

/// <summary>
/// SQLite Database Creator
/// </summary>
public class SQLiteDatabaseCreator : IDatabaseCreator
{
    /// <summary>
    /// The connection string to the database
    /// </summary>
    private string ConnectionString { get; init; }
    /// <summary>
    /// The logger to use
    /// </summary>
    private ILogger Logger;
    /// <summary>
    /// The filename of the database file
    /// </summary>
    private readonly string DbFilename;
    
    public SQLiteDatabaseCreator(ILogger logger, string connectionString)
    {
        Logger = logger;
        
        // if connection string is using relative file, update with full path
        connectionString = connectionString.Replace($"Data Source=FileFlows.sqlite",
            $"Data Source={Path.Combine(DirectoryHelper.DatabaseDirectory, "FileFlows.sqlite")}");
        
        ConnectionString = connectionString;
        DbFilename = GetFilenameFromConnectionString(connectionString);
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
    
    /// <inheritdoc />
    public Result<DbCreateResult> CreateDatabase(bool recreate)
    {
        var info = new FileInfo(DbFilename);
        if(info.Exists && info.Length < 0)
            info.Delete();

        if (info.Exists && recreate)
        {
            // move to a backup file
            info.MoveTo(DbFilename + ".backup", true);
            info = new FileInfo(DbFilename);
        }
        
        if(info.Exists == false)
        {
            FileStream fs = File.Create(DbFilename);
            fs.Close();
            return DbCreateResult.Created;
        }
        
        return DbCreateResult.AlreadyExisted;
    }
    
    
    /// <inheritdoc />
    public Result<bool> CreateDatabaseStructure()
    {
        string connString = SqliteHelper.GetConnectionString(DbFilename);
        using DbConnection con = PlatformHelper.IsArm ? new Microsoft.Data.Sqlite.SqliteConnection(connString) :
            new System.Data.SQLite.SQLiteConnection(connString);
        con.Open();
        try
        {
            string sqlTables = ScriptHelper.GetSqlScript("Sqlite", "Tables.sql", clean: true);
            if (string.IsNullOrWhiteSpace(sqlTables))
                return Result<bool>.Fail("Failed to load Tables.sql");

            using DbCommand cmd = PlatformHelper.IsArm
                ? new Microsoft.Data.Sqlite.SqliteCommand(sqlTables, (Microsoft.Data.Sqlite.SqliteConnection)con) 
                : new System.Data.SQLite.SQLiteCommand(sqlTables, (System.Data.SQLite.SQLiteConnection)con);
            cmd.ExecuteNonQuery();

            return true;
        }
        finally
        {
            con.Close();
        }
    }
}