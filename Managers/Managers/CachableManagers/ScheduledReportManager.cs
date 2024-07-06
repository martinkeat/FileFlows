namespace FileFlows.Managers;

/// <summary>
/// Manager for the scheduled reports
/// </summary>
public class ScheduledReportManager : CachedManager<ScheduledReport>
{
    /// <inheritdoc />
    protected override bool SaveRevisions => true;
}