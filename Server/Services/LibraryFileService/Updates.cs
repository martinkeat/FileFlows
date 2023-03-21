using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// Search functions for Library File Service
/// </summary>
public partial class LibraryFileService
{
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
}