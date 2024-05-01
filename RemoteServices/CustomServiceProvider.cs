namespace FileFlows.RemoteServices;

/// <summary>
/// Custom service provider for deferred service instantiation.
/// </summary>
public class CustomServiceProvider
{
    private readonly Dictionary<Type, Func<object>> _serviceFactories = new Dictionary<Type, Func<object>>();
    private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
    private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
    private bool _isBuilt = false;

    /// <summary>
    /// Registers a singleton service with a factory method.
    /// </summary>
    /// <typeparam name="TService">The type of the service interface.</typeparam>
    /// <param name="factory">The factory method to create the service instance.</param>
    public CustomServiceProvider AddSingleton<TService>(Func<TService> factory) where TService : class
    {
        if (factory == null)
            throw new ArgumentNullException(nameof(factory));

        _lock.Wait();
        try
        {
            _serviceFactories[typeof(TService)] = factory;
            return this;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Builds the service provider by initializing all registered services.
    /// </summary>
    public CustomServiceProvider BuildServiceProvider()
    {
        _lock.Wait();
        try
        {
            if (!_isBuilt)
            {
                foreach (var factory in _serviceFactories)
                {
                    _services[factory.Key] = factory.Value.Invoke();
                }
                _isBuilt = true;
            }
            return this;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Gets the registered service instance.
    /// </summary>
    /// <typeparam name="TService">The type of the service interface.</typeparam>
    /// <returns>The service instance.</returns>
    public TService? GetService<TService>() where TService : class
    {
        if (!_isBuilt)
        {
            throw new InvalidOperationException("ServiceProvider is not built yet. Call BuildServiceProvider() first.");
        }

        _lock.Wait();
        try
        {
            if (_services.TryGetValue(typeof(TService), out var service))
            {
                return (TService)service;
            }
            return null;
        }
        finally
        {
            _lock.Release();
        }
    }
}
