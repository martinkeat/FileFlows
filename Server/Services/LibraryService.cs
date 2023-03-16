namespace FileFlows.Server.Services;

using FileFlows.Server.Controllers;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Models;
using System;
using System.Threading.Tasks;

/// <summary>
/// Service for communicating with FileFlows server for libraries
/// </summary>
public class LibraryService : ILibraryService
{
    /// <summary>
    /// Gets or sets a function to load an instance of a ILibraryService
    /// </summary>
    public static Func<ILibraryService> Loader { get; set; }

    /// <summary>
    /// Loads an instance of the library service
    /// </summary>
    /// <returns>an instance of the library service</returns>
    public static ILibraryService Load()
    {
        if (Loader == null)
            return new LibraryService();
        return Loader.Invoke();
    }
    
    /// <summary>
    /// Gets a library by its UID
    /// </summary>
    /// <param name="uid">The UID of the library</param>
    /// <returns>An instance of the library if found</returns>
    public Task<Library> Get(Guid uid) => new LibraryController().Get(uid);
    
    /// <summary>
    /// Gets all libraries in the system
    /// </summary>
    /// <returns>a list of all libraries</returns>
    public Task<IEnumerable<Library>> GetAll()
        => new LibraryController().GetAll();
}