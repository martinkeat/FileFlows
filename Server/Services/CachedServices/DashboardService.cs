using FileFlows.ServerShared.Services;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// Dashboard Service
/// </summary>
public class DashboardService : CachedService<Dashboard>
{
    /// <summary>
    /// Dashboards do not need to update the configuration
    /// as they do not effect configuration on a Flow Runner
    /// </summary>
    public override bool IncrementsConfiguration => false;
}