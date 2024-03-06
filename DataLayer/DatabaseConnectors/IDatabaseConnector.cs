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
}