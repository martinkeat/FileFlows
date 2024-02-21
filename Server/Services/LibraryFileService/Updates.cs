using FileFlows.Server.Helpers;
using FileFlows.Server.Hubs;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// Search functions for Library File Service
/// </summary>
public partial class LibraryFileService
{
    /// <summary>
    /// Reprocess all files based on library UIDs
    /// </summary>
    /// <param name="libraryUids">an array of UID of the libraries to reprocess</param>
    public async Task ReprocessByLibraryUid(params Guid[] libraryUids)
    {
        if (libraryUids?.Any() != true)
            return;
        
        string inStr = string.Join(",", libraryUids.Select(x => $"'{x}'"));
        await DbHelper.Execute($"update LibraryFile set Status = 0 " +
                               $" where LibraryUid in ({inStr}) and Status <> {((int)FileStatus.Processing)}"); // we dont reset processing files
        foreach (var f in Data)
        {
            if (f.Value?.LibraryUid == null || libraryUids.Contains(f.Value.LibraryUid.Value) == false)
                continue;
            
            if (f.Value.Status != FileStatus.Processing)
                f.Value.Status = FileStatus.Unprocessed;
        }
        ClientServiceManager.Instance.UpdateFileStatus();
    }
    
    /// <summary>
    /// Sets the status of multiple files
    /// </summary>
    /// <param name="status">The status to set</param>
    /// <param name="uids">the UIDs of the files</param>
    public async Task SetStatus(FileStatus status, params Guid[] uids)
    {
        if (uids?.Any() != true)
            return;
        
        string inStr = string.Join(",", uids.Select(x => $"'{x}'"));
        int intStatus = (int)status;
        if (intStatus < 0)
            intStatus = 0;
        string hold = string.Empty;
        if (status == FileStatus.Unprocessed)
            hold = ", HoldUntil = '1970-01-01 00:00:01'";
        await DbHelper.Execute($"update LibraryFile set Status = {intStatus}{hold} where Uid in ({inStr})");
        foreach (var uid in uids)
        {
            if (Data.TryGetValue(uid, out LibraryFile? file))
            {
                if (status == FileStatus.Unprocessed)
                    file.HoldUntil = new DateTime(1970, 1, 1, 0, 0, 1);
                file.Status = status;
            }
        }
        ClientServiceManager.Instance.UpdateFileStatus();
    }

    /// <summary>
    /// Toggles a flag on files
    /// </summary>
    /// <param name="flag">the flag to toggle</param>
    /// <param name="uids">the UIDs of the files</param>
    public async Task ToggleFlag(LibraryFileFlags flag, Guid[] uids)
    {
        if (uids?.Any() != true)
            return;
        int iflag = (int)flag;
        string inStr = string.Join(",", uids.Select(x => $"'{x}'"));
        await DbHelper.Execute(@$"UPDATE LibraryFile
        SET Flags = CASE
        WHEN Flags & {iflag} > 0 THEN Flags & ~{iflag}
        ELSE Flags | {iflag}
        END
        where Uid in ({inStr})");
        
        foreach (var uid in uids)
        {
            if (Data.TryGetValue(uid, out LibraryFile? file))
            {
                file.Flags ^= flag;
            }
        }
    }

    /// <summary>
    /// Force processing a set of files
    /// </summary>
    /// <param name="uids">the UIDs of the files</param>
    public async Task ForceProcessing(Guid[] uids)
    {
        if (uids?.Any() != true)
            return;
        string inStr = string.Join(",", uids.Select(x => $"'{x}'"));
        await DbHelper.Execute($"update LibraryFile set Flags = Flags | {((int)LibraryFileFlags.ForceProcessing)} " +
                               $" where Uid in ({inStr})");
        
        foreach (var uid in uids)
        {
            if (Data.TryGetValue(uid, out LibraryFile? file))
            {
                file.Flags |= LibraryFileFlags.ForceProcessing;
            }
        }
    }


    /// <summary>
    /// Updates a flow name in the database
    /// </summary>
    /// <param name="uid">the UID of the flow</param>
    /// <param name="name">the updated name of the flow</param>
    public async Task UpdateFlowName(Guid uid, string name)
    {
        await DbHelper.Execute("update LibraryFile set FlowName = @0 where FlowUid = @1", name, uid);
        lock (Data)
        {
            foreach (var file in Data.Values)
            {
                if (file.LibraryUid == uid)
                    file.LibraryName = name;
            }
        }
    }

    /// <summary>
    /// Updates the original size of a file
    /// </summary>
    /// <param name="uid">The UID of the file</param>
    /// <param name="size">the size of the file in bytes</param>
    public async Task UpdateOriginalSize(Guid uid, long size)
    {
        var existing = GetByUid(uid);
        if (uid == Guid.Empty)
            return; // unknown file
        if (existing.OriginalSize == size)
            return; // nothing to do
        existing.OriginalSize = size;
        await DbHelper.Execute($"update LibraryFile set OriginalSize = {size} where Uid = '{uid}'");
    }

    /// <summary>
    /// Clears the executed nodes, metadata, final size etc for a file
    /// </summary>
    /// <param name="uid">The UID of the file</param>
    public Task ResetFileInfoForProcessing(Guid uid)
        => DbHelper.Execute($"update LibraryFile set ExecutedNodes = '', OriginalMetadata = '', FinalMetadata = '', FinalSize = 0, OutputPath = '', FailureReason = '', ProcessOnNodeUid = '' where Uid = '{uid}'");

}