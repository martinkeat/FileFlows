using FileFlows.DataLayer.Helpers;
using FileFlows.DataLayer.Models;
using FileFlows.Plugin;
using FileFlows.ServerShared.Models;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace FileFlows.Managers;

/// <summary>
/// Plugin manager
/// </summary>
public class PluginManager : CachedManager<PluginInfo>
{
    /// <summary>
    /// Get the plugin info for a specific plugin by package name
    /// </summary>
    /// <param name="name">The package name of the plugin</param>
    /// <returns>The plugin info for the plugin</returns>
    public async Task<PluginInfo?> GetByPackageName(string name)
        => (await GetData()).FirstOrDefault(x => x.PackageName == name);

    /// <summary>
    /// Deletes plugin settings from the package name
    /// </summary>
    /// <param name="name">the package name</param>
    public Task DeletePluginSettings(string name)
        => DatabaseAccessManager.Instance.ObjectManager.DeleteByTypeAndName(typeof(PluginSettingsModel).FullName!, name);

    /// <summary>
    /// Gets all plugin settings with and the plugin settings, does not decrypt the data
    /// </summary>
    /// <returns>all plugin settings with and the plugin settings</returns>
    public Task<List<PluginSettingsModel>> GetAllPluginSettings()
        => DatabaseAccessManager.Instance.FileFlowsObjectManager.Select<PluginSettingsModel>();

    /// <summary>
    /// Gets all plugin settings with and the plugin settings, does not decrypt the data
    /// </summary>
    /// <param name="name">the name of the plugin settings</param>
    /// <returns>all plugin settings with and the plugin settings</returns>
    public Task<Result<PluginSettingsModel?>> GetPluginSettings(string name)
        => DatabaseAccessManager.Instance.FileFlowsObjectManager.GetByName<PluginSettingsModel>(name);

    /// <summary>
    /// Gets a plugin by its package name
    /// </summary>
    /// <param name="item">the plugin</param>
    /// <param name="ignoreCase">if case should be ignored</param>
    /// <returns>the plugin if found</returns>
    protected override async Task<PluginInfo?> GetByName(PluginInfo item, bool ignoreCase = true)
    {
        var all = await GetAll();
        return all.FirstOrDefault(x => x.PackageName.Equals(item.PackageName, StringComparison.InvariantCultureIgnoreCase));
    }

    /// <summary>
    /// Sets the plugin settings
    /// </summary>
    /// <param name="name">the name of the plugin</param>
    /// <param name="json">the plugin json</param>
    /// <param name="auditDetails">The audit details</param>
    public async Task SetPluginSettings(string name, string json, AuditDetails auditDetails)
    {
        var manager = DatabaseAccessManager.Instance.FileFlowsObjectManager;
        var existing = await GetPluginSettings(name);
        string? oldJson = null;
        Result<(DbObject dbo, bool changed)> result;
        if (existing.IsFailed == false && existing.Value != null)
        {
            oldJson = existing.Value.Json;
            existing.Value.Json = json;
            result = await manager.AddOrUpdateObject(existing.Value, null);
        }
        else
        {
            result = await manager.AddOrUpdateObject(new PluginSettingsModel()
            {
                Name = name,
                DateCreated = DateTime.UtcNow,
                Json = json
            }, null, saveRevision: true);
        }
        
        if (result.IsFailed || result.Value.changed == false || auditDetails == null)
            return;

        try
        {
            var oldSettings = oldJson == null ? null : JsonSerializer.Deserialize<IDictionary<string, object>>(oldJson);
            var newSettings = JsonSerializer.Deserialize<IDictionary<string, object>>(json);
            var diff = IDictionaryConverter.GetDifferences(newSettings, oldSettings, null, null);
            if (diff?.Any() != true)
                return;
            Dictionary<string, object> changes = new();
            foreach (var d in diff)
            {
                if (d == null)
                    continue; // shouldnt happen
                int index = d.IndexOf(": ", StringComparison.Ordinal);
                if (index < 1)
                    continue;
                var key = d[..index];
                var value = d[(index + 2)..];
                if (value?.ToLowerInvariant() == "removed" || value?.Contains("' to ''", StringComparison.InvariantCulture) == true)
                    changes[key] = "Removed";
                else
                    changes[key] = "Updated";// we don't want to audit these settings as they are usually sensitive data, eg access tokens
            }

            await new AuditManager().Audit(new()
            {
                ObjectType = result.Value.dbo.Type,
                Action = AuditAction.Updated,
                LogDate = DateTime.UtcNow,
                ObjectUid = result.Value.dbo.Uid,
                OperatorName = auditDetails.UserName,
                OperatorType = auditDetails.OperatorType,
                IPAddress = auditDetails.IPAddress,
                Parameters = new()
                {
                    { "Name", name }
                },
                Changes = changes
            });
        }
        catch (Exception)
        {
        }
    }
}