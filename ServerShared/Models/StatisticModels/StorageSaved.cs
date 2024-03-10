namespace FileFlows.ServerShared.Models.StatisticModels;

public class StorageSaved
{
    public List<StorageSavedData> Data { get; set; } = new();
}

public class StorageSavedData
{
    public string Library { get; set; }
    public int TotalFiles { get; set; }
    public long FinalSize { get; set; }
    public long OriginalSize { get; set; }
}