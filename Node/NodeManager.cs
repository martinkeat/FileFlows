using FileFlows.Node.Workers;
using FileFlows.Server.Workers;
using FileFlows.ServerShared.Models;
using FileFlows.ServerShared.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using FileFlows.Node.Ui;
using FileFlows.ServerShared;
using FileFlows.ServerShared.Helpers;
using FileFlows.ServerShared.Workers;

namespace FileFlows.Node;


/// <summary>
/// A manager that handles registering a node with the FileFlows server
/// </summary>
public class NodeManager
{
    /// <summary>
    /// Gets or sets if this node is registered
    /// </summary>
    public bool Registered { get; private set; }

    /// <summary>
    /// Starts the node processing
    /// </summary>
    public void Start()
    {
        StartWorkers();
    }

    /// <summary>
    /// Stops the node processing
    /// </summary>
    public void Stop()
    {
        WorkerManager.StopWorkers();
    }

    /// <summary>
    /// Starts the node workers
    /// </summary>
    private void StartWorkers()
    {
        Shared.Logger.Instance?.ILog("Starting workers");
        var updater = new NodeUpdater();
        
        if (updater.RunCheck())
            return;

        Version nodeVersion = Globals.Version;

        var flowWorker = new FlowWorker(AppSettings.Instance.HostName)
        {
            IsEnabledCheck = () =>
            {
                if (this.Registered == false)
                {
                    Logger.Instance?.ILog($"Node not registered, Flow Worker skip running.");
                    return false;
                }

                if (AppSettings.IsConfigured() == false)
                {
                    Logger.Instance?.ILog($"Node not configured, Flow Worker skip running.");
                    return false;
                }

                var nodeService = new NodeService();
                try
                {
                    var settings = nodeService.GetByAddressAsync(AppSettings.Instance.HostName).Result;
                    if (settings == null)
                    {
                        Logger.Instance.ELog("Failed getting settings for node: " + AppSettings.Instance.HostName);
                        return false;
                    }

                    AppSettings.Instance.Enabled = settings.Enabled;
                    AppSettings.Instance.Runners = settings.FlowRunners;
                    AppSettings.Instance.TempPath = settings.TempPath;
                    AppSettings.Instance.Save();

                    var serverVersion = new SystemService().GetVersion().Result;
                    if (serverVersion != nodeVersion)
                    {
                        Logger.Instance?.ILog($"Node version '{nodeVersion}' does not match server version '{serverVersion}'");
                        NodeUpdater.CheckForUpdate();
                        return false;
                    }

                    return AppSettings.Instance.Enabled;
                }
                catch (Exception ex)
                {
                    if (ex.Message?.Contains("502 Bad Gateway") == true)
                        Logger.Instance?.ELog("Failed checking enabled: Unable to reach FileFlows Server.");
                    else
                        Logger.Instance?.ELog("Failed checking enabled: " + ex.Message + Environment.NewLine +
                                              ex.StackTrace);
                }

                return false;
            }
        };
        
        WorkerManager.StartWorkers(
            flowWorker, 
            updater, 
            new RestApiWorker(),
            new LogFileCleaner(),
            new TempFileCleaner(AppSettings.Instance.HostName), 
            new SystemStatisticsWorker(),
            new ConfigCleaner()
        );
    }

    
    /// <summary>
    /// Registers the node with the server
    /// </summary>
    /// <returns>whether or not it was registered</returns>
    public async Task<bool> Register()
    {
        string path = DirectoryHelper.BaseDirectory;

        bool windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        List<RegisterModelMapping> mappings = new List<RegisterModelMapping>
            {
                new()
                {
                    Server = "ffmpeg",
                    Local =  Globals.IsDocker ? "/usr/local/bin/ffmpeg" :
                             windows ? Path.Combine(path, "Tools", "ffmpeg.exe") : "/usr/local/bin/ffmpeg"
                }
            };
        if (AppSettings.EnvironmentalMappings?.Any() == true)
        {
            Logger.Instance.ILog("Environmental mappings found, adding those");
            mappings.AddRange(AppSettings.EnvironmentalMappings);
        }

        if (AppSettings.EnvironmentalRunnerCount != null)
            AppSettings.Instance.Runners = AppSettings.EnvironmentalRunnerCount.Value;

        if (AppSettings.EnvironmentalEnabled != null)
            AppSettings.Instance.Enabled = AppSettings.EnvironmentalEnabled.Value;

        if (string.IsNullOrEmpty(AppSettings.Instance.TempPath))
            AppSettings.Instance.TempPath = Globals.IsDocker ? "/temp" : Path.Combine(DirectoryHelper.BaseDirectory, "Temp");

        var settings = AppSettings.Instance;
        var nodeService = new NodeService();
        Shared.Models.ProcessingNode result;
        try
        {
            result = await nodeService.Register(settings.ServerUrl, settings.HostName, settings.TempPath, settings.Runners, settings.Enabled, mappings);
            if (result == null)
            {
                this.Registered = false;
                return false;
            }
        }
        catch (Exception ex)
        {
            Shared.Logger.Instance?.ELog("Failed to register with server: " + ex.Message);
            this.Registered = false;
            return false;
        }

        Service.ServiceBaseUrl = settings.ServerUrl;
        if (Service.ServiceBaseUrl.EndsWith("/"))
            Service.ServiceBaseUrl = Service.ServiceBaseUrl.Substring(0, Service.ServiceBaseUrl.Length - 1);

        Shared.Logger.Instance?.ILog("Successfully registered node");
        settings.Enabled = result.Enabled;
        settings.Runners = result.FlowRunners;
        settings.Save();
        this.Registered = true;
        return true;
    }
}
