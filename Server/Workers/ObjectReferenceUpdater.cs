using FileFlows.Server.Controllers;
using FileFlows.ServerShared.Services;
using FileFlows.ServerShared.Workers;

namespace FileFlows.Server.Workers;

/// <summary>
/// Worker that will update all object references if names change
/// </summary>
public class ObjectReferenceUpdater:Worker
{
    private static bool IsRunning = false;
    /// <summary>
    /// Creates a new instance of the Object Reference Updater 
    /// </summary>
    public ObjectReferenceUpdater() : base(ScheduleType.Daily, 1)
    {
    }

    protected override void Execute()
    {
        Run();
    }

    /// <summary>
    /// Runs the updater asynchronously 
    /// </summary>
    internal async Task RunAsync()
    {
        await Task.Delay(1);
        Run();
    }
    
    /// <summary>
    /// Runs the updater
    /// </summary>
    internal void Run()
    {
        if (IsRunning)
            return;
        IsRunning = true;
        try
        {
            DateTime start = DateTime.Now;
            var lfService = new Services.LibraryFileService();
            var libService = new Services.LibraryService();
            var libFiles = lfService.GetAll(null).Result;
            var libraries = libService.GetAll();
            var flows = new Services.FlowService().GetAll();

            var dictLibraries = libraries.ToDictionary(x => x.Uid, x => x.Name);
            var dictFlows = flows.ToDictionary(x => x.Uid, x => x.Name);
            
            Logger.Instance.ILog("Time Taken to prepare for ObjectReference rename: "+ DateTime.Now.Subtract(start));

            foreach (var lf in libFiles)
            {
                bool changed = false;
                if (dictLibraries.ContainsKey(lf.Library.Uid) && lf.Library.Name != dictLibraries[lf.Library.Uid])
                {
                    string oldName = lf.Library.Name;
                    lf.Library.Name = dictLibraries[lf.Library.Uid];
                    Logger.Instance.ILog($"Updating Library name reference '{oldName}' to '{lf.Library.Name}' in file: {lf.Name}");
                    changed = true;
                }

                if (lf.Flow != null && lf.Flow.Uid != Guid.Empty && dictFlows.ContainsKey(lf.Flow.Uid) &&
                    lf.Flow.Name != dictFlows[lf.Flow.Uid])
                {
                    string oldname = lf.Flow.Name;
                    lf.Flow.Name = dictFlows[lf.Flow.Uid];
                    Logger.Instance.ILog($"Updating Flow name reference '{oldname}' to '{lf.Flow.Name}' in file: {lf.Name}");
                    changed = true;
                }

                if (changed)
                    lfService.Update(lf).Wait();
            }

            foreach (var lib in libraries)
            {
                if (dictFlows.ContainsKey(lib.Flow.Uid) && lib.Flow.Name != dictFlows[lib.Flow.Uid])
                {
                    string oldname = lib.Flow.Name;
                    lib.Flow.Name = dictFlows[lib.Flow.Uid];
                    Logger.Instance.ILog($"Updating Flow name reference '{oldname}' to '{lib.Flow.Name}' in library: {lib.Name}");
                    libService.Update(lib);
                }
            }
            Logger.Instance.ILog("Time Taken to complete for ObjectReference rename: "+ DateTime.Now.Subtract(start));
        }
        finally
        {
            IsRunning = false;
        }
    }
}