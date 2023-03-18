using System.Data.Common;
using System.Text.RegularExpressions;
using FileFlows.Server.Controllers;
using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;
using NPoco;

namespace FileFlows.Server.Database.Managers;

/// <summary>
/// A database manager used to communicate with a Sqlite Database
/// </summary>
public class SqliteDbManager : DbManager
{
    private readonly string DbFilename;
    
    
    /// <summary>
    /// Constructs a new Sqlite Database Manager
    /// </summary>
    /// <param name="connectionString">The connection string to the database</param>
    public SqliteDbManager(string connectionString)
    {
        // Data Source={DbFilename};Version=3;
        ConnectionString = connectionString;
        DbFilename = GetFilenameFromConnectionString(connectionString);
        DbHelper.IsSqlLite = true;
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

    /// <summary>
    /// Constructs a new Sqlite Database Manager for a sqlite db file
    /// </summary>
    /// <param name="dbFile">the filename of the sqlite db file</param>
    /// <returns>A new Sqlite Database Manager instance</returns>
    public static SqliteDbManager ForFile(string dbFile) => new SqliteDbManager(GetConnetionString(dbFile));

    /// <summary>
    /// Gets a sqlite connection string for a db file
    /// </summary>
    /// <param name="dbFile">the filename of the sqlite db file</param>
    /// <returns>a sqlite connection string</returns>
    public static string GetConnetionString(string dbFile)
    {
        if (Globals.IsArm)
            return $"Data Source={dbFile}";
        return $"Data Source={dbFile};Version=3;PRAGMA journal_mode=WAL;";
    } 
        

    /// <summary>
    /// Gets if the database manager should use a memory cache
    /// Sqlite uses a memory cache due to its limitation of concurrent reading/writing
    /// </summary>
    public override bool UseMemoryCache => true;

    /// <summary>
    /// Get an instance of the IDatabase
    /// </summary>
    /// <returns>an instance of the IDatabase</returns>
    protected override NPoco.Database GetDbInstance() => GetDb(this.ConnectionString);

    /// <summary>
    /// Gets a database instance
    /// </summary>
    /// <param name="connectionString">the connection of the database to open</param>
    /// <returns>a database instance</returns>
    internal static NPoco.Database GetDb(string connectionString)
    {
        try
        {
            var db = new NPoco.Database(connectionString, null,
                Globals.IsArm ? Microsoft.Data.Sqlite.SqliteFactory.Instance : System.Data.SQLite.SQLiteFactory.Instance);
            db.Mappers.Add(new GuidConverter());
            return db;
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Error loading database: " + ex.Message);
            throw;
        }
        
    }

    #region setup code
    /// <summary>
    /// Creates the actual Database
    /// </summary>
    /// <param name="recreate">if the database should be recreated if already exists</param>
    /// <returns>true if successfully created</returns>
    protected override DbCreateResult CreateDatabase(bool recreate)
    {
        if (File.Exists(DbFilename) == false)
        {
            FileStream fs = File.Create(DbFilename);
            fs.Close();
            return DbCreateResult.Created;
        }
        
        // create backup 
        File.Copy(DbFilename, DbFilename + ".backup", true);
        return DbCreateResult.AlreadyExisted;
    }

    /// <summary>
    /// Creates the tables etc in the database
    /// </summary>
    /// <returns>true if successfully created</returns>
    protected override bool CreateDatabaseStructure()
    {
        string connString = GetConnetionString(DbFilename);
        using DbConnection con = Globals.IsArm ? new Microsoft.Data.Sqlite.SqliteConnection(connString) :
            new System.Data.SQLite.SQLiteConnection(connString);
        con.Open();
        try
        {
            string sqlTables = GetSqlScript("Sqlite", "Tables.sql", clean: true);
            
            using DbCommand cmd = Globals.IsArm
                ? new Microsoft.Data.Sqlite.SqliteCommand(sqlTables, (Microsoft.Data.Sqlite.SqliteConnection)con) 
                : new System.Data.SQLite.SQLiteCommand(sqlTables, (System.Data.SQLite.SQLiteConnection)con);
            cmd.ExecuteNonQuery();
            
            // foreach (var tbl in new[]
            //          {
            //              (nameof(DbObject), CreateDbObjectTableScript),
            //              (nameof(DbStatistic), CreateDbStatisticTableScript),
            //              (nameof(RevisionedObject), CreateDbRevisionedObjectTableScript),
            //          })
            // {
            //     string sqlExists = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{tbl.Item1}'";
            //     using DbCommand cmdExists = Globals.IsArm
            //         ? new Microsoft.Data.Sqlite.SqliteCommand(sqlExists, (Microsoft.Data.Sqlite.SqliteConnection)con)
            //         : new System.Data.SQLite.SQLiteCommand(sqlExists, (System.Data.SQLite.SQLiteConnection)con);
            //     if (cmdExists.ExecuteScalar() != null)
            //         continue;
            //
            //     using DbCommand cmd = Globals.IsArm
            //         ? new Microsoft.Data.Sqlite.SqliteCommand(tbl.Item2, (Microsoft.Data.Sqlite.SqliteConnection)con) 
            //         : new System.Data.SQLite.SQLiteCommand(tbl.Item2, (System.Data.SQLite.SQLiteConnection)con);
            //     cmd.ExecuteNonQuery();
            // }

            return true;
        }
        finally
        {
            con.Close();
        }
    }

    public override Task<Flow> GetFailureFlow(Guid libraryUid)
    {
        throw new NotImplementedException();
    }


    #endregion

    /// <summary>
    /// Looks to see if the file in the specified connection string exists, and if so, moves it
    /// </summary>
    /// <param name="connectionString">The connection string</param>
    public static void MoveFileFromConnectionString(string connectionString)
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
    /// Gets statistics by name
    /// </summary>
    /// <returns>the matching statistics</returns>
    public override async Task<IEnumerable<Statistic>> GetStatisticsByName(string name)
    {
        List<DbStatistic> stats;
        using (var db = await GetDb())
        {
            stats = await db.Db.FetchAsync<DbStatistic>("where Name = @0", name);
        }

        var results = new List<Statistic>();
        foreach (var stat in stats)
        {
            if(stat.Type == StatisticType.Number)
                results.Add(new () { Name = stat.Name, Value = stat.NumberValue});
            if(stat.Type == StatisticType.String)
                results.Add(new () { Name = stat.Name, Value = stat.StringValue});
        }

        return results;
    }
    
    /// <summary>
    /// Gets if a column exists in the given table
    /// </summary>
    /// <param name="table">the table name</param>
    /// <param name="column">the column to look for</param>
    /// <returns>true if it exists, otherwise false</returns>
    public override async Task<bool> ColumnExists(string table, string column)
    {
        using var db = await GetDb();
        bool exists = db.Db.ExecuteScalar<int>("SELECT COUNT(*) AS CNTREC FROM pragma_table_info(@0) WHERE name=@1", table, column) > 0;
        return exists;
    }
}