namespace FileFlows.RemoteServices;

/// <summary>
/// A service used to log messages to the server
/// </summary>
public class LogService : RemoteService, ILogService
{
    /// <summary>
    /// Logs a message to the server
    /// </summary>
    /// <param name="message">The log message to log</param>
    public async Task LogMessage(LogServiceMessage message)
    {
        try
        {
            await HttpHelper.Post($"{ServiceBaseUrl}/remote/log", message, timeoutSeconds: 2);
        }
        catch (Exception)
        {
            // silent fail
        }
    }
}