using System.Text.RegularExpressions;
using FileFlows.DataLayer.Helpers;
using FileFlows.Plugin;
using NPoco;
using MySqlConnectorFactory = MySqlConnector.MySqlConnectorFactory;

namespace FileFlows.DataLayer.DatabaseCreators;

/// <summary>
/// A MySQL database creator
/// </summary>
public class MySqlDatabaseCreator : IDatabaseCreator
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
    /// Initializes an instance of the MySQL database creator
    /// </summary>
    /// <param name="logger">the logger to use</param>
    /// <param name="connectionString">the connection string</param>
    public MySqlDatabaseCreator(ILogger logger, string connectionString)
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
        
        using var db = new Database(connString, null, MySqlConnectorFactory.Instance);
        bool exists = string.IsNullOrEmpty(db.ExecuteScalar<string>("select schema_name from information_schema.schemata where schema_name = @0", dbName)) == false;
        if (exists)
        {
            if(recreate == false)
                return DbCreateResult.AlreadyExisted;
            Logger.ILog("Dropping existing database");
            db.Execute($"drop database {dbName};");
        }

        Logger.ILog("Creating Database");
        return db.Execute("create database " + dbName + " character set utf8 collate 'utf8_unicode_ci';") > 0 ? 
            DbCreateResult.Created : DbCreateResult.Failed;
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
        
        using var db = new NPoco.Database(ConnectionString, null, MySqlConnector.MySqlConnectorFactory.Instance);
        string sqlTables = ScriptHelper.GetSqlScript("MySql", "Tables.sql", clean: true);
        Logger.ILog("SQL Tables:\n" + sqlTables);
        db.Execute(sqlTables);
        
        return true;
    }
}