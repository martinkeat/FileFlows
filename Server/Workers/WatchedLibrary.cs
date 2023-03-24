using FileFlows.Plugin;
using FileFlows.Server.Controllers;
using FileFlows.Shared.Models;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;
using FileFlows.Server.Helpers;
using FileHelper = FileFlows.ServerShared.Helpers.FileHelper;

namespace FileFlows.Server.Workers;

/// <summary>
/// A watched library is a folder that imports files into FileFlows
/// </summary>
public class WatchedLibrary:IDisposable
{
    private FileSystemWatcher Watcher;
    public Library Library { get;private set; } 

    public bool ScanComplete { get;private set; } = false;
    private bool UseScanner = false;
    private bool Disposed = false;

    private Mutex ScanMutex = new Mutex();

    private Queue<string> QueuedFiles = new Queue<string>();

    //private BackgroundWorker worker;
    //private System.Timers.Timer QueueTimer;

    /// <summary>
    /// Constructs a instance of a Watched Library
    /// </summary>
    /// <param name="library">The library to watch</param>
    public WatchedLibrary(Library library)
    {
        this.Library = library;
        this.UseScanner = library.Scan;

        if (Directory.Exists(library.Path) == false)
        {
            Logger.Instance.WLog("Library does not exist, falling back to scanner: " + library.Path);
            this.UseScanner = true;
        }

        if(UseScanner == false)
            SetupWatcher();

        // worker = new BackgroundWorker();
        // worker.DoWork += Worker_DoWork;
        // worker.RunWorkerAsync();
        // QueueTimer = new();
        // QueueTimer.Elapsed += QueueTimerOnElapsed;
        // QueueTimer.AutoReset = false;
        // QueueTimer.Interval = 1;
        // QueueTimer.Start();
    }


    private void LogQueueMessage(string message, Settings? settings = null)
    {
        if (settings == null)
            settings = new SettingsController().Get().Result;

        if (settings?.LogQueueMessages != true)
            return;
        
        Logger.Instance.DLog(message);
    }

