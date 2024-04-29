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
    public Task<Result<DockerMod>> Save(DockerMod mod, AuditDetails? auditDetails)
        => new DockerModManager().Update(mod, auditDetails);

    
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
        // Serialize the DockerMod object to YAML
        var match = Regex.Match(content, "(?s)# [-]{60,}(.*?)# [-]{60,}");
        if (match?.Success != true)
            return Result<bool>.Fail("Invalid script content");

        var head = match.Value;
        content = content.Replace(head, string.Empty).Trim();
        
        string yaml = string.Join("\n", head.Split('\n').Where(x => x.StartsWith("# -----") == false)
            .Select(x => x[2..]));
        string code = content;
        
        // Deserialize YAML to DockerMod object
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .Build();

        var mod = deserializer.Deserialize<DockerMod>(yaml);
        mod.Code = code;
        mod.Repository = true;

        var result = await Save(mod, auditDetails);
        if (result.Failed(out string error))
            return Result<bool>.Fail(error);

        return true;
    }
}