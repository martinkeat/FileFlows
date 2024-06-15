using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Services;

/// <summary>
/// Interface for navigation service, providing methods to navigate to a URL and manage navigation callbacks.
/// </summary>
public interface INavigationService
{   
    /// <summary>
    /// Navigates to the specified URL, invoking registered callbacks before navigating.
    /// </summary>
    /// <param name="url">The URL to navigate to.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the navigation was successful.</returns>
    Task<bool> NavigateTo(string url);
    
    /// <summary>
    /// Registers a callback to be invoked before navigation.
    /// </summary>
    /// <param name="callback">The callback function to register.</param>
    void RegisterNavigationCallback(Func<Task<bool>> callback);
    
    /// <summary>
    /// Unregisters a previously registered navigation callback.
    /// </summary>
    /// <param name="callback">The callback function to unregister.</param>
    void UnRegisterNavigationCallback(Func<Task<bool>> callback);
    
    /// <summary>
    /// Registers a callback to be invoked after navigation.
    /// </summary>
    /// <param name="callback">The callback function to register.</param>
    void RegisterPostNavigateCallback(Func<Task> callback);
    
    /// <summary>
    /// Unregisters a callback to be invoked after navigation.
    /// </summary>
    /// <param name="callback">The callback function to unregister.</param>
    void UnRegisterPostNavigateCallback(Func<Task> callback);
}

/// <summary>
/// Implementation of <see cref="INavigationService"/> to handle navigation and manage navigation callbacks.
/// </summary>
public class NavigationService : INavigationService
{
    private NavigationManager NavigationManager { get; set; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationService"/> class with the specified navigation manager.
    /// </summary>
    /// <param name="navigationManager">The navigation manager to use for navigation operations.</param>
    public NavigationService(NavigationManager navigationManager)
    {
        this.NavigationManager = navigationManager;
    }

    private readonly List<Func<Task<bool>>> _Callbacks = new ();
    private readonly List<Func<Task>> _PostCallbacks = new ();


    /// <inheritdoc />
    public async Task<bool> NavigateTo(string url)
    {
        foreach(var callback in _Callbacks)
        {
            bool ok = await callback.Invoke();
            if (ok == false)
                return false;
        }
        NavigationManager.NavigateTo(url);
        
        // does these in reverse in case the callback removes itself
        for(int i=_PostCallbacks.Count - 1;i>=0;i--)
        {
            _ = _PostCallbacks[i].Invoke();
        }
        return true;
    }

    /// <inheritdoc />
    public void RegisterNavigationCallback(Func<Task<bool>> callback)
    {
        if(_Callbacks.Contains(callback) == false)
            _Callbacks.Add(callback);
    }

    /// <inheritdoc />
    public void UnRegisterNavigationCallback(Func<Task<bool>> callback)
    {
        if(_Callbacks.Contains(callback))
            _Callbacks.Remove(callback);
    }

    /// <inheritdoc />
    public void RegisterPostNavigateCallback(Func<Task> callback)
    {
        if(_PostCallbacks.Contains(callback) == false)
            _PostCallbacks.Add(callback);
    }


    /// <inheritdoc />
    public void UnRegisterPostNavigateCallback(Func<Task> callback)
    {
        if(_PostCallbacks.Contains(callback))
            _PostCallbacks.Remove(callback);
    }
}
