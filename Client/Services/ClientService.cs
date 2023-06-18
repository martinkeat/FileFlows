using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Caching.Memory;

namespace FileFlows.Client.Services;

/// <summary>
/// Service used for cached communication between the client and server
/// </summary>
public partial class ClientService
{
    /// <summary>
    /// The URI of the WebSocket server.
    /// </summary>
    private readonly string ServerUri;
    
    /// <summary>
    /// Represents the navigation manager used to retrieve the current URL.
    /// </summary>
    private readonly NavigationManager _navigationManager;

    /// <summary>
    /// The instance of <see cref="IMemoryCache"/> used for caching.
    /// </summary>
    private readonly IMemoryCache _cache;

    /// <summary>
    /// Initializes a new instance of the ClientService class.
    /// </summary>
    /// <param name="navigationManager">The navigation manager instance.</param>
    /// <param name="memoryCache">The memory cache instance used for caching.</param>
    public ClientService(NavigationManager navigationManager, IMemoryCache memoryCache)
    {
        #if(DEBUG)
        //ServerUri = "ws://localhost:6868/client-service";
        ServerUri = "http://localhost:6868/client-service";
        #else
        ServerUri = $"{(_navigationManager.BaseUri.Contains("https") ? "wss" : "ws")}://{_navigationManager.BaseUri.Replace("https://", "").Replace("http://", "")}client-service";
        #endif
        _navigationManager = navigationManager; 
        _cache = memoryCache;
        _isConnected = false;
        _ = StartAsync();
    }

    /// <summary>
    /// Gets the executor info
    /// </summary>
    /// <returns>the executor info</returns>
    public Task<List<FlowExecutorInfo>> GetExecutorInfo()
        => GetOrCreate("FlowExecutorInfo", async () =>
        {
            var response = await HttpHelper.Get<List<FlowExecutorInfo>>("/api/worker");
            if (response.Success == false)
                return new List<FlowExecutorInfo>();
            return response.Data;
        }, absExpiration: 10);


    private TItem GetOrCreate<TItem>(object key, Func<TItem> createItem, int slidingExpiration = 5,
        int absExpiration = 30, bool force = false)
    {
        TItem cacheEntry;
        if (force || _cache.TryGetValue(key, out cacheEntry) == false) // Look for cache key.
        {
            // Key not in cache, so get data.
            cacheEntry = createItem();

            var cacheEntryOptions = new MemoryCacheEntryOptions().SetSize(1);
            cacheEntryOptions.SetPriority(CacheItemPriority.High);
            if (slidingExpiration > 0)
                cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromSeconds(slidingExpiration));
            if (absExpiration > 0)
                cacheEntryOptions.SetAbsoluteExpiration(TimeSpan.FromSeconds(absExpiration));

            // Save data in cache.
            _cache.Set(key, cacheEntry, cacheEntryOptions);
        }

        return cacheEntry;
    }
}