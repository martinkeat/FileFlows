using FileFlows.ServerShared.Models;

namespace FileFlows.Managers;

/// <summary>
/// Manager for the library files
/// </summary>
public class LibraryFileManager
{
    /// <summary>
    /// Gets a library file if it is known
    /// </summary>
    /// <param name="uid">the UID of the library file</param>
    /// <returns>the library file, or null if not found</returns>
    public Task<LibraryFile?> Get(Guid uid)
        => DatabaseAccessManager.Instance.LibraryFileManager.Get(uid);

    /// <summary>
    /// Gets a library file if it is known
    /// </summary>
    /// <param name="path">the path of the library file</param>
    /// <returns>the library file if it is known</returns>
    public Task<LibraryFile?> GetFileIfKnown(string path)
        => DatabaseAccessManager.Instance.LibraryFileManager.GetFileIfKnown(path);
    
    /// <summary>
    /// Gets a library file if it is known by its fingerprint
    /// </summary>
    /// <param name="fingerprint">the fingerprint of the library file</param>
    /// <returns>the library file if it is known</returns>
    public Task<LibraryFile?> GetFileByFingerprint(string fingerprint)
        => DatabaseAccessManager.Instance.LibraryFileManager.GetFileByFingerprint(fingerprint);

    /// <summary>
    /// Adds a files 
    /// </summary>
    /// <param name="files">the files being added</param>
    public Task Insert(params LibraryFile[] files)
        => DatabaseAccessManager.Instance.LibraryFileManager.InsertBulk(files);

    /// <summary>
    /// Updates a file 
    /// </summary>
    /// <param name="file">the file being updated</param>
    public Task UpdateFile(LibraryFile file)
        => DatabaseAccessManager.Instance.LibraryFileManager.Update(file);

    /// <summary>
    /// Remove files from the cache
    /// </summary>
    /// <param name="uids">UIDs to remove</param>
    public Task Remove(params Guid[] uids)
        => DatabaseAccessManager.Instance.LibraryFileManager.Delete(uids);

    /// <summary>
    /// Gets the library status overview
    /// </summary>
    /// <returns>the library status overview</returns>
    public async Task<List<LibraryStatus>> GetStatus()
    {
        var libraries = await new LibraryManager().GetAll();
        return await DatabaseAccessManager.Instance.LibraryFileManager.GetStatus(libraries);
    }

    /// <summary>
    /// Clears the executed nodes, metadata, final size etc for a file
    /// </summary>
    /// <param name="uid">The UID of the file</param>
    /// <param name="flowUid">the UID of the flow that will be executed</param>
    /// <param name="flowName">the name of the flow that will be executed</param>
    /// <returns>true if a row was updated, otherwise false</returns>
    public Task ResetFileInfoForProcessing(Guid uid, Guid? flowUid, string flowName)
        => DatabaseAccessManager.Instance.LibraryFileManager.ResetFileInfoForProcessing(uid, flowUid, flowName);

    /// <summary>
    /// Deletes files from the database
    /// </summary>
    /// <param name="uids">the UIDs of the files to remove</param>
    public Task Delete(Guid[] uids)
        => DatabaseAccessManager.Instance.LibraryFileManager.Delete(uids);

    /// <summary>
    /// Gets all matching library files
    /// </summary>
    /// <param name="filter">the filter to get files for</param>
    /// <returns>a list of matching library files</returns>
    public Task<List<LibraryFile>> GetAll(LibraryFileFilter filter)
        => DatabaseAccessManager.Instance.LibraryFileManager.GetAll(filter);

    /// <summary>
    /// Gets the total items matching the filter
    /// </summary>
    /// <param name="allLibraries">all the libraries in the system</param>
    /// <param name="status">the status</param>
    /// <param name="filter">the filter</param>
    /// <returns>the total number of items matching</returns>
    public Task<int> GetTotalMatchingItems(List<Library> allLibraries, FileStatus? status, string filter)
        => DatabaseAccessManager.Instance.LibraryFileManager.GetTotalMatchingItems(allLibraries, status, filter);

