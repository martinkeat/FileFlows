using FileFlows.Shared.Models;

namespace FileFlows.ServerShared.Models.StatisticModels;

/// <summary>
/// Represents the storage saved data, including information about libraries and their storage savings.
/// </summary>
public class StorageSaved
{
    /// <summary>
    /// Gets or sets the list of storage saved data entries.
    /// </summary>
    public List<StorageSavedData> Data { get; set; } = new();
}
