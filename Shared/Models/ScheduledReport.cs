using FileFlows.Plugin;

namespace FileFlows.Shared.Models;

/// <summary>
/// A report that will be run on a specific scheduled and set to the user
/// </summary>
public class ScheduledReport: FileFlowObject
{
    /// <summary>
    /// Gets or sets the recipients
    /// </summary>
    public string[] Recipients { get; set; } = null!;

    /// <summary>
    /// Gets or sets the report to run
    /// </summary>
    public ObjectReference Report { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the report scheduled
    /// </summary>
    public ReportSchedule Schedule { get; set; }
    
    /// <summary>
    /// Gets or sets the date the report was last sent
    /// </summary>
    public DateTime LastSentUtc { get; set; }
    
    /// <summary>
    /// Gets or sets the interval for this schedule.
    /// If Monthly, this is the day of the month.
    /// If Weekly, this is the day of the week.
    /// Else, ignored.
    /// </summary>
    public int ScheduleInterval { get; set; }
    
    /// <summary>
    /// Gets or sets if this report is enabled
    /// </summary>
    public bool Enabled { get; set; }
    
    /// <summary>
    /// Gets or sets the direction for this report
    /// </summary>
    public int Direction { get; set; }
    /// <summary>
    /// Gets or sets the UIDs of the Libraries to run against
    /// </summary>
    public Guid[] Libraries { get; set; }
    /// <summary>
    /// Gets or sets the UIDs of the Nodes to run against
    /// </summary>
    public Guid[] Nodes { get; set; }
    /// <summary>
    /// Gets or sets the UIDs of the Flows to run against
    /// </summary>
    public Guid[] Flows { get; set; }
}

/// <summary>
/// The possible report schedules
/// </summary>
public enum ReportSchedule
{
    /// <summary>
    /// Report will be sent daily
    /// </summary>
    Daily = 1,
    /// <summary>
    /// Report will be sent weekly
    /// </summary>
    Weekly = 2,
    /// <summary>
    /// Report will be sent monthly
    /// </summary>
    Monthly = 3
}