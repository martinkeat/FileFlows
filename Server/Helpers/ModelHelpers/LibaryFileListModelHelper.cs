using FileFlows.Server.Services;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Helpers.ModelHelpers;

/// <summary>
/// Helpers for Library Files
/// </summary>
public class LibaryFileListModelHelper
{
    /// <summary>
    /// Converts Library Files to Library File List Models
    /// </summary>
    /// <param name="files">the files to convert</param>
    /// <param name="status">the status selected in the UI</param>
    /// <param name="libraries">the libraries in the system</param>
    /// <param name="nodeNames">a dictionary of processing node names</param>
    /// <returns>a list of files in the list model</returns>
    public static IEnumerable<LibaryFileListModel> ConvertToListModel(IEnumerable<LibraryFile> files, FileStatus status, IEnumerable<Library> libraries, Dictionary<Guid, string> nodeNames)
    {
        files = files.ToList();
        var dictLibraries = libraries.ToDictionary(x => x.Uid, x => x);
        return files.Select(x =>
        {
            var item = new LibaryFileListModel
            {
                Uid = x.Uid,
                DisplayName = FileDisplayNameService.GetDisplayName(x.Name, x.RelativePath, x.LibraryName),
                Flow = x.Flow?.Name,
                Library = x.Library?.Name,
                RelativePath = x.RelativePath,
                Name = x.Name,
                OriginalSize = x.OriginalSize,
                Forced = (x.Flags & LibraryFileFlags.ForceProcessing) == LibraryFileFlags.ForceProcessing
            };

            if (status == FileStatus.Unprocessed || status == FileStatus.OutOfSchedule || status == FileStatus.Disabled)
            {
                item.Date = x.DateCreated;
            }
            if (status == FileStatus.OnHold && x.Library != null && dictLibraries.ContainsKey(x.Library.Uid))
            {
                var lib = dictLibraries[x.Library.Uid];
                var scheduledAt = x.DateCreated.AddMinutes(lib.HoldMinutes);
                item.ProcessingTime = scheduledAt.Subtract(DateTime.Now);
            }

            if (status == FileStatus.Processing)
            {
                item.Node = x.Node?.Name;
                item.ProcessingTime = x.ProcessingTime;
                item.Date = x.ProcessingStarted;
            }

            if (status == FileStatus.ProcessingFailed)
            {
                item.Node = x.Node?.Name;
                item.Date = x.ProcessingEnded < x.ProcessingStarted ? x.ProcessingStarted : x.ProcessingEnded;
            }

            if (status == FileStatus.ReprocessByFlow)
            {
                if (x.ProcessOnNodeUid != null && nodeNames.TryGetValue(x.ProcessOnNodeUid.Value, out var name))
                    item.Node = name ?? "Unknown";
                else
                    item.Node = "Unknown";
            }
            
            if (status == FileStatus.Duplicate)
                item.Duplicate = x.Duplicate?.Name;

            if (status == FileStatus.MissingLibrary)
                item.Status = x.Status;

            if (status == FileStatus.Processed)
            {
                item.FinalSize = x.FinalSize;
                item.OutputPath = x.OutputPath;
                item.ProcessingTime = x.ProcessingTime;
                item.Date = x.ProcessingEnded;
                item.Node = x.Node?.Name;
            }
            return item;
        });
    }

}