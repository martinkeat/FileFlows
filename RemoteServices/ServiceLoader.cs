namespace FileFlows.RemoteServices;

/// <summary>
/// Service Loader
/// </summary>
public static class ServiceLoader
{
    /// <summary>
    /// Gets the service provider for accessing registered services.
    /// </summary>
    public static CustomServiceProvider Provider { get; private set; }

    /// <summary>
    /// Configures and initializes the services.
    /// </summary>
    static ServiceLoader()
    {
        // Add to WebServer to if needed
        Provider = new CustomServiceProvider()
            .AddSingleton<IFlowRunnerService>(() => new FlowRunnerService())
            .AddSingleton<ILibraryFileService>(() => new LibraryFileService())
            .AddSingleton<ILogService>(() => new LogService())
            .AddSingleton<INodeService>(() => new NodeService())
            .AddSingleton<ISettingsService>(() => new SettingsService())
            .AddSingleton<IStatisticService>(() => new StatisticService())
            .AddSingleton<IVariableService>(() => new VariableService())
            .BuildServiceProvider(); // Build the service provider
    }
    
    /// <summary>
    /// Loads the specified service.
    /// </summary>
    /// <typeparam name="T">The type of service to load.</typeparam>
    /// <returns>The loaded service instance.</returns>
    public static T Load<T>() where T : class
    {
        return Provider.GetService<T>(); // Get the required service instance
    }   
}