namespace FileFlows.Server;

/// <summary>
/// Extension methods
/// </summary>
public static class ExtensionMethods
{
    /// <summary>
    /// Downloads a remote file and saves it locally
    /// </summary>
    /// <param name="client">The HttpClient instance</param>
    /// <param name="url">The url to download the file from</param>
    /// <param name="filename">The filename to save the file to</param>
    /// <returns>an awaited task</returns>
    public static async Task DownloadFile(this HttpClient client, string url, string filename)
    {
        using (var s = await client.GetStreamAsync(url))
        {
            using (var fs = new FileStream(filename, FileMode.CreateNew))
            {
                await s.CopyToAsync(fs);
            }
        }
    }
    
    /// <summary>
    /// Gets the actual IP address of the request
    /// </summary>
    /// <param name="Request">the request</param>
    /// <returns>the actual IP Address</returns>
    public static string GetActualIP(this HttpRequest Request)
    {
        try
        {
            foreach (string header in new[] { "True-Client-IP", "CF-Connecting-IP", "HTTP_X_FORWARDED_FOR" })
            {
                if (Request.Headers.ContainsKey(header) && string.IsNullOrEmpty(Request.Headers[header]) == false)
                {
                    string? ip = Request.Headers[header].FirstOrDefault()
                        ?.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)?.FirstOrDefault();
                    if (string.IsNullOrEmpty(ip) == false)
                        return ip;
                }
            }

            return Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}
