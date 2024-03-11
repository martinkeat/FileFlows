using FileFlows.Managers;
using FileFlows.Plugin;
using FileFlows.ServerShared.Models;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// Database log service
/// </summary>
public class DatabaseLogService
{
    private readonly List<DbLogMessage> LogMessages = new();
    /// <summary>
    /// Logs a message
    /// </summary>
    /// <param name="clientUid"></param>
    /// <param name="type"></param>
    /// <param name="message"></param>
    /// <exception cref="NotImplementedException"></exception>
    public async Task Log(Guid clientUid, LogType type, string message)
    {
        // by bucketing this it greatly improves speed
        List<DbLogMessage> toInsert = new();
        lock (LogMessages)
        {
            LogMessages.Add(new ()
            {
                ClientUid =  clientUid, 
                Type = type, 
                Message = message, 
                LogDate = DateTime.UtcNow
            });
            if (LogMessages.Count > 20)
            {
                toInsert = LogMessages.ToList();
                LogMessages.Clear();
            }
        }
        if(toInsert?.Any() == true)
        {
            await new DatabaseLogManager().BulkInsert(toInsert.ToArray());
        }
    }


    /// <summary>
    /// Searches the database for the log messages
    /// </summary>
    /// <param name="filter">the search filter</param>
    /// <returns>the matching log messages</returns>
    public Task<List<DbLogMessage>> Search(LogSearchModel filter)
        =>  new DatabaseLogManager().Search(filter);
}