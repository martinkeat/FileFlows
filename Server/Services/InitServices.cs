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
        ServerShared.Services.NodeService.Loader = () => new NodeService();
        ServerShared.Services.PluginService.Loader = () => new PluginService();
        ServerShared.Services.FlowService.Loader = () => new FlowService();
        ServerShared.Services.FlowRunnerService.Loader = () => new FlowRunnerService();
        ServerShared.Services.LibraryService.Loader = () => new LibraryService();
        ServerShared.Services.LibraryFileService.Loader = () => new LibraryFileService();
        ServerShared.Services.ScriptService.Loader = () => new ScriptService();
        ServerShared.Services.StatisticService.Loader = () => new StatisticService();
        ServerShared.Services.VariableService.Loader = () => new VariableService();
        
        FileDisplayNameService.Initialize();
    }
}