    /// <summary>
    /// Updates a file as started processing
    /// </summary>
    /// <param name="nextFileUid">the UID of the file</param>
    /// <param name="nodeUid">the UID of the node processing this file</param>
    /// <param name="nodeName">the name of the node processing this file</param>
    /// <param name="workerUid">the UID of the worker processing this file</param>
    /// <returns>true if successfully updated, otherwise false</returns>
    public Task<bool> StartProcessing(Guid nextFileUid, Guid nodeUid, string nodeName, Guid workerUid)
        => DatabaseAccessManager.Instance.LibraryFileManager.StartProcessing(nextFileUid, nodeUid, nodeName, workerUid);

    /// <summary>
    /// Unhold library files
    /// </summary>
    /// <param name="uids">the UIDs to unhold</param>
    /// <returns>an awaited task</returns>
    public Task Unhold(Guid[] uids)
        => DatabaseAccessManager.Instance.LibraryFileManager.SetStatus(FileStatus.Unprocessed, uids);

    /// <summary>
    /// Updates all files with the new flow name if they used this flow
    /// </summary>
    /// <param name="uid">the UID of the flow</param>
    /// <param name="name">the new name of the flow</param>
    /// <returns>a task to await</returns>
    public Task UpdateFlowName(Guid uid, string name)
        => DatabaseAccessManager.Instance.LibraryFileManager.UpdateFlowName(uid, name);

    /// <summary>
    /// Updates all files with the new library name if they used this library
    /// </summary>
    /// <param name="uid">the UID of the library</param>
    /// <param name="name">the new name of the library</param>
    /// <returns>a task to await</returns>
    public Task UpdateLibraryName(Guid uid, string name)
        => DatabaseAccessManager.Instance.LibraryFileManager.UpdateLibraryName(uid, name);

    /// <summary>
    /// Updates all files with the new node name if they used this node
    /// </summary>
    /// <param name="uid">the UID of the node</param>
    /// <param name="name">the new name of the node</param>
    /// <returns>a task to await</returns>
    public Task UpdateNodeName(Guid uid, string name)
        => DatabaseAccessManager.Instance.LibraryFileManager.UpdateNodeName(uid, name);

    /// <summary>
    /// Deletes files from the database
    /// </summary>
    /// <param name="nonProcessedOnly">if only non processed files should be delete</param>
    /// <param name="uids">the UIDs of the libraries to remove</param>
    public Task DeleteByLibrary(Guid[] uids, bool nonProcessedOnly = false)
        => DatabaseAccessManager.Instance.LibraryFileManager.DeleteByLibrary(nonProcessedOnly, uids);

    /// <summary>
    /// Reprocess all files based on library UIDs
    /// </summary>
    /// <param name="uids">an array of UID of the libraries to reprocess</param>
    /// <returns>true if any rows were updated, otherwise false</returns>
    public Task<bool> ReprocessByLibraryUid(Guid[] uids)
        => DatabaseAccessManager.Instance.LibraryFileManager.ReprocessByLibraryUid(uids);

    /// <summary>
    /// Gets all the UIDs for library files in the system
    /// </summary>
    /// <returns>the UIDs of known library files</returns>
    public Task<List<Guid>> GetUids()
        => DatabaseAccessManager.Instance.LibraryFileManager.GetUids();

    /// <summary>
    /// Gets the processing time for each library file 
    /// </summary>
    /// <returns>the processing time for each library file</returns>
    public Task<List<LibraryFileProcessingTime>> GetLibraryProcessingTimes()
        => DatabaseAccessManager.Instance.LibraryFileManager.GetLibraryProcessingTimes();


    /// <summary>
    /// Updates the original size of a file
    /// </summary>
    /// <param name="uid">The UID of the file</param>
    /// <param name="size">the size of the file in bytes</param>
    /// <returns>true if a row was updated, otherwise false</returns>
    public Task<bool> UpdateOriginalSize(Guid uid, long size)
        => DatabaseAccessManager.Instance.LibraryFileManager.UpdateOriginalSize(uid, size);

