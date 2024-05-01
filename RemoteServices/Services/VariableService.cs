namespace FileFlows.RemoteServices;

/// <summary>
/// Service for communicating with FileFlows server for variables
/// </summary>
public class VariableService: RemoteService, IVariableService
{
    /// <summary>
    /// Gets all variables in the system
    /// </summary>
    /// <returns>all variables in the system</returns>
    public async Task<List<Variable>?> GetAllAsync()
    {
        try
        {
            var result = await HttpHelper.Get<List<Variable>>($"{ServiceBaseUrl}/remote/variables");
            if (result.Success == false)
                throw new Exception("Failed to load variables: " + result.Body);
            return result.Data ?? new ();
        }
        catch (Exception ex)
        {
            Logger.Instance?.WLog("Failed to get variables => " + ex.Message);
            return null;
        }
    }
}