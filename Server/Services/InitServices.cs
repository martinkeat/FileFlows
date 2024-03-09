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

        ServerShared.Services.SettingsService.Loader = () => new SettingsService();
        ServerShared.Services.NodeService.Loader = () => ServiceLoader.Load<NodeService>();
        ServerShared.Services.PluginService.Loader = () => new PluginService();
        ServerShared.Services.FlowService.Loader = () => ServiceLoader.Load<FlowService>();
        ServerShared.Services.FlowRunnerService.Loader = () => new FlowRunnerService();
        ServerShared.Services.LibraryService.Loader = () => ServiceLoader.Load<LibraryService>();
        ServerShared.Services.LibraryFileService.Loader = () => ServiceLoader.Load<LibraryFileService>();
        ServerShared.Services.ScriptService.Loader = () => new ScriptService();
        ServerShared.Services.StatisticService.Loader = () => new StatisticService();
        ServerShared.Services.VariableService.Loader = () => ServiceLoader.Load<VariableService>();
        
        FileDisplayNameService.Initialize();
    }
}