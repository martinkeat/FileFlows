namespace FileFlows.Server.Services;

/// <summary>
/// Provides access to services within the application.
/// </summary>
public static class ServiceLoader
{
    /// <summary>
    /// Gets the service provider for accessing registered services.
    /// </summary>
    public static ServiceProvider Provider { get; private set; }

    /// <summary>
    /// Configures and initializes the services.
    /// </summary>
    static void Services()
    {
        Provider = new ServiceCollection()
            // Add logging service
            .AddLogging(options =>
            {
                options.ClearProviders(); // Clear existing logging providers
                options.AddConsole(); // Add console logging provider
            })
            // Add singleton instance of Application class
            .AddSingleton(new Application())
            .AddSingleton(new DashboardService())
            .AddSingleton(ServiceLoader.Load<FlowService>())
            .AddSingleton(ServiceLoader.Load<LibraryService>())
            .AddSingleton(ServiceLoader.Load<NodeService>())
            .AddSingleton(new PluginService())
            .AddSingleton(ServiceLoader.Load<TaskService>())
            .AddSingleton(ServiceLoader.Load<VariableService>())
            .BuildServiceProvider(); // Build the service provider
    }
    
    /// <summary>
    /// Loads the specified service.
    /// </summary>
    /// <typeparam name="T">The type of service to load.</typeparam>
    /// <returns>The loaded service instance.</returns>
    public static T Load<T>()
    {
        return Provider.GetRequiredService<T>(); // Get the required service instance
    }
}