    /// <summary>
    /// Resets any currently processing library files 
    /// This will happen if a server or node is reset
    /// </summary>
    /// <param name="nodeUid">[Optional] the UID of the node</param>
    /// <returns>true if any files were updated</returns>
    public Task<bool> ResetProcessingStatus(Guid? nodeUid)
        => DatabaseAccessManager.Instance.LibraryFileManager.ResetProcessingStatus(nodeUid);


    /// <summary>
    /// Gets the current status of a file
    /// </summary>
    /// <param name="uid">The UID of the file</param>
    /// <returns>the current status of the file</returns>
    public Task<FileStatus?> GetFileStatus(Guid uid)
        => DatabaseAccessManager.Instance.LibraryFileManager.GetFileStatus(uid);

    // /// <summary>
    // /// Special case used by the flow runner to update a processing library file
    // /// </summary>
    // /// <param name="file">the processing library file</param>
    // public Task UpdateWork(LibraryFile file)
    //     => DatabaseAccessManager.Instance.LibraryFileManager.UpdateWork(file);

    /// <summary>
    /// Moves the passed in UIDs to the top of the processing order
    /// </summary>
    /// <param name="uids">the UIDs to move</param>
    public Task MoveToTop(Guid[] uids)
        => DatabaseAccessManager.Instance.LibraryFileManager.MoveToTop(uids);

    /// <summary>
    /// Sets a status on a file
    /// </summary>
    /// <param name="status">The status to set</param>
    /// <param name="uids">the UIDs of the files</param>
    /// <returns>true if any rows were updated, otherwise false</returns>
    public Task<bool> SetStatus(FileStatus status, params Guid[] uids)
        => DatabaseAccessManager.Instance.LibraryFileManager.SetStatus(status, uids);

    /// <summary>
    /// Toggles a flag on files
    /// </summary>
    /// <param name="flag">the flag to toggle</param>
    /// <param name="uids">the UIDs of the files</param>
    /// <returns>true if any rows were updated, otherwise false</returns>
    public Task<bool> ToggleFlag(LibraryFileFlags flag, params Guid[] uids)
        => DatabaseAccessManager.Instance.LibraryFileManager.ToggleFlag(flag, uids);

    /// <summary>
    /// Force processing a set of files
    /// </summary>
    /// <param name="uids">the UIDs of the files</param>
    /// <returns>true if any rows were updated, otherwise false</returns>
    public Task<bool> ForceProcessing(Guid[] uids)
        => DatabaseAccessManager.Instance.LibraryFileManager.ForceProcessing(uids);

    /// <summary>
    /// Updates a moved file in the database
    /// </summary>
    /// <param name="file">the file to update</param>
    /// <returns>true if any files were updated</returns>
    public Task<bool> UpdateMovedFile(LibraryFile file)
        => DatabaseAccessManager.Instance.LibraryFileManager.UpdateMovedFile(file);

    /// <summary>
    /// Gets a list of all filenames and the file creation times
    /// </summary>
    /// <param name="includeOutput">if output names should be included</param>
    /// <returns>a list of all filenames</returns>
    public Task<List<KnownFileInfo>> GetKnownLibraryFilesWithCreationTimes(bool includeOutput = false)
        => DatabaseAccessManager.Instance.LibraryFileManager.GetKnownLibraryFilesWithCreationTimes(includeOutput);

    /// <summary>
    /// Gets the shrinkage groups for the files
    /// </summary>
    /// <returns>the shrinkage groups</returns>
    public Task<List<ShrinkageData>> GetShrinkageGroups()
        => DatabaseAccessManager.Instance.LibraryFileManager.GetShrinkageGroups();

    /// <summary>
    /// Gets the total storage saved
    /// </summary>
    /// <returns>the total storage saved</returns>
    public Task<long> GetTotalStorageSaved()
        => DatabaseAccessManager.Instance.LibraryFileManager.GetTotalStorageSaved();

    /// <summary>
    /// Performs a search for files
    /// </summary>
    /// <param name="filter">the search filter</param>
    /// <returns>the matching files</returns>
    public Task<List<LibraryFile>> Search(LibraryFileSearchModel filter)
        => DatabaseAccessManager.Instance.LibraryFileManager.Search(filter);
}