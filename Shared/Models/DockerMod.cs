using NPoco;

namespace FileFlows.Shared.Models;

/// <summary>
/// A modification that can be run against the docker container
/// </summary>
public class DockerMod : FileFlowObject
{
    /// <summary>
    /// Gets or sets if this modification is from the repository
    /// </summary>
    public bool Repository { get; set; }
    
    /// <summary>
    /// Gets or sets the Author who wrote this DockerMod
    /// </summary>
    public string Author { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the description of this DockerMod
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the revision of this DockerMod
    /// </summary>
    public int Revision { get; set; }
    
    /// <summary>
    /// Gets or sets the Code of this DockerMod
    /// </summary>
    public string Code { get; set; }
    
    /// <summary>
    /// Gets or sets the Icon of the DockerMod
    /// </summary>
    public string Icon { get; set; }
    
    /// <summary>
    /// Gets or sets if this is enabled and should be run at startup
    /// </summary>
    public bool Enabled { get; set; }
    
    /// <summary>
    /// Gets or sets the latest online revision of this,
    /// Only available if this is an repository DockerMod
    /// </summary>
    [DbIgnore]
    [Ignore]
    public int LatestRevision { get; set; }
}