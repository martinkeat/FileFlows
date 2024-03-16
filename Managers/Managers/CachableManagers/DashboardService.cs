namespace FileFlows.Managers;

/// <summary>
/// Dashboard Manager
/// </summary>
public class DashboardManager : CachedManager<Dashboard>
{
    /// <summary>
    /// Dashboards do not need to update the configuration
    /// as they do not effect configuration on a Flow Runner
    /// </summary>
    public override bool IncrementsConfiguration => false;

    /// <inheritdoc />
    protected override bool SaveRevisions => true;
}