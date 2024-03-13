namespace FileFlows.Shared.Models;

/// <summary>
/// Represents the storage saved data for a specific library.
/// </summary>
public class StorageSavedData
{
    /// <summary>
    /// Gets or sets the name of the library.
    /// </summary>
    public string Library { get; set; }

    /// <summary>
    /// Gets or sets the total number of files saved in the library.
    /// </summary>
    public int TotalFiles { get; set; }

    /// <summary>
    /// Gets or sets the final size of the files saved in the library (in bytes).
    /// </summary>
    public long FinalSize { get; set; }

    /// <summary>
    /// Gets or sets the original size of the files in the library (in bytes).
    /// </summary>
    public long OriginalSize { get; set; }
}