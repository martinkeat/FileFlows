namespace FileFlows.ServerShared.Services;

/// <summary>
/// Shared service loader
/// </summary>
public static class SharedServiceLoader
{
    /// <summary>
    /// Gets or sets the loader
    /// </summary>
    public static Func<Type, object> Loader { get; set; } = null!;
    
}

/// <summary>
/// Provides access to services within the application.
/// </summary>
internal static class ServiceLoader
{
    /// <summary>
    /// Loads the specified service.
    /// </summary>
    /// <typeparam name="T">The type of service to load.</typeparam>
    /// <returns>The loaded service instance.</returns>
    internal static T Load<T>() where T : notnull
        => (T)SharedServiceLoader.Loader(typeof(T));
}
