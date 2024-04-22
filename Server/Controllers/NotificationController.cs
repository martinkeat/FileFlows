using FileFlows.Server.Authentication;
using FileFlows.Server.Services;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace FileFlows.Server.Controllers;

/// <summary>
/// Controller for notifications
/// </summary>
[Route("/api/notification")]
[FileFlowsAuthorize(UserRole.Admin)]
public class NotificationController : Controller
{
    /// <summary>
    /// Gets all the notifications
    /// </summary>
    /// <returns>the notifications</returns>
    [HttpGet]
    public Task<IEnumerable<Notification>> Get()
        => ServiceLoader.Load<NotificationService>().GetAll();
}