using FileFlows.Plugin;
using FileFlows.ServerShared.FileServices;
using FileFlows.ServerShared.Services;

namespace FileFlows.FlowRunner.RunnerFlowElements;

/// <summary>
/// Flow element that downloads the file from the remote server
/// </summary>
public class FileDownloader : Node
{
    /// <summary>
    /// Executes the flow element
    /// </summary>
    /// <param name="args">the node parameters</param>
    /// <returns>the next output to call</returns>
    public override int Execute(NodeParameters args)
    {
        string dest = Path.Combine(args.TempPath, new FileInfo(args.LibraryFileName).Name);
        var downloader = new ServerShared.FileServices.FileDownloader(args.Logger, Service.ServiceBaseUrl, Program.Uid);
        downloader.OnProgress += (percent, eta, speed) =>
        {
            args.PartPercentageUpdate(percent);
            
            args.RecordAdditionalInfo("Progress", percent + "%", new TimeSpan(0, 1, 0));
            args.RecordAdditionalInfo("Speed", speed, new TimeSpan(0, 1, 0));
            args.RecordAdditionalInfo("ETA", eta == null ? null : Plugin.Helpers.TimeHelper.ToHumanReadableString(eta.Value), new TimeSpan(0, 1, 0));
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