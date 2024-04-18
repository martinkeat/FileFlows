using System.Collections.Concurrent;
using System.Diagnostics;
using FileFlows.Server.Services;
using FileFlows.ServerShared.Models;
using FileFlows.ServerShared.Workers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Workers;

/// <summary>
/// Worker that monitors system information
/// </summary>
public class SystemMonitor:Worker
{
    public readonly FixedSizedQueue<SystemValue<float>> CpuUsage = new (2000);
    public readonly FixedSizedQueue<SystemValue<float>> MemoryUsage = new (2000);
    public readonly FixedSizedQueue<SystemValue<float>> OpenDatabaseConnections = new (2000);
    public readonly FixedSizedQueue<SystemValue<long>> TempStorageUsage = new(2000);
    public readonly FixedSizedQueue<SystemValue<long>> LogStorageUsage = new(2000);
    private readonly Dictionary<Guid, NodeSystemStatistics> NodeStatistics = new();
    

    /// <summary>
    /// Gets the instance of the system monitor
    /// </summary>
    public static SystemMonitor Instance { get; private set; }

    /// <summary>
    /// The app settings service
    /// </summary>
    private AppSettingsService appSettingsService;

    /// <summary>
    /// Database service
    /// </summary>
    private DatabaseService dbService;
    
    public SystemMonitor() : base(ScheduleType.Second, 10)
    {
        Instance = this;
        appSettingsService = ServiceLoader.Load<AppSettingsService>();
        dbService = ServiceLoader.Load<DatabaseService>();
    }

    protected override void Execute()
    {
        var taskCpu = GetCpu();
        var taskTempStorage = GetTempStorageSize();
        var taskLogStorage = GetLogStorageSize();
        var taskOpenDatabaseConnections = GetOpenDatabaseConnections();

        MemoryUsage.Enqueue(new()
        {
            Value = GC.GetTotalMemory(true)
        });

        Task.WaitAll(taskCpu, taskTempStorage, taskOpenDatabaseConnections);
        CpuUsage.Enqueue(new ()
        {
            Value = taskCpu.Result
        });
        
        TempStorageUsage.Enqueue(new ()
        {
            Value = taskTempStorage.Result
        });
        LogStorageUsage.Enqueue(new ()
        {
            Value = taskLogStorage.Result
        });
        //if (appSettingsService.Settings.DatabaseType != DatabaseType.Sqlite)
        {
            OpenDatabaseConnections.Enqueue(new()
            {
                Value = taskOpenDatabaseConnections.Result
            });
        }
    }

    private async Task<float> GetCpu()
    {
        await Task.Delay(1);
        List<float> records = new List<float>();
        int max = 7;
        for (int i = 0; i <= max; i++)
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

            records.Add((float)(cpuUsageTotal * 100));
            if (i == max)
                break;
            await Task.Delay(1000);
        }

        return records.Max();
    }

    /// <summary>
    /// Gets the open database connections
    /// </summary>
    /// <returns>the number of open database connections</returns>
    private async Task<int> GetOpenDatabaseConnections()
    {
        // if (appSettingsService.Settings.DatabaseType == DatabaseType.Sqlite)
        //     return 0;
        
        await Task.Delay(1);
        List<int> records = new List<int>();
        int max = 70;
        for (int i = 0; i <= max; i++)
        {
            int count = dbService.GetOpenConnections();
                records.Add(count);
            if (i == max)
                break;
            await Task.Delay(100);
        }
        
        return records.Max();
    }
    private async Task<long> GetTempStorageSize()
    {
        var node = await new FileFlows.Server.Services.NodeService().GetServerNodeAsync();
        var tempPath = node?.TempPath;
        return GetDirectorySize(tempPath);
    }

    private async Task<long> GetLogStorageSize()
    {
        await Task.Delay(1);
        string logPath = DirectoryHelper.LoggingDirectory;
        string libFileLogPath = DirectoryHelper.LibraryFilesLoggingDirectory;
        if(libFileLogPath == null || logPath.Contains(libFileLogPath))
            return GetDirectorySize(libFileLogPath, logginDir: true);
        if(libFileLogPath == null || libFileLogPath.Contains(logPath))
            return GetDirectorySize(logPath, logginDir: true);
        long logPathLength = GetDirectorySize(logPath, true);
        long libFileLogPathLength = GetDirectorySize(libFileLogPath, true);
        return logPathLength + libFileLogPathLength;
    }
    
    
    private long GetDirectorySize(string path, bool logginDir = false)
    {
        long size = 0;
        try
        {
            if (string.IsNullOrEmpty(path) == false)
            {
                try
                {
                    var dir = new DirectoryInfo(path);
                    if (dir.Exists)
                        size = dir.EnumerateFiles("*.*", SearchOption.AllDirectories).Sum(x => x.Length);
                }
                catch (Exception)
                {
                }
            }

            lock (NodeStatistics)
            {
                foreach (var nts in NodeStatistics.Values)
                {
                    if (nts.RecordedAt > DateTime.UtcNow.AddMinutes(-5))
                    {
                        var npath = logginDir ? nts.LogDirectorySize : nts.TemporaryDirectorySize;
                        size += npath.Size;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Instance.WLog($"Failed reading directory '{path} size: " + ex.Message);
        }

        return size;
    }

    /// <summary>
    /// Records the node system statistics to the server
    /// </summary>
    /// <param name="args">the node system statistics</param>
    public void Record(NodeSystemStatistics args)
    {
        args.RecordedAt = DateTime.UtcNow;
        lock (NodeStatistics)
        {
            NodeStatistics[args.Uid] = args;
        }
    }
}


/// <summary>
/// A queue of fixed size
/// </summary>
/// <typeparam name="T">the type to queue</typeparam>
public class FixedSizedQueue<T> : ConcurrentQueue<T>
{
    private readonly object syncObject = new object();

    /// <summary>
    /// Gets or sets the max queue size
    /// </summary>
    public int Size { get; private set; }

    /// <summary>
    /// Constructs an instance of a fixed size queue
    /// </summary>
    /// <param name="size">the size of the queue</param>
    public FixedSizedQueue(int size)
    {
        Size = size;
    }

    /// <summary>
    /// Adds a item to the queue
    /// </summary>
    /// <param name="obj">the item to add</param>
    public new void Enqueue(T obj)
    {
        base.Enqueue(obj);
        lock (syncObject)
        {
            while (base.Count > Size)
            {
                base.TryDequeue(out _);
            }
        }
    }
}