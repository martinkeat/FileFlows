namespace FileFlows.ServerShared.Services;

using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;

/// <summary>
/// Interface for communicating with FileFlows server for libraries
/// </summary>
public interface ILibraryService
{
    /// <summary>
    /// Gets a library by its UID
    /// </summary>
    /// <param name="uid">The UID of the library</param>
    /// <returns>An instance of the library if found</returns>
    Task<Library?> GetByUidAsync(Guid uid);

    /// <summary>
    /// Gets all libraries in the system
    /// </summary>
    /// <returns>a list of all libraries</returns>
    Task<List<Library>> GetAllAsync();
}
