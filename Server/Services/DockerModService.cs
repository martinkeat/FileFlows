using System.Text;
using System.Text.RegularExpressions;
using FileFlows.Managers;
using FileFlows.Plugin;
using FileFlows.ServerShared.Models;
using FileFlows.Shared.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace FileFlows.Server.Services;

/// <summary>
/// Service for DockerMods
/// </summary>
public class DockerModService
{
    /// <summary>
    /// Gets a DockerMod by its UID
    /// </summary>
    /// <param name="uid">the UID of the DockerMod</param>
    /// <returns>the DockerMod if found, otherwise null</returns>
    public Task<DockerMod?> GetByUid(Guid uid)
        => new DockerModManager().GetByUid(uid);
    /// <summary>
    /// Gets a DockerMod by its name
    /// </summary>
    /// <param name="name">the name of the DockerMod</param>
    /// <returns>the DockerMod if found, otherwise null</returns>
    public Task<DockerMod?> GetByName(string name)
        => new DockerModManager().GetByName(name);

    /// <summary>
    /// Gets all DockerMods in the system
    /// </summary>
    /// <returns>all DockerMods in the system</returns>
    public Task<List<DockerMod>> GetAll()
        => new DockerModManager().GetAll();


    /// <summary>
    /// Saves a DockerMod
    /// </summary>
    /// <param name="mod">The DockerMod to save</param>
    /// <param name="auditDetails">The audit details</param>
    /// <returns>the saved DockerMod instance</returns>
    public async Task<Result<DockerMod>> Save(DockerMod mod, AuditDetails? auditDetails)
    {
        var result = await new DockerModManager().Update(mod, auditDetails);
        if (Globals.IsDocker && result.Success(out DockerMod updated))
        {
            if(updated.Enabled)
                WriteDockerMod(updated);
            else
                DeleteDockerModFromDisk(updated);
        }
        return result;
    }


    /// <summary>
    /// Exports a DockerMod
    /// </summary>
    /// <param name="uid">the UID of the DockerMod</param>
    /// <returns>The file download result</returns>
    public async Task<Result<(string Name, string Content)>> Export(Guid uid)
    {
        var mod = await ServiceLoader.Load<DockerModService>().GetByUid(uid);
        if (mod == null)
            return Result<(string, string)>.Fail("Not found");
        
        // Serialize the DockerMod object to YAML
        var serializer = new SerializerBuilder()
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();
        var yaml = new string('-', 100) + "\n" + serializer.Serialize(new {
            mod.Name,
            mod.Description,
            Author = mod.Author?.EmptyAsNull(),
            mod.Revision,
            Icon = mod.Icon?.EmptyAsNull()
        }) + new string('-', 100);

        string content = string.Join("\n", yaml.Split('\n').Select(x => "# " + x))
                         + "\n\n" + mod.Code;
        return (mod.Name, content);
    }
    
    /// <summary>
    /// Imports a DockerMod from the repository script
    /// </summary>
    /// <param name="content">the repository script content</param>
    /// <param name="auditDetails">The audit details</param>
    public async Task<Result<bool>> ImportFromRepository(string content, AuditDetails? auditDetails)
    {
        var modResult = Parse(content);
        if (modResult.Failed(out string error))
            return Result<bool>.Fail(error);
        
        var mod = modResult.Value;

        var existing = await GetByName(mod.Name);
        if (existing != null)
        {
            if(mod.Repository == false)
                return Result<bool>.Fail("Cannot update non-repository DockerMod");
            existing.Code = mod.Code;
            existing.Revision = mod.Revision;
            existing.Author = mod.Author;
            existing.Description = mod.Description;
            existing.Icon = mod.Icon;
            mod = existing;
        }

        var result = await Save(mod, auditDetails);
        if (result.Failed(out error))
            return Result<bool>.Fail(error);

        return true;
    }

    /// <summary>
    /// Parses the content of a DockerMod from the repository and returns the result
    /// </summary>
    /// <param name="content">the DockerMod repository content</param>
    /// <returns>The DockerMod</returns>
    public Result<DockerMod> Parse(string content)
    {
        try
        {
            var match = Regex.Match(content, "(?s)# [-]{60,}(.*?)# [-]{60,}");
            if (match?.Success != true)
                return Result<DockerMod>.Fail("Invalid DockerMod content");

            var head = match.Value;
            content = content.Replace(head, string.Empty).Trim();

            var yaml = string.Join("\n", head.Split('\n').Where(x => x.StartsWith("# -----") == false)
                .Select(x => x[2..]));
            var code = content;

            // Deserialize YAML to DockerMod object
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(PascalCaseNamingConvention.Instance)
                .Build();

            var mod = deserializer.Deserialize<DockerMod>(yaml);
            mod.Code = code;
            mod.Repository = true;
            return mod;
        }
        catch (Exception ex)
        {
            return Result<DockerMod>.Fail(ex.Message);
        }
    }

    /// <summary>
    /// Deletes the given DockerMods
    /// </summary>
    /// <param name="uids">the UID of the DockerMods to delete</param>
    /// <param name="auditDetails">the audit details</param>
    /// <returns>a task to await</returns>
    public Task Delete(Guid[] uids, AuditDetails auditDetails)
        => new DockerModManager().Delete(uids, auditDetails);

    /// <summary>
    /// Writes the DockerMods to the DockerMods directory
    /// </summary>
    public async Task WriteDockerMods()
    {
        if (Globals.IsDocker == false)
            return;
        var mods = await GetAll();
        foreach (var dm in mods)
        {
            if (dm.Enabled == false)
                continue;
            WriteDockerMod(dm);
        }
    }

    /// <summary>
    /// Writes a single DockerMod to the DockerMods directory
    /// </summary>
    /// <param name="mod">the DockerMod to write</param>
    private void WriteDockerMod(DockerMod mod)
    {
        if (Globals.IsDocker == false)
            return;
        if (mod.Enabled == false)
            return;
        if (string.IsNullOrWhiteSpace(mod.Code))
            return;

        try
        {
            var file = Path.Combine(DirectoryHelper.DockerModsDirectory, mod.Name + ".sh");
            File.WriteAllText(file, mod.Code);
        }
        catch (Exception ex)
        {
            Logger.Instance.WLog($"Failed writing DockerMod '{mod.Name}': {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes a DockerMod from disk
    /// </summary>
    /// <param name="mod">the DockerMod to delete</param>
    private void DeleteDockerModFromDisk(DockerMod mod)
    {
        var file = Path.Combine(DirectoryHelper.DockerModsDirectory, mod.Name + ".sh");
        if(File.Exists(file))
            File.Delete(file);
    }
}