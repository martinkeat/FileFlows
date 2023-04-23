using FileFlows.Plugin;

namespace FileFlows.Shared.Models;

/// <summary>
/// A script is a special function node that lets you reuse them
/// </summary>
public class Script:IUniqueObject<string>, IInUse
{
    /// <summary>
    /// Gets or sets the name of the script
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the javascript code of the script
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Gets or sets if this script is a from a repository and cannot be modified
    /// </summary>
    public bool Repository { get; set; }

    /// <summary>
    /// Gets or sets the type of script
    /// </summary>
    public ScriptType Type { get; set; }

    /// <summary>
    /// Gets or sets the UID of this script, which is the original name of it
    /// </summary>
    public string Uid { get; set; }
    
    /// <summary>
    /// Gets or sets the revision of the script
    /// </summary>
    public int Revision { get; set; }
    
    /// <summary>
    /// Gets or sets the latest revision of the script
    /// </summary>
    public int LatestRevision { get; set; }

    /// <summary>
    /// Gets or sets the remote path of the script
    /// </summary>
    public string Path { get; set; }
    
    /// <summary>
    /// Gets or sets the comment block for this script
    /// </summary>
    public string CommentBlock { get; set; }

    /// <summary>
    /// Gets or sets what is using this object
    /// </summary>
    public List<ObjectReference> UsedBy { get; set; }
}