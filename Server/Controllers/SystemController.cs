using System.Diagnostics;
using FileFlows.Server.Authentication;
using FileFlows.Server.Hubs;
using FileFlows.Server.Services;
using FileFlows.Server.Workers;
using FileFlows.ServerShared.Models.StatisticModels;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using FileHelper = FileFlows.ServerShared.Helpers.FileHelper;
using LibraryFileService = FileFlows.Server.Services.LibraryFileService;
using SettingsService = FileFlows.Server.Services.SettingsService;
using StatisticService = FileFlows.Server.Services.StatisticService;

namespace FileFlows.Server.Controllers;

/// <summary>
/// System Controller
/// </summary>
[Route("/api/system")]
[FileFlowsAuthorize]
public class SystemController:BaseController
{
    /// <summary>
    /// Opens a URL in the host OS
    /// </summary>
    /// <param name="url">the URL to open</param>
    [HttpPost("open-url")]
    public void OpenUrl([FromQuery]string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;
        if (url.ToLowerInvariant()?.StartsWith("http") != true)
            return; // dont allow
        if (Application.UsingWebView == false)
            return; // only open if using WebView
        
        if (OperatingSystem.IsWindows())
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        else if (OperatingSystem.IsMacOS())
            Process.Start("open", url);
        else
            Process.Start(new ProcessStartInfo("xdg-open", url));
    }
    
    /// <summary>
    /// Pauses the system
    /// </summary>
    /// <param name="duration">duration in minutes to pause for, any number less than 1 will resume</param>
    [HttpPost("pause")]
    [FileFlowsAuthorize(UserRole.PauseProcessing)]
    public async Task Pause([FromQuery] int duration)
    {
        var service = (SettingsService)ServiceLoader.Load<ISettingsService>();
        var settings = await service.Get();
        if (duration < 1)
        {
            settings.PausedUntil = DateTime.MinValue;
            ClientServiceManager.Instance.SystemPaused(0);
        }
        else
        {
            settings.PausedUntil = DateTime.UtcNow.AddMinutes(duration);
            ClientServiceManager.Instance.SystemPaused(duration);
        }

        await service.Save(settings, await GetAuditDetails());
    }


    /// <summary>
    /// Gets the system information for the FileFlows server,
    /// which includes memory and CPU usage
    /// </summary>
    /// <returns></returns>
    [HttpGet("info")]
    public async Task<SystemInfo> GetSystemInfo()
    {
        SystemInfo info = new ();
        //Process proc = Process.GetCurrentProcess();
        //info.MemoryUsage = proc.PrivateMemorySize64;
        info.MemoryUsage = GC.GetTotalMemory(true);
        info.CpuUsage = await GetCpuPercentage();
        var settings = await ServiceLoader.Load<ISettingsService>().Get();
        info.IsPaused = settings.IsPaused;
        info.PausedUntil = settings.PausedUntil;
        return info;
    }

    /// <summary>
    /// Gets history CPU data of system information
    /// </summary>
    /// <param name="since">data since a date</param>
    /// <returns>the history CPU data</returns>
    [HttpGet("history-data/cpu")]
    public IEnumerable<SystemValue<float>> GetCpuData([FromQuery] DateTime? since = null)
    {
        if (SystemMonitor.Instance == null)
            return new SystemValue<float>[] { };
        if (since != null)
            return SystemMonitor.Instance.CpuUsage.Where(x => x.Time > since);
        var data = SystemMonitor.Instance.CpuUsage;
        return EaseData(data);
    }
    
    /// <summary>
    /// Gets history memory data of system information
    /// </summary>
    /// <param name="since">data since a date</param>
    /// <returns>the history memory data</returns>
    [HttpGet("history-data/memory")]
    public IEnumerable<SystemValue<float>> GetMemoryData([FromQuery] DateTime? since = null)
    {
        if (SystemMonitor.Instance == null)
            return new SystemValue<float>[] { };
        if (since != null)
            return SystemMonitor.Instance.MemoryUsage.Where(x => x.Time > since);
        var data = SystemMonitor.Instance.MemoryUsage;
        return EaseData(data);
    }
    
    /// <summary>
    /// Gets history of open database connections
    /// </summary>
    /// <param name="since">data since a date</param>
    /// <returns>the open database connections data</returns>
    [HttpGet("history-data/database-connections")]
    public IEnumerable<SystemValue<float>> GetOpenDatabaseConnectionsData([FromQuery] DateTime? since = null)
    {
        if (SystemMonitor.Instance == null)
            return new SystemValue<float>[] { };
        if (since != null)
            return SystemMonitor.Instance.OpenDatabaseConnections.Where(x => x.Time > since);
        var data = SystemMonitor.Instance.OpenDatabaseConnections;
        return EaseData(data);
    }
    
