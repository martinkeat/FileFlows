namespace FileFlows.Plugin.Models;


/// <summary>
/// Represents detailed information about a file.
/// </summary>
public class FileInformation
{
    /// <summary>
    /// Gets or sets the local time when the file was created.
    /// </summary>
    public DateTime CreationTime { get; set; }

    /// <summary>
    /// Gets or sets the Coordinated Universal Time (UTC) when the file was created.
    /// </summary>
    public DateTime CreationTimeUtc { get; set; }

    /// <summary>
    /// Gets or sets the local time when the file was last written to.
    /// </summary>
    public DateTime LastWriteTime { get; set; }

    /// <summary>
    /// Gets or sets the Coordinated Universal Time (UTC) when the file was last written to.
    /// </summary>
    public DateTime LastWriteTimeUtc { get; set; }

    /// <summary>
    /// Gets or sets the file extension without the '.' character.
    /// </summary>
    public string Extension { get; set; }

    /// <summary>
    /// Gets or sets the short filename of the file.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the full filename of the file.
    /// </summary>
    public string FullName { get; set; }
    
    /// <summary>
    /// Gets or sets the length/size of the file in bytes
    /// </summary>
    public long Length { get; set; }

    /// <summary>
    /// Gets or sets the path containing the file.
    /// </summary>
    public string Directory { get; set; }
}