using FileFlows.Plugin;
using FileFlows.Server.Services;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace FileFlows.Server.Controllers;

/// <summary>
/// Controller for the Repository
/// </summary>
[Route("/api/repository")]
public class RepositoryController : Controller
{
    /// <summary>
    /// Gets the scripts
    /// </summary>
    /// <param name="type">the type of scripts to get</param>
    /// <param name="missing">only include scripts not downloaded</param>
    /// <returns>a collection of scripts</returns>
    [HttpGet("scripts")]
    public async Task<IEnumerable<RepositoryObject>> GetScripts([FromQuery] ScriptType type, [FromQuery] bool missing = true)
    {
        var repo = await new RepositoryService().GetRepository();
        var scripts = (
                type == ScriptType.System ? repo.SystemScripts : 
                type == ScriptType.Webhook ? repo.WebhookScripts : 
                repo.FlowScripts)
            .Where(x => new Version(Globals.Version) >= x.MinimumVersion);
        if (missing)
        {
            List<string> known = new();
            foreach (var file in new DirectoryInfo(
                         type == ScriptType.System ? DirectoryHelper.ScriptsDirectorySystem : 
                         DirectoryHelper.ScriptsDirectoryFlow).GetFiles("*.js", SearchOption.AllDirectories))
            {
                try
                {
                    string line = (await System.IO.File.ReadAllLinesAsync(file.FullName)).First();
                    if (line?.StartsWith("// path:") == true)
                        known.Add(line[9..].Trim());
                }
                catch (Exception)
                {
                }
            }

            scripts = scripts.Where(x => known.Contains(x.Path) == false && known.Contains(x.Name) == false).ToList();
        }
        return scripts;
    }
    
    /// <summary>
    /// Increments the configuration revision
    /// </summary>
    /// <returns>an awaited task</returns>
    private Task RevisionIncrement()
        => new SettingsService().RevisionIncrement();

    /// <summary>
    /// Gets the code of a script
    /// </summary>
    /// <param name="path">the script path</param>
    /// <returns>the script code</returns>
    [HttpGet("content")]
    public Task<string> GetContent([FromQuery] string path) => new RepositoryService().GetContent(path);
    
    /// <summary>
    /// Download script into the FileFlows system
    /// </summary>
    /// <param name="model">A list of script to download</param>
    /// <returns>an awaited task</returns>
    [HttpPost("download")]
    public async Task Download([FromBody] RepositoryDownloadModel model)
    {
        if (model == null || model.Scripts?.Any() != true)
            return; // nothing to download

        // always re-download all the shared scripts to ensure they are up to date
        await DownloadActual(model.Scripts);
        await RevisionIncrement();

    }

    /// <summary>
    /// Perform the actual downloading of scripts
    /// </summary>
    /// <param name="scripts">the scripts to download</param>
    private async Task DownloadActual(List<string> scripts)
    {
        // always re-download all the shared scripts to ensure they are up to date
        var service = new RepositoryService();
        await service.Init();
        await service.DownloadSharedScripts();
        await service.DownloadObjects(scripts);
        
    }


    /// <summary>
    /// Update the scripts from th repository
    /// </summary>
    [HttpPost("update-scripts")]
    public async Task UpdateScripts()
    {
        var service = new RepositoryService();
        var original = (await GetScripts(ScriptType.Flow, missing: false)).ToDictionary(x => x.Path, x => x.Revision);
        await service.Init();
        await service.Update();
        var updated = (await GetScripts(ScriptType.Flow, missing: false)).ToDictionary(x => x.Path, x => x.Revision);
        bool changes = false;
        foreach (var key in original.Keys)
        {
            if (updated.ContainsKey(key) == false)
            {
                // shouldn't happen, but if it does
                changes = true;
                break;
            }

            if (updated[key] != original[key])
            {
                // revision changed, this means the config must update
                changes = true;
                break;
            }
        }
        if(changes)
        {
            // scripts were update, increment the revision
            await RevisionIncrement();
        }
    }

    /// <summary>
    /// Download the latest revisions for the specified scripts
    /// </summary>
    /// <param name="model">The list of scripts to update</param>
    /// <returns>if the updates were successful or not</returns>
    [HttpPost("update-specific-scripts")]
    public async Task<bool> UpdateSpecificScripts([FromBody] ReferenceModel<string> model)
    {
        var service = new RepositoryService();
        await service.Init();
        var repo = await service.GetRepository();
        var objects = repo.FlowScripts.Union(repo.SystemScripts).Where(x => x.MinimumVersion <= new Version(Globals.Version))
            .Where(x => model.Uids.Contains(x.Path)).ToList();
        if (objects.Any() == false)
            return false; // nothing to update
        await DownloadActual(objects.Select(x => x.Path).ToList());
        // we always do an update here, its a user forcing an update
        await RevisionIncrement();
        return true;
    }
    
    /// <summary>
    /// Download model
    /// </summary>
    public class RepositoryDownloadModel
    {
        /// <summary>
        /// A list of plugin packages to download
        /// </summary>
        public List<string> Scripts { get; set; }
    }
}