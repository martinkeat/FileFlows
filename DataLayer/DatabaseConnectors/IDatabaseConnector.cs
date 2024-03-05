using FileFlows.Plugin;

namespace FileFlows.DataLayer.DatabaseConnectors;

/// <summary>
/// Interface for different database connection types
/// </summary>
public interface IDatabaseConnector
{
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
}