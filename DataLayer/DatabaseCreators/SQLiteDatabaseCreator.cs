using System.Data.Common;
using System.Text.RegularExpressions;
using FileFlows.DataLayer.DatabaseConnectors;
using FileFlows.DataLayer.Helpers;
using FileFlows.Plugin;

namespace FileFlows.DataLayer.DatabaseCreators;

public class SQLiteDatabaseCreator : IDatabaseCreator
{
    private string ConnectionString { get; init; }
    private ILogger Logger;
    private readonly string DbFilename;
    
    public SQLiteDatabaseCreator(ILogger logger, string connectionString)
    {
        Logger = logger;
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
        
        if(info.Exists == false)
        {
            FileStream fs = File.Create(DbFilename);
            fs.Close();
            return DbCreateResult.Created;
        }
        
        // create backup 
        File.Copy(DbFilename, DbFilename + ".backup", true);
        return DbCreateResult.AlreadyExisted;
    }
    
    
    /// <inheritdoc />
    public Result<bool> CreateDatabaseStructure()
    {
        string connString = SQLiteConnector.GetConnectionString(DbFilename);
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