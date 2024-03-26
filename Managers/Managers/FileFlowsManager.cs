using FileFlows.ServerShared;

namespace FileFlows.Managers;

/// <summary>
/// System FileFlows manager
/// </summary>
public class FileFlowsManager
{
    /// <summary>
    /// Get the version number
    /// </summary>
    /// <returns>a version number</returns>
    public Task<Version?> Get()
        => DatabaseAccessManager.Instance.VersionManager.Get();
    
    /// <summary>
    /// Sets the version
    /// </summary>
    /// <param name="version">the version</param>
    public Task SetVersion(string version)
        => DatabaseAccessManager.Instance.VersionManager.Set(version);
}