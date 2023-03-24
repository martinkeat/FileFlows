using FileFlows.Server.Workers;
using Microsoft.AspNetCore.Mvc;
using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;
using System.Text.RegularExpressions;
using FileFlows.Server.Services;

namespace FileFlows.Server.Controllers;

/// <summary>
/// Library controller
/// </summary>
[Route("/api/library")]
public class LibraryController : Controller
{
    private static bool? _HasLibraries;
    /// <summary>
    /// Gets if there are any libraries
    /// </summary>
    internal static bool HasLibraries
    {
        get
        {
            if (_HasLibraries == null)
                UpdateHasLibraries();
            return _HasLibraries == true;
        }
        private set => _HasLibraries = value;
    }
    private static void UpdateHasLibraries()
        => _HasLibraries = new LibraryService().GetAll().Count > 0;

    /// <summary>
    /// Gets all libraries in the system
    /// </summary>
    /// <returns>a list of all libraries</returns>
    [HttpGet]
    public IEnumerable<Library> GetAll() 
        => new LibraryService().GetAll().OrderBy(x => x.Name.ToLowerInvariant());


    /// <summary>
    /// Get a library
    /// </summary>
    /// <param name="uid">The UID of the library</param>
    /// <returns>the library instance</returns>
    [HttpGet("{uid}")]
    public Library Get(Guid uid) =>
        new LibraryService().GetByUid(uid);

    /// <summary>
    /// Saves a library
    /// </summary>
    /// <param name="library">The library to save</param>
    /// <returns>the saved library instance</returns>
    [HttpPost]
    public Library Save([FromBody] Library library)
    {
        if (library?.Flow == null)
            throw new Exception("ErrorMessages.NoFlowSpecified");
        if (library.Uid == Guid.Empty)
            library.LastScanned = DateTime.MinValue; // never scanned
        if (Regex.IsMatch(library.Schedule, "^[01]{672}$") == false)
            library.Schedule = new string('1', 672);

        var service = new LibraryService();
        bool nameUpdated = false;
        if (library.Uid != Guid.Empty)
        {
            // existing, check for name change
            var existing = service.GetByUid(library.Uid);
            nameUpdated = existing != null && existing.Name != library.Name;
        }
        
        bool newLib = library.Uid == Guid.Empty;
        service.Update(library);

        _ = Task.Run(async () =>
        {
            await Task.Delay(1);
            if (nameUpdated)
                await new ObjectReferenceUpdater().RunAsync();

            RefreshCaches();

            if (newLib)
                Rescan(new() { Uids = new[] { library.Uid } });
        });
        
        return library;
    }

    /// <summary>
    /// Refresh the caches where libraries are stored in memory
    /// </summary>
    private void RefreshCaches()
    {
        LibraryWorker.UpdateLibraries();
    }

    /// <summary>
    /// Set the enable state for a library
    /// </summary>
    /// <param name="uid">The UID of the library</param>
    /// <param name="enable">true if enabled, otherwise false</param>
    /// <returns>the updated library instance</returns>
    [HttpPut("state/{uid}")]
    public Library SetState([FromRoute] Guid uid, [FromQuery] bool enable)
    {
        var service = new LibraryService();
        var library = service.GetByUid(uid);
        if (library == null)
            throw new Exception("Library not found.");
        
        if (library.Enabled != enable)
        {
            library.Enabled = enable;
            service.Update(library);
        }
        return library;
    }

    /// <summary>
    /// Delete libraries from the system
    /// </summary>
    /// <param name="model">A reference model containing UIDs to delete</param>
    /// <param name="deleteLibraryFiles">[Optional] if libraries files should also be deleted for this library</param>
    /// <returns>an awaited task,</returns>
    [HttpDelete]
    public async Task Delete([FromBody] ReferenceModel<Guid> model, [FromQuery] bool deleteLibraryFiles = false)
    {
        if (model?.Uids?.Any() != true)
            return;
        await new LibraryService().Delete(model.Uids);
        if (deleteLibraryFiles)
        {
            await new LibraryFileService().DeleteFromLibraries(model.Uids);
        }

        UpdateHasLibraries();
        RefreshCaches();
    }

