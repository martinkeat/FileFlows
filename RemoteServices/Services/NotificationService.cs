namespace FileFlows.RemoteServices;

/// <summary>
/// Service for sending a notification to the server
/// </summary>
public class NotificationService : RemoteService
{
    /// <summary>
    /// Records a new notification with the specified severity, title, and message.
    /// </summary>
    /// <param name="severity">The severity level of the notification.</param>
    /// <param name="title">The title of the notification.</param>
    /// <param name="message">The message content of the notification.</param>
    public async Task Record(NotificationSeverity severity, string title, string? message = null)
    {
        try
        {
            await HttpHelper.Post($"{ServiceBaseUrl}/remote/notification/record", new
            {
                Severity = severity,
                Title = title,
                Message = message
            });
        }
        catch (Exception)
        {
            // ignored
        }
    }
}
