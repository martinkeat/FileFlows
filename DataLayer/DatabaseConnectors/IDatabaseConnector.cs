using FileFlows.Plugin;
using FileFlows.Shared.Models;

namespace FileFlows.DataLayer.DatabaseConnectors;

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
}