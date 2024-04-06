using FileFlows.Plugin;
using FileFlows.Server.Controllers;
using FileFlows.Server.Services;

namespace FileFlows.Server.Middleware;

/// <summary>
/// A middleware used to log all requests
/// </summary>
public class LoggingMiddleware
{
    /// <summary>
    /// Next request delegate
    /// </summary>
    private readonly RequestDelegate _next;

    private SettingsService _settingsService;
    /// <summary>
    /// Settings service
    /// </summary>
    private SettingsService SettingsService
    {
        get
        {
            if (_settingsService == null)
                _settingsService = ServiceLoader.Load<SettingsService>();
            return _settingsService;
        }
    }
    
    
    /// <summary>
    /// Gets the logger for the request logger
    /// </summary>
    public static FileLogger RequestLogger { get; private set; }

    /// <summary>
    /// Constructs a instance of the exception middleware
    /// </summary>
    /// <param name="next">the next middleware to call</param>
    public LoggingMiddleware(RequestDelegate next)
    {
        _next = next;
        RequestLogger = new FileLogger(DirectoryHelper.LoggingDirectory, "FileFlowsHTTP", register: false);
    }

    /// <summary>
    /// Invokes the middleware
    /// </summary>
    /// <param name="context">the HttpContext executing this middleware</param>
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        finally
        {
            try
            {
                if (SettingsService.Get().Result.LogEveryRequest)
                {
                    _ = RequestLogger.Log((LogType) 999,
                        $"REQUEST [{context.Request?.Method}] [{context.Response?.StatusCode}]: {context.Request?.Path.Value}");
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
