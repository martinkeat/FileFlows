namespace FileFlows.Shared.Widgets;

/// <summary>
/// Widget definition, these are the different types of Widgets in the system
/// </summary>
public abstract class WidgetDefinition
{
    /// <summary>
    /// Gets a Widget definition from its UID
    /// </summary>
    /// <param name="uid">The UID of the Widget definition</param>
    /// <returns>the Widget definition</returns>
    public static WidgetDefinition GetDefinition(Guid uid)
    {
        if (uid == Processing.WD_UID)
            return new Processing();
        if (uid == FilesRecentlyFinished.WD_UID)
            return new FilesRecentlyFinished();
        if (uid == FilesUpcoming.WD_UID)
            return new FilesUpcoming();
        if (uid == AudioCodecs.WD_UID)
            return new AudioCodecs();
        if (uid == Codecs.WD_UID)
            return new Codecs();
        if (uid == Encoders.WD_UID)
            return new Encoders();
        if (uid == Decoders.WD_UID)
            return new Decoders();
        if (uid == Decoders.WD_UID)
            return new Decoders();
        if (uid == DecoderParameters.WD_UID)
            return new DecoderParameters();
        if (uid == ComicFormats.WD_UID)
            return new ComicFormats();
        if (uid == ComicPages.WD_UID)
            return new ComicPages();
        if (uid == CpuUsage.WD_UID)
            return new CpuUsage();
        if (uid == ImageFormats.WD_UID)
            return new ImageFormats();
        if (uid == LibraryProcessingTimes.WD_UID)
            return new LibraryProcessingTimes();
        if (uid == LogStorage.WD_UID)
            return new LogStorage();
        if (uid == MemoryUsage.WD_UID)
            return new MemoryUsage();
        if (uid == ProcessingTimes.WD_UID)
            return new ProcessingTimes();
        if (uid == TempStorage.WD_UID)
            return new TempStorage();
        if (uid == VideoCodecs.WD_UID)
            return new VideoCodecs();
        if (uid == VideoContainers.WD_UID)
            return new VideoContainers();
        if (uid == AudioContainers.WD_UID)
            return new AudioContainers();
        if (uid == VideoResolution.WD_UID)
            return new VideoResolution();
        if (uid == OpenDatabaseConnections.WD_UID)
            return new OpenDatabaseConnections();
        if (uid == StorageSaved.WD_UID)
            return new StorageSaved();
        if (uid == NvidiaSmi.WD_UID)
            return new NvidiaSmi();
        if (uid == ProcessingNodes.WD_UID)
            return new ProcessingNodes();
        if (uid == ProcessingNodes.WD_UID)
            return new ProcessingNodes();
        if (uid == CurrentRevision.WD_UID)
            return new CurrentRevision();
        throw new Exception("Unknown widget: " + uid);
    }
    
    /// <summary>
    /// Gets the UID 
    /// </summary>
    public abstract Guid Uid { get; }
    /// <summary>
    /// Gets the URL
    /// </summary>
    public abstract string Url { get; }
    /// <summary>
    /// Gets the Icon
    /// </summary>
    public abstract string Icon { get; }
    /// <summary>
    /// Gets the Name
    /// </summary>
    public abstract string Name { get; }
    /// <summary>
    /// Gets the type of Widget
    /// </summary>
    public abstract WidgetType Type { get; }
    /// <summary>
    /// Gets any flags 
    /// </summary>
    public abstract int Flags { get; }
}


/// <summary>
/// Available Widget types
/// </summary>
public enum WidgetType
{
    /// <summary>
    /// Processing files
    /// </summary>
    Processing = 1,
    /// <summary>
    /// Table of library files
    /// </summary>
    LibraryFileTable = 2,
    /// <summary>
    /// Processing nodes overview
    /// </summary>
    ProcessingNodes = 3,
    /// <summary>
    /// Box plot 
    /// </summary>
    BoxPlot = 101,
    /// <summary>
    /// Heat map
    /// </summary>
    HeatMap = 102,
    /// <summary>
    /// Pie chart
    /// </summary>
    PieChart = 103,
    /// <summary>
    /// Tree map
    /// </summary>
    TreeMap = 104,
    /// <summary>
    /// Time series percentage
    /// </summary>
    TimeSeries = 105,
    /// <summary>
    /// Bar graph
    /// </summary>
    Bar = 106,
    /// <summary>
    /// Bell curve
    /// </summary>
    BellCurve = 107,
    /// <summary>
    /// Counter
    /// </summary>
    Counter = 108,
    /// <summary>
    /// Key Value Table
    /// </summary>
    KeyValueTable = 109,
    /// <summary>
    /// Table that shows the totals
    /// </summary>
    TotalsTable = 110,
    /// <summary>
    /// Nvidia Chart
    /// </summary>
    Nvidia = 121
    
}