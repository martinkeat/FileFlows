using FileFlows.Server.Services;

namespace FileFlows.Server.Controllers;

using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Home controller
/// </summary>
public class HomeController : Controller
{
    /// <summary>
    /// Main application index page
    /// </summary>
    /// <returns>the index page</returns>
    [ApiExplorerSettings(IgnoreApi = true)]
    [ResponseCache(NoStore = true, Duration = 0)]
    public IActionResult Index()
    {
        return File("~/index.html", "text/html");
    }
    
    /// <summary>
    /// Database is offline error message
    /// </summary>
    /// <returns>the view</returns>
    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpGet("database-offline")]
    public IActionResult DatabaseOffline()
    {
        var result = new StartupService().CanConnectToDatabase();
        if (result is { IsFailed: false, Value: true })
        {
            // no longer disconnected
            return Redirect("/");
        }
        ViewBag.Title = "Database Offline";
        ViewBag.Message = "Database is offline.\nPlease check the database is running and FileFlows has access to it.";
        return View("Error");
    }
}
