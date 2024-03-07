using System.Text.RegularExpressions;
using FileFlows.DataLayer.Helpers;
using FileFlows.Plugin;
using Microsoft.Data.SqlClient;
using NPoco;

namespace FileFlows.DataLayer.DatabaseCreators;

/// <summary>
/// Creator for a Postgres database
/// </summary>
public class PostgresDatabaseCreator : IDatabaseCreator
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
    /// Initializes an instance of the Postgres database creator
    /// </summary>
    /// <param name="logger">the logger to use</param>
    /// <param name="connectionString">the connection string</param>
    public PostgresDatabaseCreator(ILogger logger, string connectionString)
    {
        Logger = logger;
        ConnectionString = connectionString;
    }
    
    /// <inheritdoc />
    public Result<DbCreateResult> CreateDatabase(bool recreate)
    {
        string connString = Regex.Replace(ConnectionString, "(^|;)Database=[^;]+", "");
        if (connString.StartsWith(";"))
            connString = connString[1..];
        string dbName = GetDatabaseName();
        
        using var db = new Database(connString, null, Npgsql.NpgsqlFactory.Instance);
        bool exists = db.ExecuteScalar<int>("SELECT CASE WHEN EXISTS (SELECT 1 FROM pg_database WHERE datname = @0) THEN 1 ELSE 0 END", dbName) == 1;
        if (exists)
        {
            if(recreate == false)
                return DbCreateResult.AlreadyExisted;
            Logger.ILog("Dropping existing database");
            db.Execute($"SELECT pg_terminate_backend(pg_stat_activity.pid) FROM pg_stat_activity WHERE pg_stat_activity.datname = '{dbName}' AND pid <> pg_backend_pid();");
            db.Execute($"DROP DATABASE \"{dbName}\";");
        }

        Logger.ILog("Creating Database");
        var sql = $"CREATE DATABASE \"{dbName}\"";
        Logger.ILog("SQL: " + sql);
        try
        {
            db.Execute(sql);
            return DbCreateResult.Created;
        }
        catch (Exception ex)
        {
            Logger.ELog("Error creating SQL Server database: " + ex.Message);
            return DbCreateResult.Failed;
        }
    }
    
    /// <summary>
    /// Gets the database name from the connection string
    /// </summary>
    /// <returns>the database name</returns>
    private string GetDatabaseName()
        => Regex.Match(ConnectionString, @"(?<=(Database=))[a-zA-Z0-9_\-]+").Value;
    
    
    /// <inheritdoc />
    public Result<bool> CreateDatabaseStructure()
    {
        Logger.ILog("Creating Database Structure");
        
        using var db = new NPoco.Database(ConnectionString, null, Npgsql.NpgsqlFactory.Instance);
        string sqlTables = ScriptHelper.GetSqlScript("Postgres", "Tables.sql", clean: true);
        db.Execute(sqlTables);
        
        return true;
    }
}