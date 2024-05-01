using Microsoft.JSInterop;

namespace FileFlows.Client.Services;

/// <summary>
/// Service for interacting with local storage using JavaScript Interop.
/// </summary>
public class FFLocalStorageService
{
    private readonly IJSRuntime _jsRuntime;
    private bool? _localStorageEnabled;
    private string _accessToken;

    /// <summary>
    /// Gets a value indicating whether local storage is enabled.
    /// </summary>
    public bool LocalStorageEnabled => _localStorageEnabled == true;

    /// <summary>
    /// Initializes a new instance of the <see cref="FFLocalStorageService"/> class.
    /// </summary>
    /// <param name="jsRuntime">The JavaScript Runtime service provided by Blazor.</param>
    public FFLocalStorageService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Sets an item in the local storage.
    /// </summary>
    /// <param name="key">The key of the item to set.</param>
    /// <param name="value">The value of the item to set.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SetItemAsync(string key, object value)
    {
        if (_localStorageEnabled == null)
            await CheckLocalStorageEnabled();
        if(_localStorageEnabled == true)
        {
            if (value != null)
                value = System.Text.Json.JsonSerializer.Serialize(value);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value);
        }
        // Handle case where local storage is not enabled
    }

    /// <summary>
    /// Gets an item from the local storage.
    /// </summary>
    /// <typeparam name="T">The type of the item to get.</typeparam>
    /// <param name="key">The key of the item to get.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the value of the item, or default(T) if not found.</returns>
    public async Task<T> GetItemAsync<T>(string key)
    {
        if (_localStorageEnabled == null)
            await CheckLocalStorageEnabled();
        if(_localStorageEnabled == true)
        {
            try
            {
                var result = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);
                return result != null ? System.Text.Json.JsonSerializer.Deserialize<T>(result) : default;
            }
            catch (Exception)
            {
                return default;
            }
        }
        // Handle case where local storage is not enabled
        return default;
    }

    /// <summary>
    /// Checks if local storage is enabled.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task CheckLocalStorageEnabled()
    {
        try
        {
            _localStorageEnabled = await _jsRuntime.InvokeAsync<bool>("localStorageEnabled");
        }
        catch (Exception)
        {
            // Ignored
        }
    }

    /// <summary>
    /// Gets the access token
    /// </summary>
    /// <returns>the access token</returns>
    public async Task<string?> GetAccessToken()
    {
        if (_localStorageEnabled == null)
            await CheckLocalStorageEnabled();
        if (_localStorageEnabled == true)
            return await GetItemAsync<string>("ACCESS_TOKEN");
        return _accessToken;
    }
    
    /// <summary>
    /// Sets the access token
    /// </summary>
    /// <param name="token">the token</param>
    public async Task SetAccessToken(string token)
    {
        _accessToken = token;
        
        if (_localStorageEnabled == null)
            await CheckLocalStorageEnabled();
        if (_localStorageEnabled == true)
            await SetItemAsync("ACCESS_TOKEN", token);
        await _jsRuntime.InvokeVoidAsync("ff.setAccessToken", token);
    }
}