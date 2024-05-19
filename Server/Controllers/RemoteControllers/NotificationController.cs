using FileFlows.RemoteServices;
using FileFlows.Server.Authentication;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace FileFlows.Server.Controllers.RemoteControllers;

/// <summary>
/// Notification controller
/// </summary>
[Route("/remote/notification")]
[FileFlowsApiAuthorize]
[ApiExplorerSettings(IgnoreApi = true)]
public class NotificationController
{
    /// <summary>
    /// Records a notification
    /// </summary>
    /// <param name="notification">the notification being recorded</param>
    [HttpPost("record")]
    public async Task Record([FromBody] Notification notification)
    {
        var service = ServiceLoader.Load<NotificationService>();
        await service.Record(notification.Severity, notification.Title, notification.Message);
    }
}