    /// <summary>
    /// Gets history temporary storage data of system information
    /// </summary>
    /// <param name="since">data since a date</param>
    /// <returns>the history temporary storage data</returns>
    [HttpGet("history-data/temp-storage")]
    public IEnumerable<SystemValue<long>> GetTempStorageData([FromQuery] DateTime? since = null)
    {
        if (SystemMonitor.Instance == null)
            return new SystemValue<long>[] { };
        if (since != null)
            return SystemMonitor.Instance.TempStorageUsage.Where(x => x.Time > since);
        var data = SystemMonitor.Instance.TempStorageUsage;
        return EaseData(data);
    }

    /// <summary>
    /// Gets history logging storage data of system information
    /// </summary>
    /// <param name="since">data since a date</param>
    /// <returns>the history logging storage data</returns>
    [HttpGet("history-data/log-storage")]
    public IEnumerable<SystemValue<long>> GetLoggingStorageData([FromQuery] DateTime? since = null)
    {
        if (SystemMonitor.Instance == null)
            return new SystemValue<long>[] { };
        if (since != null)
            return SystemMonitor.Instance.LogStorageUsage.Where(x => x.Time > since);
        var data = SystemMonitor.Instance.LogStorageUsage;
        return EaseData(data);
    }

    private IEnumerable<SystemValue<T>> EaseData<T>(IEnumerable<SystemValue<T>> data)
    {
        List<SystemValue<T>> eased = new();
        var dtCutoff = DateTime.UtcNow.AddMinutes(-5);
        var recent = data.Where(x => x.Time > dtCutoff);
        var older = data.Where(x => x.Time <= dtCutoff)
            .GroupBy(x => new DateTime(x.Time.Year, x.Time.Month, x.Time.Day, x.Time.Hour, x.Time.Minute, 0))
            .ToDictionary(x => x.Key, x => x.ToList());
        foreach (var old in older)
        {
            double max = old.Value.Max(x => Convert.ToDouble(x.Value));
            eased.Add(new ()
            {
                Time = old.Key,
                Value = (T)Convert.ChangeType(max, typeof(T))
            });
        }
        eased.AddRange(recent);
        return eased;
    }
    
    /// <summary>
    /// Gets history library processing time data
    /// </summary>
    /// <returns>history library processing time data</returns>
    [HttpGet("history-data/library-processing-time")]
    public async Task<object> GetLibraryProcessingTime()
    {
        var data = (await ServiceLoader.Load<LibraryFileService>().GetLibraryProcessingTimes()).ToArray();
        var dict = data.Select(x => new
        {
            x.Library,
            Value = (x.OriginalSize / 1_000_000d) / x.Seconds
        }).OrderBy(x => x.Value).GroupBy(x => x.Library, x=> x);
        
        return dict.Where(x => x.Count() > 10).Select(x =>
        {
            var list = x.ToList();
            int length = list.Count;
            var median = list[length / 2];
            var lq = list[length / 4];
            var hq = list[length / 4 * 3];
            return new
            {
                x = x.Key?.EmptyAsNull() ?? "Unknown", 
                y = new [] { (int)list[0].Value, (int)lq.Value, (int)median.Value,(int) hq.Value,(int) list[length -1].Value }
            };
        });
    }

    /// <summary>
    /// Gets heat map data for the processing times of the system
    /// </summary>
    /// <returns></returns>
    [HttpGet("history-data/processing-heatmap")]
    public List<HeatmapData> GetProcessingHeatMap()
        => ServiceLoader.Load<StatisticService>().GetHeatMap(Globals.STAT_PROCESSING_TIMES_HEATMAP);

    private async Task<float> GetCpuPercentage()
    {
        try
        {
            var startTime = DateTime.UtcNow;
            var startCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            await Task.Delay(100);

            stopWatch.Stop();
            var endTime = DateTime.UtcNow;
            var endCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;

            var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;
            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

            var cpuUsagePercentage = (float)(cpuUsageTotal * 100);
            return cpuUsagePercentage;
        }
        catch (Exception)
        {
            return 0;
        }
    }

    /// <summary>
    /// Restarts FileFlows server
    /// </summary>
    [HttpPost("restart")]
    [FileFlowsAuthorize(UserRole.Admin)]
    public void Restart()
    {
        if (Application.Docker == false)
        {
            string script = Path.Combine(DirectoryHelper.BaseDirectory, "Server",
                "restart." + (Globals.IsWindows ? "bat" : "sh"));
            if (Globals.IsLinux)
                FileHelper.MakeExecutable(script);
            
            var psi = new ProcessStartInfo(script);
            psi.ArgumentList.Add(Process.GetCurrentProcess().Id.ToString());
            psi.WorkingDirectory = Path.Combine(DirectoryHelper.BaseDirectory, "Server");
            psi.UseShellExecute = true;
            psi.CreateNoWindow = true;
#if(!DEBUG)
            Process.Start(psi);
#endif
        }

        // docker is easy, just stop it and it should auto restart
        WorkerManager.StopWorkers();
        Environment.Exit(99);
    }
}