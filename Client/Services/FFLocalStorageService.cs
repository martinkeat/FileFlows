using Microsoft.JSInterop;

namespace FileFlows.Client.Services;

/// <summary>
/// Service for interacting with local storage using JavaScript Interop.
/// </summary>
public class FFLocalStorageService
{
    private readonly IJSRuntime _jsRuntime;
    private bool? _localStorageEnabled;

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
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value);
        }
        else
        {
            // Handle case where local storage is not enabled
        }
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
            var result = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);
            return result != null ? System.Text.Json.JsonSerializer.Deserialize<T>(result) : default;

        }
        else
        {
            // Handle case where local storage is not enabled
            return default;
        }
    }

    /// <summary>
    /// Checks if local storage is enabled.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task CheckLocalStorageEnabled()
    {
        _localStorageEnabled = await _jsRuntime.InvokeAsync<bool>("localStorageEnabled.check");
    }
}