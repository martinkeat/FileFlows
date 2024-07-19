using FileFlows.DataLayer;
using FileFlows.Managers;
using FileFlows.Plugin;
using FileFlows.ServerShared.Models.StatisticModels;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Formatters;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// Statistic service
/// </summary>
public class StatisticService : IStatisticService
{
    private Dictionary<string, object> CachedData = new();

    private FairSemaphore _semaphore = new(1);

    /// <summary>
    /// Initializes a new instance of the statistic service
    /// </summary>
    public StatisticService()
    {
        CachedData = new StatisticManager().GetAll().Result;
    }

    /// <summary>
    /// Clears DbStatistics based on specified conditions.
    /// </summary>
    /// <param name="name">Optional. The name for which DbStatistics should be cleared.</param>
    public async Task Clear(string? name = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            CachedData.Remove(name);
        else
            CachedData.Clear();
        
        await new StatisticManager().Clear(name);   
    }

    /// <summary>
    /// Gets statistics by name
    /// </summary>
    /// <returns>the matching statistics</returns>
    public Dictionary<string, long> GetRunningTotals(string name)
    {
        if (CachedData.TryGetValue(name, out var o) == false)
            return new();
        if (o is not RunningTotals stat)
            return new ();
        return stat.Data;
    }

    /// <summary>
    /// Gets average by name
    /// </summary>
    /// <returns>the matching average</returns>
    public Dictionary<int, int> GetAverage(string name)
    {
        if (CachedData.TryGetValue(name, out var o) == false)
            return new();
        if (o is not Average stat)
            return new ();
        return stat.Data;
    }
    
    /// <summary>
    /// Gets heatmap by name
    /// </summary>
    /// <returns>the heatmap</returns>
    public List<HeatmapData> GetHeatMap(string name)
    {
        if (CachedData.TryGetValue(name, out var o) == false)
            return new Heatmap().ConvertData();
        
        var data = o as Heatmap ?? new();
        return data.ConvertData();
    }

    /// <summary>
    /// Gets storage saved
    /// </summary>
    /// <returns>the storage saved</returns>
    public List<StorageSavedData> GetStorageSaved()
    {
        if (CachedData.TryGetValue(Globals.STAT_STORAGE_SAVED, out var o) == false)
            return new ();
        return (o as StorageSaved)?.Data ?? new();
    }

    /// <summary>
    /// Records a file has started processing 
    /// </summary>
    public async Task RecordFileStarted()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (CachedData.ContainsKey(Globals.STAT_PROCESSING_TIMES_HEATMAP) == false || CachedData[Globals.STAT_PROCESSING_TIMES_HEATMAP] is Heatmap == false)
            {
                CachedData[Globals.STAT_PROCESSING_TIMES_HEATMAP] = new Heatmap();
            }

            int quarter = TimeHelper.GetCurrentQuarter();
            var heatmap = (Heatmap)CachedData[Globals.STAT_PROCESSING_TIMES_HEATMAP];
            if (heatmap == null)
                return;

