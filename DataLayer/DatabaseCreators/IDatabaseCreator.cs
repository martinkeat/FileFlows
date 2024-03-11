using FileFlows.Plugin;
using FileFlows.Shared.Models;

namespace FileFlows.DataLayer.DatabaseCreators;

public interface IDatabaseCreator
{
    /// <summary>
    /// Creates the database
    /// </summary>
    /// <param name="recreate">If the database should be recreated if it already exists</param>
    /// <returns>true if successful</returns>
    Result<DbCreateResult> CreateDatabase(bool recreate);

    /// <summary>
    /// Creates the database structure, ie the tables
    /// </summary>
    /// <returns>if successful or not</returns>
    Result<bool> CreateDatabaseStructure();
}

/// <summary>
/// Static methods for database creators
/// </summary>
internal static class DatabaseCreator 
{
    /// <summary>
    /// Gets a database creator from its type 
    /// </summary>
    /// <param name="logger">the logger to use</param>
    /// <param name="type">the type of database to use</param>
    /// <param name="connectionString">the connection string to the database</param>
    /// <returns>the database creator</returns>
    internal static IDatabaseCreator Get(ILogger logger, DatabaseType type, string connectionString)
    {
        switch (type)
        {
            case DatabaseType.Postgres:
                return new PostgresDatabaseCreator(logger, connectionString);
            case DatabaseType.MySql:
                return new MySqlDatabaseCreator(logger, connectionString);
            case DatabaseType.SqlServer:
                return new SqlServerDatabaseCreator(logger, connectionString);
            default:
                return new SQLiteDatabaseCreator(logger, connectionString);
        }
    }
}