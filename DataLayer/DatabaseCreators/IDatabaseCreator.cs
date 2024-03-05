using FileFlows.Plugin;

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