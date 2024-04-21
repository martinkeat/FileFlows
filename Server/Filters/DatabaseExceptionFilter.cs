using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FileFlows.Server.Filters;

/// <summary>
/// Represents a custom exception filter that redirects to "/database-offline"
/// if the exception message contains "at Npgsql.NpgsqlConnection.Open".
/// </summary>
public class DatabaseExceptionFilter : IExceptionFilter
{
    /// <summary>
    /// Called when an exception occurs during the processing of a request.
    /// </summary>
    /// <param name="context">The context for the action that threw the exception.</param>
    public void OnException(ExceptionContext context)
    {
        // Check if an exception occurred
        if (context.Exception == null)
            return;
        
        // Check if the request URL contains "/remote/"
        var requestPath = context.HttpContext.Request.Path;
        if (requestPath.HasValue && requestPath.Value.Contains("/remote/"))
            return;
        
        // Get the exception message
        string exceptionMessage = context.Exception.ToString();

        // Check if the exception message contains "at Npgsql.NpgsqlConnection.Open"
        if (exceptionMessage.Contains("at Npgsql.NpgsqlConnection.Open") == false)
            return;
        
        // Redirect to "/database-offline"
        context.Result = new RedirectResult("/database-offline");
    }
}
