using FileFlows.Server.Controllers;
using FileFlows.ServerShared.Services;
using FileFlows.ServerShared.Workers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Workers;

/// <summary>
/// Worker to monitor libraries
/// </summary>
public class LibraryWorker : Worker
{
    
    private Dictionary<string, WatchedLibrary> WatchedLibraries = new ();

    /// <summary>
    /// Gets the instance of the library worker
    /// </summary>
    private static LibraryWorker Instance;

    /// <summary>
    /// Creates a new instance of the library worker
    /// </summary>
    public LibraryWorker() : base(ScheduleType.Minute, 1)
    {
        Instance = this;
    }

    public override void Start()
    {
        base.Start();
        UpdateLibraries();
    }

    public override void Stop()
    {
        base.Stop();
    }
    
    private DateTime LibrariesLastUpdated = DateTime.MinValue;

    /// <summary>
    /// Triggers a scan now
    /// </summary>
    public static void ScanNow() => Instance?.Trigger();

    /// <summary>
    /// Updates the libraries being watched
    /// </summary>
    public static void UpdateLibraries() => Instance?.UpdateLibrariesInstance();
    

    private void Watch(params Library[] libraries)
    {
        foreach(var lib in libraries)
        {
            string key = lib.Uid + ":" + lib.Path;
            lock (WatchedLibraries)
            {
                if (WatchedLibraries.ContainsKey(key))
                    continue;
                WatchedLibraries.Add(key, new WatchedLibrary(lib));
            }
        }
    }

    private void Unwatch(params string[] keys)
    {
        foreach(string key in keys)
        {
            if (WatchedLibraries.ContainsKey(key) == false)
                continue;
            var watcher = WatchedLibraries[key];
            watcher.Dispose();
            WatchedLibraries.Remove(key);
            watcher = null;
        }
    }

    /// <summary>
    /// Updates the libraries being watched
    /// </summary>
    private void UpdateLibrariesInstance()
    {
        Logger.Instance.DLog("LibraryWorker: Updating Libraries");
        var libraries = new Services.LibraryService().GetAll();
        var libraryUids = libraries.Select(x => x.Uid + ":" + x.Path).ToList();            

        Watch(libraries.Where(x => WatchedLibraries.ContainsKey(x.Uid + ":" + x.Path) == false).ToArray());
        Unwatch(WatchedLibraries.Keys.Where(x => libraryUids.Contains(x) == false).ToArray());

        foreach (var libwatcher in WatchedLibraries.Values)
        {   
            var library = libraries.FirstOrDefault(x => libwatcher.Library.Uid == x.Uid);
            if (library == null)
                continue;
            libwatcher.UpdateLibrary(library);
        }
     
        LibrariesLastUpdated = DateTime.Now;
    }

    protected override void Execute()
    {
        if(LibrariesLastUpdated < DateTime.Now.AddHours(-1))
            UpdateLibrariesInstance();

        foreach(var libwatcher in WatchedLibraries.Values)
        {
            var library = libwatcher.Library;
            if (library.FullScanIntervalMinutes == 0)
                library.FullScanIntervalMinutes = 60;
            if (libwatcher.ScanComplete == false)
            {
                // hasn't been scanned yet, we scan when the app starts or library is first added
            }
            else if (library.Scan == false)
            {
                if (library.FullScanDisabled)
                {
                    Logger.Instance.DLog($"LibraryWorker: Library '{library.Name}' full scan disabled");
                    continue;
                }

                // need to check full scan interval
                if (library.LastScannedAgo.TotalMinutes < library.FullScanIntervalMinutes)
                {
                    Logger.Instance.DLog($"LibraryWorker: Library '{library.Name}' was scanned recently {library.LastScannedAgo} (full scan interval {library.FullScanIntervalMinutes} minutes)");
                    continue;
                }
            }
            else if (library.LastScannedAgo.TotalSeconds < library.ScanInterval)
            {
                Logger.Instance.DLog($"LibraryWorker: Library '{library.Name}' was scanned recently {library.LastScannedAgo} ({(new TimeSpan(library.ScanInterval * TimeSpan.TicksPerSecond))}");
                continue;
            }

            Logger.Instance.DLog($"LibraryWorker: Library '{library.Name}' calling scan " +
                                 $"(Scan complete: {libwatcher.ScanComplete}) " +
                                 $"(Library Scan: {library.Scan} " +
                                 $"(last scanned: {library.LastScannedAgo}) " +
                                 $"(Full Scan interval: {library.FullScanIntervalMinutes})");

            Task.Run(async () =>
            {
                await Task.Delay(1);
                libwatcher.Scan();
            });
        }
    }

    /// <summary>
    /// Resets processing of all library files that are currently marked as Processing to Unprocessed
    /// </summary>
    /// <param name="internalOnly">If only the internal processing node should be reset</param>
    internal static void ResetProcessing(bool internalOnly = true)
    {
        var service = new Server.Services.LibraryFileService();
        if (internalOnly)
        {
            service.ResetProcessingStatus(Globals.InternalNodeUid).Wait();
        }
        else
        {
            // special case can use dbhelper directly
            // this is called at the start up of FileFlows
            service.ResetProcessingStatus().Wait();
        }
    }
}
