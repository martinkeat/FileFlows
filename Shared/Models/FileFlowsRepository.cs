namespace FileFlows.Shared.Models;

/// <summary>
/// The FileFlows repository
/// </summary>
public class FileFlowsRepository
{
    /// <summary>
    /// Gets or sets the shared scripts
    /// </summary>
    public List<RepositoryObject> SharedScripts { get; set; } = new ();
    /// <summary>
    /// Gets or sets the system scripts
    /// </summary>
    public List<RepositoryObject> SystemScripts { get; set; } = new ();
    /// <summary>
    /// Gets or sets the webhook scripts
    /// </summary>
    public List<RepositoryObject> WebhookScripts { get; set; } = new ();
    /// <summary>
    /// Gets or sets the flow scripts
    /// </summary>
    public List<RepositoryObject> FlowScripts { get; set; } = new ();
    /// <summary>
    /// Gets or sets the function scripts
    /// </summary>
    public List<RepositoryObject> FunctionScripts { get; set; } = new ();
    
    
    /// <summary>
    /// Gets or sets the flow templates
    /// </summary>
    public List<RepositoryObject> FlowTemplates { get; set; } = new ();
    /// <summary>
    /// Gets or sets the library templates
    /// </summary>
    public List<RepositoryObject> LibraryTemplates { get; set; } = new ();
}

/// <summary>
/// A Remote script
/// </summary>
public class RepositoryObject
{
    /// <summary>
    /// Gets or sets the path of the script
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the revision of the script
    /// </summary>
    public int Revision { get; set; }

    /// <summary>
    /// Gets or sets the minimum version of FileFlows this object requires
    /// </summary>
    public Version MinimumVersion { get; set; }
}