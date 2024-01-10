using System.Text;
using System.Text.Json;
using FileFlows.Plugin;
using HttpMethod = System.Net.Http.HttpMethod;

namespace FileFlows.ServerShared.FileServices;

/// <summary>
/// Helper class for downloading and validating files from a URL.
/// </summary>
public class FileDownloader
{
    private static readonly HttpClient _client;
    
    /// <summary>
    /// The logger used for log messages
    /// </summary>
    private ILogger logger;
    
    /// <summary>
    /// The URL where the file will be uploaded.
    /// </summary>
    private readonly string serverUrl;
    
    /// <summary>
    /// The UID of the executor for the authentication
    /// </summary>
    private readonly Guid executorUid;
    
    /// <summary>
    /// Constructs an instance of the file downloader
    /// </summary>
    /// <param name="logger">the logger to use in the file downloader</param>
    /// <param name="serverUrl">The URL where the file will be uploaded.</param>
    /// <param name="executorUid">The UID of the executor for the authentication</param>
    public FileDownloader(ILogger logger, string serverUrl,Guid executorUid)
    {
        this.logger = logger;
        this.serverUrl = serverUrl;
        this.executorUid = executorUid;
    }

    /// <summary>
    /// Static constructor
    /// </summary>
    static FileDownloader()
    {
        HttpClientHandler handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

        _client = new HttpClient(handler);
        _client.Timeout = Timeout.InfiniteTimeSpan;
    }

    /// <summary>
    /// Downloads a file from the specified URL and saves it to the destination path.
    /// Validates the downloaded file using a SHA256 hash provided by the server.
    /// </summary>
    /// <param name="uid">The UID of the file to download</param>
    /// <param name="destinationPath">The path where the downloaded file will be saved.</param>
    /// <returns>A tuple indicating success (True) or failure (False) and an error message if applicable.</returns>
    public async Task<Result<bool>> DownloadFile(string path, string destinationPath)
    {
        try
        {
            logger.ILog("Downloading file: " + path);
            logger.ILog("Destination file: " + destinationPath);
            string url = serverUrl;
            if (url.EndsWith("/") == false)
                url += "/";
            url += "api/file-server";
            
            DateTime start = DateTime.Now;
            
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url + "/download");
            request.Headers.Add("x-executor", executorUid.ToString());
            string json = JsonSerializer.Serialize(new { path });
            request.Content  = new StringContent(json, Encoding.UTF8, "application/json");

            // Send the request and read the response content as a stream
            HttpResponseMessage response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (response.IsSuccessStatusCode == false)
            {
                string error = (await response.Content.ReadAsStringAsync()).EmptyAsNull() ?? "Failed to remotely download the file";
                logger.ELog("Download Failed: " + error);
                return Result<bool>.Fail(error);
            }

            using Stream contentStream = await response.Content.ReadAsStreamAsync();


            long fileSize = 0;
            if (response.Content.Headers.ContentLength.HasValue)
            {
                fileSize = response.Content.Headers.ContentLength.Value;
                logger?.ILog($"Content-Length: {fileSize} bytes");
            }
            else
            {
                logger?.ILog("Content-Length header is not present in the response.");
            }

            using FileStream fileStream = File.OpenWrite(destinationPath);
            
            int bufferSize;
            
            if (fileSize < 10 * 1024 * 1024) // If file size is less than 10 MB
                bufferSize = 4 * 1024;
            else if (fileSize < 100 * 1024 * 1024) // If file size is less than 100 MB
                bufferSize = 64 * 1024;
            else
                bufferSize = 1 * 1024 * 1024;
            
            byte[] buffer = new byte[bufferSize]; 

            int bytesRead;
            long bytesReadTotal = 0;
            int percent = 0;
            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead);
                bytesReadTotal += bytesRead;
                float percentage = bytesReadTotal * 100f / fileSize;
                int iPercent = Math.Clamp((int)Math.Round(percentage), 0, 100);
                if (iPercent != percent)
                {
                    percent = iPercent;
                    logger.ILog($"Download Percentage: {percent} %");
                }
            }

            var timeTaken = DateTime.Now.Subtract(start);
            logger.ILog($"Time taken to download file: {timeTaken}");
            
            // using FileStream fileStream = File.OpenWrite(destinationPath);
            // await response.Content.CopyToAsync(fileStream);
            
            if(fileSize < 1)
                fileSize = await GetFileSize(url, path);
            // long downloadedBytes = 0;
            //
            // await using (var outputStream = new FileStream(destinationPath, FileMode.Create))
            // while (downloadedBytes < fileSize)
            // {
            //     var request = new HttpRequestMessage(HttpMethod.Get, serverUrl);
            //     request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(downloadedBytes, fileSize - 1);
            //
            //     using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            //     await using var responseStream = await response.Content.ReadAsStreamAsync();
            //     
            //     byte[] buffer = new byte[BufferSize];
            //     int bytesRead;
            //     while ((bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            //     {
            //         await outputStream.WriteAsync(buffer, 0, bytesRead);
            //         downloadedBytes += bytesRead;
            //     }
            // }

            var size = new FileInfo(destinationPath).Length;
            if (Math.Abs(size - fileSize) > bufferSize)
            {
                return Result<bool>.Fail("File failed to download completely!");
            }
            return true;

            // var hash = await FileHasher.Hash(destinationPath);
            //
            // if (await ValidateFileHash(url, hash) == false)
            // // if(downloadedHash != fileHash)
            // {
            //     return (false, "File validation failed!");
            // }
            //
            //
            // return (true, string.Empty);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail($"An error occurred: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Validates the downloaded file hash against the hash provided by the server for the specified URL.
    /// </summary>
    /// <param name="url">The URL of the file for which the hash needs to be validated.</param>
    /// <param name="hash">The hash of the downloaded file.</param>
    /// <returns>True if the file hash is validated successfully; otherwise, False.</returns>
    private async Task<bool> ValidateFileHash(string url, string hash)
    {
        try
        {
            var serverHash = await _client.GetStringAsync(url + "/hash");;
            bool result = string.Equals(hash, serverHash, StringComparison.InvariantCulture);
            if (result == false)
            {
                logger?.ELog("File-Hash mismatch");
            }

            return result;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Gets the file size from the server for the specified URL.
    /// </summary>
    /// <param name="url">The URL of the file for which the size needs to be fetched.</param>
    /// <param name="path">The path of the file</param>
    /// <returns>The size of the file in bytes.</returns>
    private async Task<long> GetFileSize(string url, string path)
    {
        try
        {
            // Create the GET request with required headers
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url + "/size");
            request.Headers.Add("x-executor", executorUid.ToString());
            string json = JsonSerializer.Serialize(new { path });
            request.Content  = new StringContent(json, Encoding.UTF8, "application/json");

            // Send the request and retrieve the response content as a string
            HttpResponseMessage response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            string str = await response.Content.ReadAsStringAsync();
            long.TryParse(str, out long result);
            return result;
        }
        catch (Exception ex)
        {
            return 0;
        }
    }
}