    private void QueueTimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        try
        {
            ProcessQueuedItem();
        }
        catch (Exception)
        {
        }
        finally
        {
            if (Disposed == false && QueuedHasItems())
            {
                Thread.Sleep(1000);
                // QueueTimer.Start();
            }
        }
    }

    private SemaphoreSlim processorLock= new (1);
    private void ProcessQueue()
    {
        if (processorLock.Wait(1000) == false)
            return;

        Task.Run(async () =>
        {
            await Task.Delay(100);
            try
            {
                if (QueuedFiles.Any())
                    ProcessQueuedItem();
            }
            catch (Exception)
            {
            }
            finally
            {
                processorLock.Release();
            }
        });
    }

    private void Worker_DoWork(object? sender, DoWorkEventArgs e)
    {
        while (Disposed == false)
        {
            ProcessQueuedItem();
            if(QueuedHasItems() != true)
            {
                LogQueueMessage($"{Library.Name} nothing queued");
                Thread.Sleep(1000);
            }
        }
    }

    private void ProcessQueuedItem()
    {
        try
        {
            string? fullpath;
            lock (QueuedFiles)
            {
                if (QueuedFiles.TryDequeue(out fullpath) == false)
                    return;
            }

            LogQueueMessage($"{Library.Name} Dequeued: {fullpath}");

            if (CheckExists(fullpath) == false)
            {
                Logger.Instance.DLog($"{Library.Name} file does not exist: {fullpath}");
                return;
            }

            if (this.Library.ExcludeHidden)
            {
                if (FileIsHidden(fullpath))
                {
                    LogQueueMessage($"{Library.Name} file is hidden: {fullpath}");
                    return;
                }
            }

            if (IsMatch(fullpath) == false || fullpath.EndsWith("_"))
            {
                LogQueueMessage($"{Library.Name} file does not match pattern or ends with _: {fullpath}");
                return;
            }

            if (fullpath.ToLower().StartsWith(Library.Path.ToLower()) == false)
            {
                Logger.Instance?.ILog($"Library file \"{fullpath}\" no longer belongs to library \"{Library.Path}\"");
                return; // library was changed
            }

            StringBuilder scanLog = new StringBuilder();
            DateTime dtTotal = DateTime.Now;

            FileSystemInfo fsInfo = Library.Folders ? new DirectoryInfo(fullpath) : new FileInfo(fullpath);

            var (knownFile, fingerprint, duplicate) = IsKnownFile(fullpath, fsInfo);
            if (knownFile && duplicate == null)
                return;

            string type = Library.Folders ? "folder" : "file";

            if (Library.Folders && Library.WaitTimeSeconds > 0)
            {
                DirectoryInfo di = (DirectoryInfo)fsInfo;
                try
                {
                    var files = di.GetFiles("*.*", SearchOption.AllDirectories);
                    if (files.Any())
                    {
                        var lastWriteTime = files.Select(x => x.LastWriteTime).Max();
                        if (lastWriteTime > DateTime.Now.AddSeconds(-Library.WaitTimeSeconds))
                        {
                            Logger.Instance.ILog(
                                $"Changes recently written to folder '{di.FullName}' cannot add to library yet");
                            Thread.Sleep(2000);
                            QueueItem(fullpath);
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.ILog(
                        $"Error reading folder '{di.FullName}' cannot add to library yet, will try again: " +
                        ex.Message);
                    Thread.Sleep(2000);
                    QueueItem(fullpath);
                    return;
                }
            }

            Logger.Instance.DLog($"New unknown {type}: {fullpath}");

            if (Library.SkipFileAccessTests == false && Library.Folders == false &&
                CanAccess((FileInfo)fsInfo, Library.FileSizeDetectionInterval).Result == false)
            {
                Logger.Instance.WLog($"Failed access checks for file file: " + fullpath +"\n" +
                                     "These checks can be disabled in library settings, but ensure the flow can read and write to the library.");
                return;
            }


            long size = Library.Folders ? 0 : ((FileInfo)fsInfo).Length;
            var lf = new LibraryFile
            {
                Name = fullpath,
                RelativePath = GetRelativePath(fullpath),
                Status = duplicate != null ? FileStatus.Duplicate : FileStatus.Unprocessed,
                IsDirectory = fsInfo is DirectoryInfo,
                Fingerprint = fingerprint ?? string.Empty,
                OriginalSize = size,
                CreationTime = fsInfo.CreationTime,
                LastWriteTime = fsInfo.LastWriteTime,
                Duplicate = duplicate,
                HoldUntil = Library.HoldMinutes > 0 ? DateTime.Now.AddMinutes(Library.HoldMinutes) : DateTime.MinValue,
                Library = new ObjectReference
                {
                    Name = Library.Name,
                    Uid = Library.Uid,
                    Type = Library.GetType()?.FullName ?? string.Empty
                },
                Order = -1
            };

            LibraryFile result;
            if (knownFile)
            {
                // update the known file, we can't add it again
                result = new LibraryFileController().Update(lf).Result;
            }
            else
            {
                result = new LibraryFileController().Add(lf).Result;
            }

            if (result != null && result.Uid != Guid.Empty)
            {
                SystemEvents.TriggerFileAdded(result, Library);
                Logger.Instance.DLog(
                    $"Time taken \"{(DateTime.Now.Subtract(dtTotal))}\" to successfully add new library file: \"{fullpath}\"");
            }
            else
            {
                Logger.Instance.ELog(
                    $"Time taken \"{(DateTime.Now.Subtract(dtTotal))}\" to fail to add new library file: \"{fullpath}\"");
            }
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Error in queue: " + ex.Message + Environment.NewLine + ex.StackTrace);
        }
    }

    private string GetRelativePath(string fullpath)
    {
        int skip = Library.Path.Length;
        if (Library.Path.EndsWith("/") == false && Library.Path.EndsWith("\\") == false)
            ++skip;

        return fullpath.Substring(skip);
    }

    private bool MatchesDetection(string fullpath)
    {
        FileSystemInfo info = this.Library.Folders ? new DirectoryInfo(fullpath) : new FileInfo(fullpath);
        long size = this.Library.Folders ? Helpers.FileHelper.GetDirectorySize(fullpath) : ((FileInfo)info).Length;

        return MatchesDetection(Library, info, size);
    }

    /// <summary>
    /// Tests if a file in a library matches the detection settings for hte library
    /// </summary>
    /// <param name="library">the library to test</param>
    /// <param name="info">the info for the file or folder to test</param>
    /// <param name="size">the size of the file or folder in bytes</param>
    /// <returns>true if matches detection, otherwise false</returns>
    public static bool MatchesDetection(Library library, FileSystemInfo info, long size)
    {
        if(MatchesValue((int)DateTime.Now.Subtract(info.CreationTime).TotalMinutes, library.DetectFileCreation, library.DetectFileCreationLower, library.DetectFileCreationUpper) == false)
            return false;

        if(MatchesValue((int)DateTime.Now.Subtract(info.LastWriteTime).TotalMinutes, library.DetectFileLastWritten, library.DetectFileLastWrittenLower, library.DetectFileLastWrittenUpper) == false)
            return false;
        
        if(MatchesValue(size, library.DetectFileSize, library.DetectFileSizeLower, library.DetectFileSizeUpper) == false)
            return false;
        
        return true;
        
    }
    
    private static bool MatchesValue(long value, MatchRange range, long low, long high)
    {
        if (range == MatchRange.Any)
            return true;
        
        if (range == MatchRange.GreaterThan)
            return value > low;
        if (range == MatchRange.LessThan)
            return value < low;
        bool between = value >= low && value <= high;
        return range == MatchRange.Between ? between : !between;
    }

    private (bool known, string? fingerprint, ObjectReference? duplicate) IsKnownFile(string fullpath, FileSystemInfo fsInfo)
    {
        var service = new Server.Services.LibraryFileService();
        var knownFile = service.GetFileIfKnown(fullpath);
        string? fingerprint = null;
        if (knownFile != null)
        {
            // FF-393 - check to see if the file has been modified
            var creationDiff = Math.Abs(fsInfo.CreationTime.Subtract(knownFile.CreationTime).TotalSeconds);
            var writeDiff = Math.Abs(fsInfo.LastWriteTime.Subtract(knownFile.LastWriteTime).TotalSeconds);
            bool needsReprocessing = false;
            if (Library.UseFingerprinting && (creationDiff > 5 || writeDiff > 5))
            {
                // file has been modified, recalculate the fingerprint to see if it needs to be reprocessed
                fingerprint = FileHelper.CalculateFingerprint(fullpath);
                if (
                    string.IsNullOrEmpty(knownFile.FinalFingerprint) == false // only check this after FF-425 and we have the final fingerprint  
                    && fingerprint?.EmptyAsNull() != knownFile.Fingerprint?.EmptyAsNull() // fingerprint doesnt match original
                    && fingerprint?.EmptyAsNull() != knownFile.FinalFingerprint // fingerprint doesnt match the final fingerprint we have saved for it
                    )
                {
                    // so if we have the final fingerprint, and the current fingerprint doesnt match the 
                    // 1. original = in case the final action copies or moves a newly created file to another directory,
                    //    then the original file should remain with original fingerprint
                    // 2. final fingerprint = if the user chose to replace original, the original file now should have 
                    //    the final fingerprint as its fingerprint
                    // so this file doesnt match either fingerprints, therefore, we must reprocess it.
                    Logger.Instance.ILog(
                        $"File '{fullpath}' has been modified since last was processed by FileFlows, marking for reprocessing");
                    needsReprocessing = true;
                }
            }

            if (needsReprocessing == false)
            {
                if (Library.ReprocessRecreatedFiles == false || creationDiff < 5)
                {
                    LogQueueMessage($"{Library.Name} skipping known file '{fullpath}'");
                    // we dont return the duplicate here, or the hash since this could trigger a insertion, its already in the db, so we want to skip it
                    return (true, null, null);
                }

                Logger.Instance.DLog(
                    $"{Library.Name} file '{fullpath}' creation time has changed, reprocessing file '{fsInfo.CreationTime}' vs '{knownFile.CreationTime}'");
            }

            knownFile.CreationTime = fsInfo.CreationTime;
            knownFile.LastWriteTime = fsInfo.LastWriteTime;
            knownFile.Status = FileStatus.Unprocessed;
            knownFile.Fingerprint = fingerprint?.EmptyAsNull() ?? ServerShared.Helpers.FileHelper.CalculateFingerprint(fullpath);
            new LibraryFileController().Update(knownFile).Wait();
            // we dont return the duplicate here, or the hash since this could trigger a insertion, its already in the db, so we want to skip it
            return (true, null, null);
        }

        if (Library.UseFingerprinting && Library.Folders == false)
        {
            fingerprint = ServerShared.Helpers.FileHelper.CalculateFingerprint(fullpath);
            if (string.IsNullOrEmpty(fingerprint) == false)
            {
                knownFile = service.GetFileByFingerprint(fingerprint);
                if (knownFile != null)
                {
                    if (knownFile.Name != fullpath && Library.UpdateMovedFiles && knownFile.LibraryUid == Library.Uid)
                    {
                        // library is set to update moved files, so check if the original file still exists
                        if (File.Exists(knownFile.Name) == false)
                        {
                            // original no longer exists, update the original to be this file
                            knownFile.CreationTime = fsInfo.CreationTime;
                            knownFile.LastWriteTime = fsInfo.LastWriteTime;
                            if (knownFile.OutputPath == knownFile.Name)
                                knownFile.OutputPath = fullpath;
                            knownFile.Name = fullpath;
                            knownFile.RelativePath = GetRelativePath(fullpath);
                            new FileFlows.Server.Services.LibraryFileService().UpdateMovedFile(knownFile).Wait();
                            // new LibraryFileController().Update(knownFile).Wait();
                            // file has been updated, we return this is known and tell the scanner to just continue
                            return (true, null, null);
                        }
                    }
                    return (false, fingerprint, new ObjectReference()
                    {
                        Name = knownFile.Name,
                        Type = typeof(LibraryFile).FullName,
                        Uid = knownFile.Uid
                    });
                }
            }
        }

        return (false, fingerprint, null);
    }

    private bool CheckExists(string fullpath)
    {
        try
        {
            if (Library.Folders)
                return Directory.Exists(fullpath);
            return File.Exists(fullpath);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private bool FileIsHidden(string fullpath)
    {
        try
        {
            FileAttributes attributes = File.GetAttributes(fullpath);
            if ((attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                return true;
        }
        catch (Exception)
        {
            return false;
        }

        // recursively search the directories to see if its hidden
        var dir = new FileInfo(fullpath).Directory;
        int count = 0;
        while(dir.Parent != null)
        {
            if (dir.Attributes.HasFlag(FileAttributes.Hidden))
                return true;
            dir = dir.Parent;
            if (++count > 20)
                break; // infinite recrusion safety check
        }
        return false;
    }

    public void Dispose()
    {
        Disposed = true;            
        DisposeWatcher();
        //worker.Dispose();
        //QueueTimer?.Dispose();
    }

    void SetupWatcher()
    {
        DisposeWatcher();

        Watcher = new FileSystemWatcher(Library.Path);
        Watcher.NotifyFilter =
                         //NotifyFilters.Attributes |
                         NotifyFilters.CreationTime |
                         NotifyFilters.DirectoryName |
                         NotifyFilters.FileName |
                         // NotifyFilters.LastAccess |
                         NotifyFilters.LastWrite |
                         //| NotifyFilters.Security
                         NotifyFilters.Size;
        Watcher.IncludeSubdirectories = true;
        Watcher.Changed += Watcher_Changed;
        Watcher.Created += Watcher_Changed;
        Watcher.Renamed += Watcher_Changed;
        Watcher.EnableRaisingEvents = true;

    }

    void DisposeWatcher()
    {
        if (Watcher != null)
        {
            Watcher.Changed -= Watcher_Changed;
            Watcher.Created -= Watcher_Changed;
            Watcher.Renamed -= Watcher_Changed;
            Watcher.EnableRaisingEvents = false;
            Watcher.Dispose();
            Watcher = null;
        }
    }

    private bool IsMatch(string input)
    {
        if (string.IsNullOrWhiteSpace(Library.ExclusionFilter) == false)
        {
            try
            {
                if (new Regex(Library.ExclusionFilter, RegexOptions.IgnoreCase).IsMatch(input))
                    return false;
            }
            catch (Exception) { }
        }
        
        if (string.IsNullOrWhiteSpace(Library.Filter) == false)
        {
            try
            {
                return new Regex(Library.Filter, RegexOptions.IgnoreCase).IsMatch(input);
            }
            catch (Exception) { }
        }
        // default to true
        return true;
    }

    private void Watcher_Changed(object sender, FileSystemEventArgs e)
    {
        try
        {
            if (Library.Folders == false && Directory.Exists(e.FullPath))
            {
                foreach (var file in Directory.GetFiles(e.FullPath, "*.*", SearchOption.AllDirectories))
                {
                    FileChangeEvent(file);
                }
            }
            else
            {
                var file = new FileInfo(e.FullPath);
                if (file.Exists == false)
                    return;
                                    
                long size = file.Length;
                Thread.Sleep(20_000);
                if (size < file.Length)
                    return; // if the file is being copied, we need to wait for that to finish, which will fire a new event

                FileChangeEvent(e.FullPath);
            }
        }
        catch (Exception ex)
        {
            if (ex.Message?.StartsWith("Could not find a part of the path") == true)
                return; // can happen if file is being moved quickly
            Logger.Instance?.ELog("WatchedLibrary.Watched Exception: " + ex.Message + Environment.NewLine + ex.StackTrace);
        }
    }

    private void FileChangeEvent(string fullPath)
    { 
        if (IsMatch(fullPath) == false)
        {
            if (fullPath.Contains("_UNPACK_"))
                return; // dont log this, too many
            return;
        }

        if (QueueContains(fullPath) == false)
        {
            LogQueueMessage($"{Library.Name} queueing file: {fullPath}");
            QueueItem(fullPath);
        }
    }

    internal void UpdateLibrary(Library library)
    {
        this.Library = library;
        if (Directory.Exists(library.Path) == false)
        {
            UseScanner = true;
        }
        else if (UseScanner && library.Scan == false)
        {
            Logger.Instance.ILog($"WatchedLibrary: Library '{library.Name}' switched to watched mode, starting watcher");
            UseScanner = false;
            SetupWatcher();
        }
        else if(UseScanner == false && library.Scan == true)
        {
            Logger.Instance.ILog($"WatchedLibrary: Library '{library.Name}' switched to scan mode, disposing watcher");
            UseScanner = true;
            DisposeWatcher();
        }
        else if(UseScanner == false && Watcher != null && Watcher.Path != library.Path)
        {
            // library path changed, need to change watcher
            Logger.Instance.ILog($"WatchedLibrary: Library '{library.Name}' path changed, updating watched path");
            SetupWatcher(); 
        }

        if (library.Enabled && library.LastScanned < new DateTime(2020, 1, 1) && Directory.Exists(library.Path))
        {
            ScanComplete = false; // this could happen if they click "Rescan" on the library page, this will force a full new scan
            Logger.Instance?.ILog($"WatchedLibrary: Library '{library.Name}' marked for re-scan");
        }
    }

    public void Scan()
    {
        if (ScanMutex.WaitOne(1) == false)
            return;
        try
        {
            if (Library.ScanInterval < 10)
                Library.ScanInterval = 60;

            if (Library.Enabled == false)
                return;

            if (TimeHelper.InSchedule(Library.Schedule) == false)
            {
                Logger.Instance?.ILog($"WatchedLibrary: Library '{Library.Name}' outside of schedule, scanning skipped.");
                return;
            }

            if (string.IsNullOrEmpty(Library.Path) || Directory.Exists(Library.Path) == false)
            {
                Logger.Instance?.WLog($"WatchedLibrary: Library '{Library.Name}' path not found: {Library.Path}");
                return;
            }
            
            Logger.Instance.DLog($"WatchedLibrary: Scan started on '{Library.Name}': {Library.Path}");
            
            int count = 0;
            if (Library.Folders)
            {
                var dirs = new DirectoryInfo(Library.Path).GetDirectories();
                foreach (var dir in dirs)
                {
                    if (QueueContains(dir.FullName) == false)
                    {
                        QueueItem(dir.FullName);
                        ++count;
                    }
                }
            }
            else
            {
                var service = new Server.Services.LibraryFileService();
                var knownFiles = service.GetKnownLibraryFilesWithCreationTimes();

                var files = GetFiles(new DirectoryInfo(Library.Path));
                var settings = new SettingsController().Get().Result;
                foreach (var file in files)
                {
                    if (IsMatch(file.FullName) == false || file.FullName.EndsWith("_"))
                        continue;

                    if (MatchesDetection(file.FullName) == false)
                        continue;
                
                    if (knownFiles.ContainsKey(file.FullName.ToLowerInvariant()))
                    {
                        if (DatesAreSame(file, knownFiles[file.FullName.ToLowerInvariant()]))
                            continue;
                    }


                    if (QueueContains(file.FullName) == false)
                    {
                        LogQueueMessage($"WatchedLibrary: {Library.Name} queueing file for scan: {file.FullName}", settings);
                        QueueItem(file.FullName);
                        ++count;
                    }
                }
            }

            LogQueueMessage($"WatchedLibrary: Files queued for '{Library.Name}': {count} / {QueueCount()}");
            ScanComplete = true;
            
            Library.LastScanned = DateTime.Now;
            new LibraryController().UpdateLastScanned(Library.Uid);
        }
        catch(Exception ex)
        {
            while(ex.InnerException != null)
                ex = ex.InnerException;

            Logger.Instance.ELog("WatchedLibrary: Failed scanning for files: " + ex.Message + Environment.NewLine + ex.StackTrace);
            return;
        }
        finally
        {
            ScanMutex.ReleaseMutex();
        }
    }

    private bool DatesAreSame(FileInfo file, (DateTime CreationTime, DateTime LastWriteTime) knownFile)
    {
        if (datesSame(
                file.CreationTime, knownFile.CreationTime,
                file.LastWriteTime, knownFile.LastWriteTime
            ))
            return true;
        
        if (datesSame(
                file.CreationTimeUtc, knownFile.CreationTime,
                file.LastWriteTimeUtc, knownFile.LastWriteTime
            ))
            return true;
        
        return false;

        bool datesSame(DateTime create1, DateTime create2, DateTime write1, DateTime write2)
        {
            var createDiff = (int) Math.Abs(create1.Subtract(create2).TotalSeconds);
            var writeDiff = (int)Math.Abs(write1.Subtract(write2).TotalSeconds);
            
            bool create = createDiff < 5;
            bool write = writeDiff < 5;
            return create && write;
        }
    }

    private async Task<bool> CanAccess(FileInfo file, int fileSizeDetectionInterval)
    {
        DateTime now = DateTime.Now;
        bool canRead = false, canWrite = false, checkedAccess = false;
        try
        {
            if (file.LastWriteTime > DateTime.Now.AddSeconds(-10))
            {
                // check if the file size changes
                long fs = file.Length;
                if (fileSizeDetectionInterval > 0)
                    await Task.Delay(Math.Min(300, fileSizeDetectionInterval) * 1000);

                if (fs != file.Length)
                {
                    Logger.Instance.ILog("WatchedLibrary: File size has changed, skipping for now: " + file.FullName);
                    return false; // file size has changed, could still be being written too
                }
            }

            checkedAccess = true;

            using (var fs = File.Open(file.FullName, FileMode.Open))
            {
                if(fs.CanRead == false)
                {
                    Logger.Instance.ILog("Cannot read file: " + file.FullName);
                    return false;
                }
                canRead = true;
                if (fs.CanWrite == false)
                {
                    Logger.Instance.ILog("Cannot write file: " + file.FullName);
                    return false;
                }

                canWrite = true;
            }

            return true;
        }
        catch (Exception)
        {
            if (checkedAccess)
            {
                if (canRead == false)
                    Logger.Instance.ILog("Cannot read file: " + file.FullName);
                if (canWrite == false)
                    Logger.Instance.ILog("Cannot write file: " + file.FullName);
            }

            return false;
        }
        finally
        {
            LogQueueMessage($"Time taken \"{(DateTime.Now.Subtract(now))}\" to test can access file: \"{file}\"");
        }
    }

    public List<FileInfo> GetFiles(DirectoryInfo dir)
    {
        var files = new List<FileInfo>();
        try
        {
            foreach (var subdir in dir.GetDirectories())
            {
                files.AddRange(GetFiles(subdir));
            }
            files.AddRange(dir.GetFiles());
        }
        catch (Exception) { }
        return files;
    }
    

    /// <summary>
    /// Safely gets the number of queued items
    /// </summary>
    /// <returns>the number of queued items</returns>
    private int QueueCount()
    {
        lock (QueuedFiles)
        {
            return QueuedFiles.Count();
        }
    }

    /// <summary>
    /// Safely checks if the queue has items
    /// </summary>
    /// <returns>if the queue has items</returns>
    private bool QueuedHasItems()
    {
        lock (QueuedFiles)
        {
            return QueuedFiles.Any();
        }   
    }

    /// <summary>
    /// Safely adds an item to the queue
    /// </summary>
    /// <param name="fullPath">the item to add</param>
    private void QueueItem(string fullPath)
    {
        if (MatchesDetection(fullPath) == false)
        {
            Logger.Instance.DLog($"{Library.Name} file failed file detection: {fullPath}");
            return;
        }
        
        lock (QueuedFiles)
        {
            QueuedFiles.Enqueue(fullPath);
        }

        ProcessQueue();
    }

    /// <summary>
    /// Safely checks if the queue contains an item
    /// </summary>
    /// <param name="item">the item to check</param>
    /// <returns>true if the queue contains it</returns>
    private bool QueueContains(string item)
    {
        lock (QueuedFiles)
        {
            return QueuedFiles.Contains(item);
        }
    }
}
