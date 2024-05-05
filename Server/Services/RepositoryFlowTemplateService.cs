using System.Text.RegularExpressions;
using FileFlows.Plugin;
using FileFlows.Server.DefaultTemplates;
using FileFlows.Server.Helpers;
using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// Service for loading flow templates from the repository
/// </summary>
public class RepositoryFlowTemplateService
{
    private List<FlowTemplateModel> Templates;
    private DateTime FetchedAt = DateTime.MinValue;
    private readonly string FlowTemplatesFile = Path.Combine(DirectoryHelper.TemplateDirectoryFlow, "flow-templates.json");

    /// <summary>
    /// Gets the available templates
    /// </summary>
    /// <returns>the available templates</returns>
    public async Task<List<FlowTemplateModel>> GetTemplates()
    {
        if (Templates == null || FetchedAt < DateTime.UtcNow.AddMinutes(-10))
        {
            await RefreshTemplates();
            // always refresh the fetched at, incase it fails we dont want to fail again and again
            FetchedAt = DateTime.UtcNow; 
        }

        return Templates.ToList(); // clone it so they dont alter this list
    }
    
    /// <summary>
    /// Refreshes the templates
    /// </summary>
    async Task RefreshTemplates()
    {
        RequestResult<List<FlowTemplateModel>>? result = null;
        try
        {
            result = await HttpHelper.Get<List<FlowTemplateModel>>(
                "https://raw.githubusercontent.com/revenz/FileFlowsRepository/master/flows.json?dt=" +
                DateTime.UtcNow.Ticks);
        }
        catch (Exception ex)
        {
            Logger.Instance?.ELog("Failed to load flow templates from github: " + ex.Message);
        }
        
        if (result?.Success == true && result.Data != null)
        {
            Templates = result.Data;
            // save to disk
            string json = JsonSerializer.Serialize(result.Data);
            _ = File.WriteAllTextAsync(FlowTemplatesFile, json);
        }

        if(Templates == null)
            LoadFlowTemplatesFromLocalStorage();
        if (Templates == null)
            LoadFromEmbeddedResources();
        if (Templates == null)
            Templates = []; // shouldnt happened, embedded should be safe fall back
        
        Templates = Templates.OrderBy(x => x.Name.IndexOf(" ", StringComparison.Ordinal) < 0 ? 1 : Regex.IsMatch(x.Name, "^[\\w]+ File") ? 2 : 3)
            .ThenBy(x => x.Author == "FileFlows" ? 1 : 2)
            .ThenBy(x => Regex.IsMatch(x.Name, @"^Convert [\w]+$") ? 1 : 2)
            .ThenBy(x => x.Name)
            .ToList();
    }
    
    /// <summary>
    /// Loads the flows templates from the local storage
    /// </summary>
    private void LoadFlowTemplatesFromLocalStorage()
    {
        if(File.Exists(FlowTemplatesFile) == false)
            return;
        try
        {
            string json = File.ReadAllText(FlowTemplatesFile);
            Templates = JsonSerializer.Deserialize<List<FlowTemplateModel>>(json);
        }
        catch (Exception ex)
        {
            Logger.Instance.WLog("Error parsing flow-templates.json, deleting:  " + ex.Message);
            File.Delete(FlowTemplatesFile);
        }
    }
    
    /// <summary>
    /// Loads the flows templates from the local storage
    /// </summary>
    private void LoadFromEmbeddedResources()
    {
        try
        {
            string json = TemplateLoader.GetFlowsJson();
            Templates = JsonSerializer.Deserialize<List<FlowTemplateModel>>(json);
        }
        catch (Exception ex)
        {
            Logger.Instance.WLog("Error parsing embedded flows.json:  " + ex.Message);
        }
    }
    
    /// <summary>
    /// Loads a specific flow template
    /// </summary>
    /// <param name="path">the path to the template</param>
    /// <param name="revision">the revision</param>
    /// <returns>the loaded flow from the template</returns>
    public async Task<Result<Flow>> LoadFlowTemplate(string path, int revision)
    {
        // Templates/Flow/Standard/Audio File.json
        if (path.Contains("..") || path.StartsWith("Templates/Flow/") == false || path.EndsWith(".json") == false)
            return Result<Flow>.Fail("Bad file: " + path);
        
        string fileName = Path.Combine(DirectoryHelper.TemplateDirectoryFlow, path[15..].Replace('/', Path.DirectorySeparatorChar));
        string? json;
        if (File.Exists(fileName) == false)
        {
            // need to download the flow template from github
            var result = await HttpHelper.Get<string>("https://raw.githubusercontent.com/revenz/FileFlowsRepository/master/" + path);
            if (result.Success == false)
                return Result<Flow>.Fail("Failed to download from repository");
            try
            {
                var dir = new FileInfo(fileName).Directory;
                if (dir.Exists == false)
                    dir.Create();

                await File.WriteAllTextAsync(fileName, result.Data);
                json = result.Data;
            }
            catch (Exception ex)
            {
                Logger.Instance.ELog("Error saving template: "+ ex.Message);
                return Result<Flow>.Fail("Failed to save template");
            }
        }
        else
        {
            json = await File.ReadAllTextAsync(fileName);
        }
        json = TemplateHelper.ReplaceOutputPathVariable(json);
        var flow = JsonSerializer.Deserialize<Flow>(json);

        if (flow.Revision < revision)
        {
            // download a new version
            var result = await HttpHelper.Get<string>("https://raw.githubusercontent.com/revenz/FileFlowsRepository/master/" + path);
            if (result.Success)
            {
                await File.WriteAllTextAsync(fileName, result.Data);
                json = result.Data;
                flow = JsonSerializer.Deserialize<Flow>(json);
            }
        }

        return flow;
    }
}