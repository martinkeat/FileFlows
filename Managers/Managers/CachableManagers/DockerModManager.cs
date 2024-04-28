namespace FileFlows.Managers;

/// <summary>
/// Manager for the DockerMods
/// </summary>
public class DockerModManager : CachedManager<DockerMod>
{
    /// <inheritdoc />
    protected override bool SaveRevisions => true;
}