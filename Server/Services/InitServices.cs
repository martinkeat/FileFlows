namespace FileFlows.Server.Services;

/// <summary>
/// A class to initialize the services
/// </summary>
internal class InitServices
{
    /// <summary>
    /// Initializes the services
    /// </summary>
    public static void Init()
    {
        //Node.FlowExecution.FlowRunnerCommunicator.SignalrUrl = Controllers.NodeController.SignalrUrl;
        //
        // ServerShared.Services.SettingsService.Loader = () => ServiceLoader.Load<SettingsService>();
        // ServerShared.Services.NodeService.Loader = () => ServiceLoader.Load<NodeService>();
        // ServerShared.Services.PluginService.Loader = () => ServiceLoader.Load<PluginService>();
        // ServerShared.Services.FlowService.Loader = () => ServiceLoader.Load<FlowService>();
        // ServerShared.Services.FlowRunnerService.Loader = () => ServiceLoader.Load<FlowRunnerService>();
        // ServerShared.Services.LibraryService.Loader = () => ServiceLoader.Load<LibraryService>();
        // ServerShared.Services.LibraryFileService.Loader = () => ServiceLoader.Load<LibraryFileService>();
        // ServerShared.Services.ScriptService.Loader = () => ServiceLoader.Load<ScriptService>();
        // ServerShared.Services.StatisticService.Loader = () => ServiceLoader.Load<StatisticService>();
        // ServerShared.Services.VariableService.Loader = () => ServiceLoader.Load<VariableService>();
        
        FileDisplayNameService.Initialize();
    }
}