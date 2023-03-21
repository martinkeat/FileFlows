namespace FileFlows.Server.Services;

using FileFlows.ServerShared.Services;
using FileFlows.Shared.Models;
using System;

/// <summary>
/// Service for communicating with FileFlows server for libraries
/// </summary>
public class LibraryService : CachedService<Library>, ILibraryService
{
    static LibraryService()
    {
        if(Globals.IsUnitTesting == false)
            new LibraryService().Refresh();
    }
    
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
}