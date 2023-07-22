using System.Net;

namespace FileFlows.Shared.Models;

/// <summary>
/// A HTTP request result
/// </summary>
/// <typeparam name="T">the type of object the request returns</typeparam>
public class RequestResult<T>
{
    /// <summary>
    /// Gets or sets if the request was successful
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Gets or sets the HTML body of the request
    /// </summary>
    public string Body { get; set; }
    
    /// <summary>
    /// Gets or sets the parsed response object 
    /// </summary>
    public T Data { get; set; }
    
    /// <summary>
    /// Gets or sets the status code from the response
    /// </summary>
    public HttpStatusCode StatusCode { get; set; }
    
    /// <summary>
    /// Gets or sets any custom headers returned
    /// </summary>
    public Dictionary<string, string> Headers { get; set; }
}