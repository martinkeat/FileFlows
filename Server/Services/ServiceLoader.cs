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
    static ServiceLoader()
    {
        // Add to WebServer to if needed
        Provider = new ServiceCollection()
            .AddSingleton<Application>()
            .AddSingleton<AppSettingsService>()
            .AddSingleton<StartupService>()
            .AddSingleton<DatabaseService>()
            .AddSingleton<SettingsService>()
            .AddSingleton<DatabaseLogService>()
            .AddSingleton<StatisticService>()
            .AddSingleton<DashboardService>()
            .AddSingleton<FlowService>()
            .AddSingleton<LibraryService>()
            .AddSingleton<LibraryFileService>()
            .AddSingleton<NodeService>()
            .AddSingleton<PluginService>()
            .AddSingleton<TaskService>()
            .AddSingleton<UserService>()
            .AddSingleton<VariableService>()
            .AddSingleton<RevisionService>()
            .AddSingleton<FlowRunnerService>()
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
