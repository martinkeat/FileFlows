using FileFlows.Server.Workers;
using Microsoft.AspNetCore.Mvc;
using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;
using System.Text.RegularExpressions;

namespace FileFlows.Server.Controllers;

/// <summary>
/// Library controller
/// </summary>
[Route("/api/library")]
public class LibraryController : ControllerStore<Library>
{
    protected override bool AutoIncrementRevision => true;
        
    internal override async Task<IEnumerable<Library>> GetDataList(bool? useCache = null)
    {
        return (await GetData()).Values.Select(x =>
        {
            if (string.IsNullOrEmpty(x.Schedule))
                x.Schedule = new string('1', 672);
            return x;
        }).ToList();
    }
    
    private static bool? _HasLibraries;
    /// <summary>
    /// Gets if there are any libraries
    /// </summary>
    internal static bool HasLibraries
    {
        get
        {
            if (_HasLibraries == null)
                UpdateHasLibraries().Wait();
            return _HasLibraries == true;
        }
        private set => _HasLibraries = value;
    }
    private static async Task UpdateHasLibraries()
    {
        _HasLibraries = await DbHelper.HasAny<Flow>();
    }

    /// <summary>
    /// Gets all libraries in the system
    /// </summary>
    /// <returns>a list of all libraries</returns>
    [HttpGet]
    public async Task<IEnumerable<Library>> GetAll() => (await GetDataList()).OrderBy(x => x.Name);


    /// <summary>
    /// Get a library
    /// </summary>
    /// <param name="uid">The UID of the library</param>
    /// <returns>the library instance</returns>
    [HttpGet("{uid}")]
    public Task<Library> Get(Guid uid) => GetByUid(uid);

    /// <summary>
    /// Saves a library
    /// </summary>
    /// <param name="library">The library to save</param>
    /// <returns>the saved library instance</returns>
    [HttpPost]
    public async Task<Library> Save([FromBody] Library library)
    {
        if (library?.Flow == null)
            throw new Exception("ErrorMessages.NoFlowSpecified");
        if (library.Uid == Guid.Empty)
            library.LastScanned = DateTime.MinValue; // never scanned
        if (Regex.IsMatch(library.Schedule, "^[01]{672}$") == false)
            library.Schedule = new string('1', 672);

        bool nameUpdated = false;
        if (library.Uid != Guid.Empty)
        {
            // existing, check for name change
            var existing = await GetByUid(library.Uid);
            nameUpdated = existing != null && existing.Name != library.Name;
        }
        
        bool newLib = library.Uid == Guid.Empty; 
        var result = await base.Update(library, checkDuplicateName: true);
        if(nameUpdated)
            _ = new ObjectReferenceUpdater().RunAsync();
        
        if (newLib && result != null)
            _ = Rescan(new() { Uids = new[] { result.Uid } });
        
        LibraryWorker.UpdateLibraries();
        
        return result;
    }

    /// <summary>
    /// Set the enable state for a library
    /// </summary>
    /// <param name="uid">The UID of the library</param>
    /// <param name="enable">true if enabled, otherwise false</param>
    /// <returns>the updated library instance</returns>
    [HttpPut("state/{uid}")]
    public async Task<Library> SetState([FromRoute] Guid uid, [FromQuery] bool enable)
    {
        var library = await GetByUid(uid);
        if (library == null)
            throw new Exception("Library not found.");
        if (library.Enabled != enable)
        {
            library.Enabled = enable;
            return await Update(library);
        }
            LibraryWorker.UpdateLibraries();
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
        await DeleteAll(model);
        if (deleteLibraryFiles)
        {
            await new Server.Services.LibraryFileService().DeleteFromLibraries(model.Uids);
        }

        await UpdateHasLibraries();
        LibraryWorker.UpdateLibraries();
    }

    /// <summary>
    /// Rescans libraries
    /// </summary>
    /// <param name="model">A reference model containing UIDs to rescan</param>
    /// <returns>an awaited task</returns>
    [HttpPut("rescan")]
    public async Task Rescan([FromBody] ReferenceModel<Guid> model)
    {
        foreach(var uid in model.Uids)
        {
            var item = await GetByUid(uid);
            if (item == null)
                continue;
            item.LastScanned = DateTime.MinValue;
            _ = Update(item);
        }

        _ = Task.Run(async () =>
        {
            await Task.Delay(1);
            LibraryWorker.UpdateLibraries();
            LibraryWorker.ScanNow();
        });
    }

    internal async Task UpdateFlowName(Guid uid, string name)
    {
        var libraries = await GetDataList();
        foreach (var lib in libraries.Where(x => x.Flow?.Uid == uid))
        {
            lib.Flow.Name = name;
            await Update(lib);
        }
    }

    internal async Task UpdateLastScanned(Guid uid)
    {
        var lib = await GetByUid(uid);
        if (lib == null)
            return;
        lib.LastScanned = DateTime.Now;
        await Update(lib, dontIncremetnConfigRevision: true);
    }


    private FileInfo[] GetTemplateFiles() => new System.IO.DirectoryInfo(DirectoryHelper.TemplateDirectoryLibrary).GetFiles("*.json", SearchOption.AllDirectories);

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
                var jst = System.Text.Json.JsonSerializer.Deserialize<LibraryTemplate>(json, new System.Text.Json.JsonSerializerOptions
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
    public async Task RescanEnabled()
    {
        var libs = (await GetAll()).Where(x => x.Enabled).ToList();
        foreach (var lib in libs)
        {
            lib.LastScanned = DateTime.MinValue;
            await Update(lib);
        }
        LibraryWorker.ScanNow();

        var uids = libs.Select(x => x.Uid).ToList();

        int count = 0;
        do
        {
            await Task.Delay(1000);
            for (int i = uids.Count() - 1; i >= 0; i--)
            {
                var lib = await GetByUid(uids[i]);
                if (lib.LastScanned != DateTime.MinValue)
                {
                    uids.RemoveAt(i);
                }
            }
            ++count;
        } while (uids.Count() > 0 && count < 30);
    }
}