    /// <summary>
    /// Rescans libraries
    /// </summary>
    /// <param name="model">A reference model containing UIDs to rescan</param>
    /// <returns>an awaited task</returns>
    [HttpPut("rescan")]
    public void Rescan([FromBody] ReferenceModel<Guid> model)
    {
        var service = new LibraryService();
        foreach(var uid in model.Uids)
        {
            var item = service.GetByUid(uid);
            if (item == null)
                continue;
            item.LastScanned = DateTime.MinValue;
            service.Update(item);
        }

        _ = Task.Run(async () =>
        {
            await Task.Delay(1);
            RefreshCaches();
            LibraryWorker.ScanNow();
        });
    }

    internal void UpdateFlowName(Guid uid, string name)
    {
        var service = new LibraryService();
        var libraries = service.GetAll();
        foreach (var lib in libraries.Where(x => x.Flow?.Uid == uid))
        {
            lib.Flow.Name = name;
            service.Update(lib);
        }
    }

    internal void UpdateLastScanned(Guid uid)
    {
        var service = new LibraryService();
        var lib = service.GetByUid(uid);
        if (lib == null)
            return;
        lib.LastScanned = DateTime.Now;
        service.Update(lib, dontIncrementConfigRevision: true);
    }


    private FileInfo[] GetTemplateFiles() 
        => new DirectoryInfo(DirectoryHelper.TemplateDirectoryLibrary).GetFiles("*.json", SearchOption.AllDirectories);

    /// <summary>
    /// Gets a list of library templates
    /// </summary>
    /// <returns>a list of library templates</returns>
    [HttpGet("templates")]
    public Dictionary<string, List<Library>> GetTemplates()
    {
        SortedDictionary<string, List<Library>> templates = new(StringComparer.OrdinalIgnoreCase);
        var lstGeneral = new List<Library>();
        foreach (var tf in GetTemplateFiles())
        {
            try
            {
                string json = string.Join("\n", System.IO.File.ReadAllText(tf.FullName).Split('\n').Skip(1)); // remove the //path comment
                json = TemplateHelper.ReplaceWindowsPathIfWindows(json);
                var jst =JsonSerializer.Deserialize<LibraryTemplate>(json, new JsonSerializerOptions
                {
                    AllowTrailingCommas = true,
                    PropertyNameCaseInsensitive = true
                });
                string group = jst.Group ?? string.Empty;
                var library = new Library
                {
                    Enabled = true,
                    FileSizeDetectionInterval = jst.FileSizeDetectionInterval,
                    Filter = jst.Filter ?? string.Empty,
                    ExclusionFilter = jst.ExclusionFilter ?? string.Empty,
                    Name = jst.Name,
                    Description = jst.Description,
                    Path = jst.Path,
                    Priority = jst.Priority,
                    ScanInterval = jst.ScanInterval,
                    ReprocessRecreatedFiles = jst.ReprocessRecreatedFiles
                };
                if (group == "General")
                    lstGeneral.Add(library);
                else
                {
                    if (templates.ContainsKey(group) == false)
                        templates.Add(group, new List<Library>());
                    templates[group].Add(library);
                }
            }
            catch (Exception) { }
        }

        var dict = new Dictionary<string, List<Library>>();
        if(lstGeneral.Any())
            dict.Add("General", lstGeneral.OrderBy(x => x.Name.ToLowerInvariant()).ToList());
        foreach (var kv in templates)
        {
            if(kv.Value.Any())
                dict.Add(kv.Key, kv.Value.OrderBy(x => x.Name.ToLowerInvariant()).ToList());
        }

        return dict;
    }

    /// <summary>
    /// Rescans enabled libraries and waits for them to be scanned
    /// </summary>
    [HttpPost("rescan-enabled")]
    public void RescanEnabled()
    {
        var service = new LibraryService();
        var libs = service.GetAll().Where(x => x.Enabled).Select(x => x.Uid).ToArray();
        Rescan(new ReferenceModel<Guid> { Uids = libs });
    }
}