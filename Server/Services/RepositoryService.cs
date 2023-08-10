using System.Text.RegularExpressions;
using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// Service used to interact with the repository
/// </summary>
class RepositoryService
{
    private FileFlowsRepository repo;
    const string BASE_URL = "https://raw.githubusercontent.com/revenz/FileFlowsRepository/master/";


    public async Task Init()
    {
        repo = await GetRepository();
    }

    /// <summary>
    /// Get the repository data which contains information on all the scripts
    /// </summary>
    /// <returns>the repository data which contains information on all the scripts</returns>
    /// <exception cref="Exception">If failed to load the repository</exception>
    internal async Task<FileFlowsRepository> GetRepository()
    {
        string url = BASE_URL + "repo.json?ts=" + DateTime.UtcNow.ToFileTimeUtc();
        try
        {
            var srResult = await HttpHelper.Get<FileFlowsRepository>(url);
            if (srResult.Success == false)
                throw new Exception(srResult.Body);
            return srResult.Data;
        }
        catch (Exception ex)
        {
            Logger.Instance.WLog("Error getting repository: " + ex.Message);
            return new FileFlowsRepository()
            {
                FlowScripts = new(),
                FlowTemplates = new(),
                FunctionScripts = new(),
                LibraryTemplates = new(),
                SharedScripts = new(),
                SystemScripts = new(),
                WebhookScripts = new ()
            };
        }
    }

    /// <summary>
    /// Downloads the shared scripts from the repository
    /// </summary>
    /// <param name="force">when true, this will force every template to be re-downloaded and not just updates</param>
    /// <returns>a task to await</returns>
    internal Task DownloadSharedScripts(bool force = false) =>
        DownloadObjects(repo.SharedScripts, DirectoryHelper.ScriptsDirectoryShared, force);
    
    /// <summary>
    /// Downloads the function scripts from the repository
    /// <param name="force">when true, this will force every template to be re-downloaded and not just updates</param>
    /// </summary>
    /// <returns>a task to await</returns>
    internal Task DownloadFunctionScripts(bool force = false)
        => DownloadObjects(repo.FunctionScripts, DirectoryHelper.ScriptsDirectoryFunction, force);
    
    /// <summary>
    /// Downloads the library templates from the repository
    /// <param name="force">when true, this will force every template to be re-downloaded and not just updates</param>
    /// </summary>
    /// <returns>a task to await</returns>
    internal Task DownloadLibraryTemplates(bool force = false)
        => DownloadObjects(repo.LibraryTemplates, DirectoryHelper.TemplateDirectoryLibrary, force);
    

    /// <summary>
    /// Downloads objects from the repository
    /// </summary>
    /// <param name="objects">the objects to download</param>
    /// <param name="destination">the location to save the objects to</param>
    /// <param name="force">when true, this will force every template to be re-downloaded and not just updates</param>
    /// <returns>a task to await</returns>
    private async Task DownloadObjects(IEnumerable<RepositoryObject> objects, string destination, bool force)
    {
        foreach (var obj in objects)
        {
            if (obj.MinimumVersion > new Version(Globals.Version))
                continue;
            string output = obj.Path;
            output = Regex.Replace(output, @"^Scripts\/[^\/]+\/", string.Empty);
            output = Regex.Replace(output, @"^Templates\/[^\/]+\/", string.Empty);
            output = Path.Combine(destination, output);
            if (force == false && File.Exists(output))
            {
                // check the revision
                string existing = File.ReadAllText(output);
                var jsonMatch = Regex.Match(existing, @"(""revision""[\s]*:|@revision)[\s]*([\d]+)");
                if (jsonMatch?.Success == true)
                {
                    int revision = int.Parse(jsonMatch.Groups[2].Value);
                    if (obj.Revision == revision)
                    {
                        Logger.Instance.ILog($"Repository item already up to date [{revision}]: {output}");
                        continue;
                    }
                }

            }

            await DownloadObject(obj.Path, output);
        }
    }

