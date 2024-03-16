namespace FileFlows.Managers;

/// <summary>
/// Service for communicating with FileFlows server for flows
/// </summary>
public class FlowManager : CachedManager<Flow>
{
    static FlowManager()
        => new FlowManager().Refresh().Wait();
    
    /// <inheritdoc />
    protected override bool SaveRevisions => true;

    /// <summary>
    /// Gets the Failure Flow for a specific library
    /// This is the flow that is called if the flow fails 
    /// </summary>
    /// <param name="libraryUid">The UID of the library</param>
    /// <returns>An instance of the Failure Flow if found</returns>
    public async Task<Flow?> GetFailureFlow(Guid libraryUid)
    {
        var data = await GetData();
        return data.FirstOrDefault(x => x.Type == FlowType.Failure && x.Enabled && x.Default);
    }

    /// <summary>
    /// Gets if there are any flows in the system
    /// </summary>
    /// <returns>true if there are any flows</returns>
    public Task<bool> HasAny()
        => DatabaseAccessManager.Instance.ObjectManager.Any(typeof(Flow).FullName!);
}
