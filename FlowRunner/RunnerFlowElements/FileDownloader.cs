using FileFlows.Plugin;
using FileFlows.RemoteServices;
using FileFlows.ServerShared.FileServices;
using FileFlows.ServerShared.Services;

namespace FileFlows.FlowRunner.RunnerFlowElements;

/// <summary>
/// Flow element that downloads the file from the remote server
/// </summary>
public class FileDownloader : Node
{
    /// <summary>
    /// The run instance running this
    /// </summary>
    private readonly RunInstance runInstance;
    
    /// <summary>
    /// Creates a new instance of the file downloader
    /// </summary>
    /// <param name="runInstance">the run instance running this</param>
    public FileDownloader(RunInstance runInstance)
    {
        this.runInstance = runInstance;
    }
    
    /// <summary>
    /// Executes the flow element
    /// </summary>
    /// <param name="args">the node parameters</param>
    /// <returns>the next output to call</returns>
    public override int Execute(NodeParameters args)
    {
        string dest = Path.Combine(args.TempPath, new FileInfo(args.LibraryFileName).Name);
        var downloader = new ServerShared.FileServices.FileDownloader(args.Logger, RemoteService.ServiceBaseUrl, 
            runInstance.Uid, RemoteService.AccessToken, RemoteService.NodeUid);
        downloader.OnProgress += (percent, eta, speed) =>
        {
            args.PartPercentageUpdate(percent);
            
            args.RecordAdditionalInfo("Progress", percent + "%", 1, new TimeSpan(0, 1, 0));
            args.RecordAdditionalInfo("Speed", speed, 1, new TimeSpan(0, 1, 0));
            args.RecordAdditionalInfo("ETA", eta == null ? null : Plugin.Helpers.TimeHelper.ToHumanReadableString(eta.Value),1, new TimeSpan(0, 1, 0));
        };
        
        var result = downloader.DownloadFile(args.LibraryFileName, dest).Result;
        if (result.IsFailed)
        {
            args.Logger?.ELog("Failed to remotely download file: " + result.Error);
            return -1;
        }

        args.SetWorkingFile(dest);
        return 1;
    }
}