    /// <summary>
    /// Downloads an object and saves it to disk
    /// </summary>
    /// <param name="path">the path identifier in the repository</param>
    /// <param name="outputFile">the filename where to save the file</param>
    /// <returns>a task to await</returns>
    private async Task DownloadObject(string path, string outputFile)
    {
        try
        {
            string content = await GetContent(path);
            if(outputFile.EndsWith(".json") == false)
                content = "// path: " + path + "\n\n" + content;
            
            var dir = new FileInfo(outputFile).Directory;
            if(dir.Exists == false)
                dir.Create();
            await File.WriteAllTextAsync(outputFile, content);
        }
        catch (Exception ex)
        { 
            Logger.Instance?.ELog($"Failed downloading script: '{path}' => {ex.Message}");
        }
    }
    
    /// <summary>
    /// Gets the content of a repository object
    /// </summary>
    /// <param name="path">the repository object path</param>
    /// <returns>the repository object content</returns>
    public async Task<string> GetContent(string path)
    {
        string url = BASE_URL + path;
        var result = await HttpHelper.Get<string>(url);
        if (result.Success == false)
            throw new Exception(result.Body);
        return result.Data;
    }

    /// <summary>
    /// Update all the repository objects
    /// </summary>
    /// <returns>an awaited task</returns>
    internal async Task Update()
    {
        await UpdateScripts();
        await UpdateTemplates();
    }

    /// <summary>
    /// Updates all the downloaded scripts from the repo
    /// </summary>
    /// <returns>an awaited task</returns>
    internal async Task UpdateScripts()
    {
        var files = Directory.GetFiles(DirectoryHelper.ScriptsDirectory, "*.js", SearchOption.AllDirectories);
        List<string> knownPaths = repo.FlowScripts.Union(repo.FunctionScripts).Union(repo.SharedScripts)
            .Union(repo.SystemScripts).Where(x => new Version(Globals.Version) >= x.MinimumVersion).Select(x => x.Path).ToList();
        await UpdateObjects(files, knownPaths);
    }
    
    
    /// <summary>
    /// Updates all the downloaded templates from the repo
    /// </summary>
    /// <returns>an awaited task</returns>
    internal async Task UpdateTemplates()
    {
        var files = Directory.GetFiles(DirectoryHelper.TemplateDirectory, "*.json", SearchOption.AllDirectories);
        List<string> knownPaths = repo.LibraryTemplates.Union(repo.FlowTemplates).Where(x => new Version(Globals.Version) >= x.MinimumVersion).Select(x => x.Path).ToList();
        await UpdateObjects(files, knownPaths);
    }

    /// <summary>
    /// Updates objects
    /// </summary>
    /// <param name="files">the files to update</param>
    /// <param name="knownPaths">the known paths</param>
    /// <returns>an awaited task</returns>
    private async Task UpdateObjects(IEnumerable<string> files, List<string> knownPaths)
    {
        List<Task> tasks = new();
        foreach (string file in files)
        {
            try
            {
                string line = (await File.ReadAllLinesAsync(file)).First();
                if (line?.StartsWith("// path:") == false)
                    continue;
                string path = line.Substring("// path:".Length).Trim();
                if(knownPaths.Contains(path))
                    tasks.Add(DownloadObject(path, file));
            }
            catch (Exception)
            {
            }
        }

        Task.WaitAll(tasks.ToArray());
        
    }

    /// <summary>
    /// Downloads objects from the repository
    /// </summary>
    /// <param name="paths">paths of the objects to download</param>
    /// <returns>a task to await</returns>
    internal async Task DownloadObjects(List<string> paths)
    {
        foreach (string path in paths)
        {
            await DownloadObject(path, Path.Combine(DirectoryHelper.ScriptsDirectory, "..", path));
        }
    }
}