            if (heatmap.Data.TryAdd(quarter, 1) == false)
                heatmap.Data[quarter] += 1;
            await new StatisticManager().Update(new()
            {
                Name = Globals.STAT_PROCESSING_TIMES_HEATMAP,
                Type = StatisticType.Heatmap,
                Value = heatmap
            });
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Failed to file started: " + ex.Message + Environment.NewLine + ex.StackTrace);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task RecordRunningTotal(string name, string value)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value))
            return; // bad stat
        await _semaphore.WaitAsync();
        try
        {
            if (CachedData.ContainsKey(name) == false || CachedData[name] is RunningTotals == false)
            {
                CachedData[name] = new RunningTotals();
            }

            var stat = (RunningTotals)CachedData[name];
                
            if (stat.Data.TryAdd(value, 1) == false)
                stat.Data[value] += 1;
            
            await new StatisticManager().Update(new()
            {
                Name = name,
                Type = StatisticType.RunningTotals,
                Value = stat
            });
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Failed to record running total: " + ex.Message + Environment.NewLine + ex.StackTrace);
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    /// <summary>
    /// Records a average 
    /// </summary>
    /// <param name="name">the name of the statistic</param>
    /// <param name="value">the value of the statistic</param>
    public async Task RecordAverage(string name, int value)
    {
        if (string.IsNullOrWhiteSpace(name))
            return; // bad stat
        await _semaphore.WaitAsync();
        try
        {
            if (CachedData.ContainsKey(name) == false || CachedData[name] is Average == false)
            {
                CachedData[name] = new Average();
            }

            var stat = (Average)CachedData[name];
                
            if (stat.Data.TryAdd(value, 1) == false)
                stat.Data[value] += 1;
            
            await new StatisticManager().Update(new()
            {
                Name = name,
                Type = StatisticType.Average,
                Value = stat
            });
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Failed to record average: " + ex.Message + Environment.NewLine + ex.StackTrace);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Resynchronizes the storage saved for all library files
    /// </summary>
    /// <returns>an awaited task</returns>
    public async Task SyncStorageSaved()
    {
        var data = await new LibraryFileManager().GetLibraryFileStats();
        var libraries = await new LibraryManager().GetAll();
        
        await _semaphore.WaitAsync();
        try
        {
            if (CachedData.ContainsKey(Globals.STAT_STORAGE_SAVED) == false ||
                CachedData[Globals.STAT_STORAGE_SAVED] is StorageSaved == false)
            {
                CachedData[Globals.STAT_STORAGE_SAVED] = new StorageSaved();
            }
            var saved = (StorageSaved)CachedData[Globals.STAT_STORAGE_SAVED];

            saved.Data = new();

            foreach (var d in data)
            {
                var lib = libraries.FirstOrDefault(x => x.Uid == d.LibraryUid);
                if (lib == null)
                    continue; // library is gone, don't count this data anymore
                saved.Data.Add(new ()
                {
                    Library = lib.Name,
                    FinalSize = d.SumFinalSize,
                    OriginalSize = d.SumOriginalSize,
                    TotalFiles = d.TotalFiles
                });
            }

            await new StatisticManager().Update(new()
            {
                Name = Globals.STAT_STORAGE_SAVED,
                Type = StatisticType.StorageSaved,
                Value = saved
            });
            Logger.Instance.ILog("Synchronized storage saved statistics:\n" + string.Join("\n", saved.Data.Select(x =>
                $" - {x.Library}\n   - Files: {x.TotalFiles:N0}\n   - Original: {FileSizeFormatter.Format(x.OriginalSize)}\n   - Final: {FileSizeFormatter.Format(x.FinalSize)}")));
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Failed to synchronizing storage saved statistics: " + ex.Message + "\n" + ex.StackTrace);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Records storage saved statistic
    /// </summary>
    /// <param name="library">the name of the library</param>
    /// <param name="originalSize">the original size</param>
    /// <param name="finalSize">the final size</param>
    /// <returns>an awaited task</returns>
    public async Task RecordStorageSaved(string library, long originalSize, long finalSize)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (CachedData.ContainsKey(Globals.STAT_STORAGE_SAVED) == false ||
                CachedData[Globals.STAT_STORAGE_SAVED] is StorageSaved == false)
            {
                CachedData[Globals.STAT_STORAGE_SAVED] = new StorageSaved();
            }

            var saved = (StorageSaved)CachedData[Globals.STAT_STORAGE_SAVED];

            saved.Data ??= new();
            var lib = saved.Data.FirstOrDefault(x => x.Library == library);
            if (lib == null)
            {
                lib = new();
                lib.Library = library;
                saved.Data.Add(lib);
            }

            lib.OriginalSize += originalSize;
            lib.FinalSize += finalSize;

            await new StatisticManager().Update(new()
            {
                Name = Globals.STAT_STORAGE_SAVED,
                Type = StatisticType.StorageSaved,
                Value = saved
            });
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Failed to record storage saved: "+ ex.Message + Environment.NewLine + ex.StackTrace);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}