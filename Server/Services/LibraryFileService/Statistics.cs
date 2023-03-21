using System.Text.RegularExpressions;
using FileFlows.Server.Controllers;
using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// Statistics functions for Library File Service
/// </summary>
public partial class LibraryFileService
{
    
    /// <summary>
    /// Gets the processing time for each library file 
    /// </summary>
    /// <returns>the processing time for each library file</returns>
    public async Task<IEnumerable<LibraryFileProcessingTime>> GetLibraryProcessingTimes()
    {
        string sql = @$"select 
LibraryName as {nameof(LibraryFileProcessingTime.Library)},
OriginalSize, " +
                     SqlHelper.TimestampDiffSeconds("ProcessingStarted", "ProcessingEnded", nameof(LibraryFileProcessingTime.Seconds)) + 
                     @" from LibraryFile 
where Status = 1 and ProcessingEnded > ProcessingStarted;";

        return await Database_Fetch<LibraryFileProcessingTime>(sql);
    }

    /// <summary>
    /// Gets data for a days/hours heatmap.  Where the list is the days, and the dictionary is the hours with the count as the values
    /// </summary>
    /// <returns>heatmap data</returns>
    public async Task<List<Dictionary<int, int>>> GetHourProcessingTotals()
    {
        string sql = @"select " +
                     SqlHelper.DayOfWeek("ProcessingStarted","day") + ", " + 
                     SqlHelper.Hour("ProcessingStarted","hour") + ", " +
                     " count(Uid) as count " + 
                     " from LibraryFile where Status = 1 AND ProcessingStarted > '2000-01-01 00:00:00' " +
                     " group by " + SqlHelper.DayOfWeek("ProcessingStarted") + "," +
                     SqlHelper.Hour("ProcessingStarted");

        List<(int day, int hour, int count)>
            data = (await Database_Fetch<(int day, int hour, int count)>(sql)).ToList();


        var days = new List<Dictionary<int, int>>();
        for (int i = 0; i < 7; i++)
        {
            var results = new Dictionary<int, int>();
            for (int j = 0; j < 24; j++)
            {
                // sun=1, mon=2, sat =7
                // so we use x.day - 1 here to convert sun=0
                int count = data.Where(x => (x.day - 1) == i && x.hour == j).Select(x => x.count).FirstOrDefault();
                results.Add(j, count);
            }

            days.Add(results);
        }

        return days;
    }

    /// <summary>
    /// Gets the library status overview
    /// </summary>
    /// <returns>the library status overview</returns>
    public IEnumerable<LibraryStatus> GetStatus()
    {
        var libraries = new LibraryService().GetAll();
        var disabled = libraries.Where(x => x.Enabled == false).Select(x => x.Uid);
        List<Guid> libraryUids = libraries.Select(x => x.Uid).ToList();
        int quarter = TimeHelper.GetCurrentQuarter();
        var outOfSchedule = libraries.Where(x => x.Schedule?.Length != 672 || x.Schedule[quarter] == '0').Select(x => x.Uid);

        FileStatus unknown = (FileStatus)999;
        return Data.Select(x =>
        {
            if (x.Value.LibraryUid == null)
                return unknown;
            if (libraryUids.Contains(x.Value.LibraryUid.Value) == false)
                return unknown;
            
            if ((int)x.Value.Status > 0)
                return x.Value.Status;
            if ((x.Value.Flags & LibraryFileFlags.ForceProcessing) == LibraryFileFlags.ForceProcessing)
                return FileStatus.Unprocessed;
            if (disabled.Contains(x.Value.LibraryUid.Value))
                return FileStatus.Disabled;
            if (outOfSchedule.Contains(x.Value.LibraryUid.Value))
                return FileStatus.OutOfSchedule;
            if (x.Value.HoldUntil > DateTime.Now)
                return FileStatus.OnHold;
            return FileStatus.Unprocessed;
        }).Where(x => x != unknown).GroupBy(x => x).Select(x => new LibraryStatus()
        {
            Count = x.Count(),
            Name = Regex.Replace(x.Key.ToString(), "([A-Z])", " $1").Trim(),
            Status = x.Key
        }).ToList();
    }

    /// <summary>
    /// Gets the shrinkage groups for the files
    /// </summary>
    /// <returns>the shrinkage groups</returns>
    public List<ShrinkageData> GetShrinkageGroups()
    {
        var libraries = Data.Where(x => x.Value.Status == FileStatus.Processed)
            .Select(x => x.Value)
            .GroupBy(x => x.LibraryName)
            .Select(x => new ShrinkageData()
            {
                Library = x.Key,
                Items = x.Count(),
                FinalSize = x.Sum(y => y.FinalSize),
                OriginalSize = x.Sum(y=> y.OriginalSize)
            }).ToList();
        return libraries;
    }

}