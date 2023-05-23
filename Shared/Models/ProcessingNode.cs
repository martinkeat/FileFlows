using System.ComponentModel.DataAnnotations;
using FileFlows.Plugin;

namespace FileFlows.Shared.Models;

/// <summary>
/// A processing node used by FileFlows to process Library Files
/// </summary>
public class ProcessingNode: FileFlowObject
{
    /// <summary>
    /// Gets or sets the temporary path used by this node
    /// </summary>
    public string TempPath { get; set; }

    /// <summary>
    /// Gets or sets the address this node is located at, hostname or ip address
    /// </summary>
    public string Address { get; set; }

    /// <summary>
    /// Gets or sets when the node was last seen
    /// </summary>
    public DateTime LastSeen { get; set; }

    /// <summary>
    /// Gets or sets if this node is enabled
    /// </summary>
    public bool Enabled { get; set; }
    
    /// <summary>
    /// Gets or sets the priority of the processing node
    /// Higher the value, the higher the priority 
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets the type of operating system this node is running on
    /// </summary>
    public OperatingSystemType OperatingSystem { get; set; }
    /// <summary>
    /// Gets or sets the FileFlows version of this node
    /// </summary>
    public string Version { get; set; }
    
    /// <summary>
    /// Gets or sets a script to execute before a runner can start
    /// </summary>
    public string PreExecuteScript { get; set; }

    /// <summary>
    /// Gets or sets the number of flow runners this node can run simultaneously 
    /// </summary>
    public int FlowRunners { get; set; }

    /// <summary>
    /// Gets or sets the SignalrUrl this node uses
    /// </summary>
    public string SignalrUrl { get; set; }
    /// <summary>
    /// Gets or sets the mappings for this node
    /// </summary>
    public List<KeyValuePair<string, string>> Mappings { get; set; }
    /// <summary>
    /// Gets or sets the schedule for this node
    /// </summary>
    public string Schedule { get; set; }
    /// <summary>
    /// Gets or sets if the owner should not be changed
    /// </summary>
    public bool DontChangeOwner { get; set; }
    /// <summary>
    /// Gets or sets if permissions should not be set
    /// </summary>
    public bool DontSetPermissions { get; set; }
    /// <summary>
    /// Gets or sets the permissions to set
    /// </summary>
    public string Permissions { get; set; }

    /// <summary>
    /// Gets or sets if this node can process all libraries
    /// </summary>
    public ProcessingLibraries AllLibraries { get; set; }
    /// <summary>
    /// Gets or sets the libraries this node can process
    /// </summary>
    public List<ObjectReference> Libraries { get; set; }
    /// <summary>
    /// Gets or sets the maximum file size this node can process
    /// </summary>
    [Range(0, 10_000_000)]
    public int MaxFileSizeMb { get; set; }

    /// <summary>
    /// The directory separator used by this node
    /// </summary>
    public char DirectorySeperatorChar = System.IO.Path.DirectorySeparatorChar;

    /// <summary>
    /// Maps a path locally for this node
    /// </summary>
    /// <param name="path">The path to map</param>
    /// <returns>The path mapped locally for this node</returns>
    public string Map(string path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;
        if (Mappings != null && Mappings.Count > 0)
        {
            // convert all \ to / for now
            path = path.Replace("\\", "/");
            foreach (var mapping in Mappings)
            {
                if (string.IsNullOrEmpty(mapping.Value) || string.IsNullOrEmpty(mapping.Key))
                    continue;
                string pattern = Regex.Escape(mapping.Key.Replace("\\", "/"));
                string replacement = mapping.Value.Replace("\\", "/");
                path = Regex.Replace(path, "^" + pattern, replacement, RegexOptions.IgnoreCase);
            }
            // now convert / to path charcter
            if (DirectorySeperatorChar != '/')
                path = path.Replace('/', DirectorySeperatorChar);
            if(path.StartsWith("//")) // special case for SMB paths
                path = path.Replace('/', '\\');
        }
        return path;
    }
    
    /// <summary>
    /// Unmaps a path for this node
    /// </summary>
    /// <param name="path">The mapped path</param>
    /// <returns>The unmapped path</returns>
    public string UnMap(string path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;
        if (Mappings != null && Mappings.Count > 0)
        {
            foreach (var mapping in Mappings)
            {
                if (string.IsNullOrEmpty(mapping.Value) || string.IsNullOrEmpty(mapping.Key))
                    continue;
                path = Regex.Replace(path, "^" + Regex.Escape(mapping.Value), mapping.Key, RegexOptions.IgnoreCase);
                path = Regex.Replace(path, "^" + Regex.Escape(mapping.Value.Replace("\\", "/")), mapping.Key, RegexOptions.IgnoreCase);
                path = Regex.Replace(path, "^" + Regex.Escape(mapping.Value.Replace("/", "\\")), mapping.Key, RegexOptions.IgnoreCase);
            }
        }

        int forwardIndex = path.IndexOf("/");
        int backIndex = path.IndexOf("\\");
        if (forwardIndex >= 0 && backIndex >= 0)
        {
            // we have both slashes, need to use the first one
            (string correct, string incorrect) = backIndex < forwardIndex ? ("\\", "/") : ("/", "\\");
            path = path.Replace(incorrect, correct);
        }

        return path;
    }
}
