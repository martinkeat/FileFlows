using FileFlows.Plugin;
using FileFlows.Shared.Models;

namespace FileFlows.DataLayer.DatabaseConnectors;

/// <summary>
/// Loads a database connector
/// </summary>
internal static class DatabaseConnectorLoader
{
    /// <summary>
    /// Loads a database connector 
    /// </summary>
    /// <param name="logger">The logger to used for logging</param>
    /// <param name="type">The type of connector to load</param>
    /// <param name="connectionString">The connection string of the database</param>
    /// <returns>The initialized connector</returns>
    internal static IDatabaseConnector LoadConnector(ILogger logger, DatabaseType type, string connectionString)
    {
        switch (type)
        {
            case DatabaseType.MySql:
                return new MySqlConnector(logger, connectionString);
            case DatabaseType.SqlServer:
                return new SqlServerConnector(logger, connectionString);
            case DatabaseType.Postgres:
                return new PostgresConnector(logger, connectionString);
            default:
                return new SQLiteConnector(logger, connectionString);
        }
    }
}

/// <summary>
/// Interface for different database connection types
/// </summary>
public interface IDatabaseConnector
{
    /// <summary>
    /// Gets the database type
    /// </summary>
    DatabaseType Type { get; }
    
    /// <summary>
    /// Gets the database connection
    /// </summary>
    /// <param name="write">If the query will be writing data</param>
    /// <returns>the database connection</returns>
    Task<DatabaseConnection> GetDb(bool write = false);

    /// <summary>
    /// Wraps a field name in the character supported by this database
    /// </summary>
    /// <param name="name">the name of the field to wrap</param>
    /// <returns>the wrapped field name</returns>
    string WrapFieldName(string name);

    /// <summary>
    /// Converts a datetime to a string for the database in quotes
    /// </summary>
    /// <param name="date">the date to convert</param>
    /// <returns>the converted data as a string</returns>
    string FormatDateQuoted(DateTime date);

    /// <summary>
    /// Creates a time difference sql select
    /// </summary>
    /// <param name="start">the start column</param>
    /// <param name="end">the end column</param>
    /// <param name="asColumn">the name of the result</param>
    /// <returns>the sql select statement</returns>
    string TimestampDiffSeconds(string start, string end, string asColumn);

    /// <summary>
    /// Creates a day of week sql select
    /// </summary>
    /// <param name="column">the date column</param>
    /// <param name="asColumn">the name of the result</param>
    /// <returns>the sql select statement</returns>
    string DayOfWeek(string column, string asColumn = null);

    /// <summary>
    /// Creates a hour sql select
    /// </summary>
    /// <param name="column">the date column</param>
    /// <param name="asColumn">the name of the result</param>
    /// <returns>the sql select statement</returns>
    string Hour(string column, string asColumn = null);

    /// <summary>
    /// Check to see if a column exists
    /// </summary>
    /// <param name="table">the table to check</param>
    /// <param name="column">the name of the column</param>
    /// <returns>true if the column exists, otherwise false</returns>
    Task<bool> ColumnExists(string table, string column);

    /// <summary>
    /// Creates a column with the given name, data type, and default value.
    /// </summary>
    /// <param name="table">The name of the table.</param>
    /// <param name="column">The name of the column to create.</param>
    /// <param name="type">The data type of the column.</param>
    /// <param name="defaultValue">The default value for the column.</param>
    Task CreateColumn(string table, string column, string type, string defaultValue);
}