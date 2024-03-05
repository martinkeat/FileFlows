namespace FileFlows.Managers;

/// <summary>
/// Service for communicating with FileFlows server for flows
/// </summary>
public class FlowManager : CachedManager<Flow>
{
    static FlowManager()
        => new FlowManager().Refresh();

    /// <summary>
    /// Gets the Failure Flow for a specific library
    /// This is the flow that is called if the flow fails 
    /// </summary>
    /// <param name="libraryUid">The UID of the library</param>
    /// <returns>An instance of the Failure Flow if found</returns>
    public async Task<Flow?> GetFailureFlow(Guid libraryUid)
    {
        var data = UseCache ? Data : await LoadDataFromDatabase();
        return data.FirstOrDefault(x => x.Type == FlowType.Failure && x.Enabled && x.Default);
    }

}
