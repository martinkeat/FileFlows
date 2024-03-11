using FileFlows.ServerShared.Models;

namespace FileFlows.Managers;

/// <summary>
/// Database log manager
/// </summary>
public class DatabaseLogManager
{
    /// <summary>
    /// Bulk inserts multiple messages
    /// </summary>
    /// <param name="messages">the messages to insert</param>
    public Task BulkInsert(params DbLogMessage[] messages)
        => DatabaseAccessManager.Instance.LogMessageManager.BulkInsert(messages);

    /// <summary>
    /// Deletes old log messages
    /// </summary>
    /// <param name="max">the max log message to retrain</param>
    /// <returns>a task to await</returns>
    public Task PruneOldLogs(int max)
        => DatabaseAccessManager.Instance.LogMessageManager.PruneOldLogs(max);

    /// <summary>
    /// Searches the database for the log messages
    /// </summary>
    /// <param name="filter">the search filter</param>
    /// <returns>the matching log messages</returns>
    public Task<List<DbLogMessage>> Search(LogSearchModel filter)
        => DatabaseAccessManager.Instance.LogMessageManager.Search(filter);
}