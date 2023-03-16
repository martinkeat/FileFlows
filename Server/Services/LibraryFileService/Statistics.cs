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
    public async Task<IEnumerable<LibraryStatus>> GetStatus()
    {
        var libraries = await new LibraryController().GetAll();
        var disabled = string.Join(", ",
            libraries.Where(x => x.Enabled == false).Select(x => "'" + x.Uid + "'"));
        int quarter = TimeHelper.GetCurrentQuarter();
        var outOfSchedule = string.Join(", ",
            libraries.Where(x => x.Schedule?.Length != 672 || x.Schedule[quarter] == '0').Select(x => "'" + x.Uid + "'"));

        string sql = @"
        select 
        case 
            when LibraryFile.Status > 0 then LibraryFile.Status " + "\n";
        if (string.IsNullOrEmpty(disabled) == false)
        {
            sql += $" when LibraryFile.Status = 0 and LibraryUid IN ({disabled}) then -2 " + "\n";
        }

        if (string.IsNullOrEmpty(outOfSchedule) == false)
        {
            string flags = $"(Flags & {(int)LibraryFileFlags.ForceProcessing}) <> {(int)LibraryFileFlags.ForceProcessing}";
            sql += $" when LibraryFile.Status = 0 and LibraryUid IN ({outOfSchedule}) and ({flags}) then -1 " + "\n";
        }

        sql += $@"when HoldUntil > {SqlHelper.Now()} then -3
        else LibraryFile.Status
        end as FileStatus,
        count(Uid) as Count
        from LibraryFile 
        group by FileStatus
";
        var statuses = await Database_Fetch<LibraryStatus>(sql);
        foreach (var status in statuses)
            status.Name = Regex.Replace(status.Status.ToString(), "([A-Z])", " $1").Trim();
        return statuses;
    }

    /// <summary>
    /// Gets the shrinkage groups for the files
    /// </summary>
    /// <returns>the shrinkage groups</returns>
    public async Task<List<ShrinkageData>> GetShrinkageGroups()
        => (await Database_Fetch<ShrinkageData>(
            $"select LibraryName as Library, sum(OriginalSize) as OriginalSize, sum(FinalSize) as FinalSize, Count(Uid) as Items " +
            $" from LibraryFile where Status = {(int)FileStatus.Processed}" +
            $" group by LibraryName;")).OrderByDescending(x => x.Items).ToList();

}