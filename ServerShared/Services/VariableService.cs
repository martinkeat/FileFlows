using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.ServerShared.Services;

/// <summary>
/// Interface for communicating with FileFlows server for variables
/// </summary>
public interface IVariableService
{
    /// <summary>
    /// Gets all variables in the system
    /// </summary>
    /// <returns>all variables in the system</returns>
    Task<List<Variable>> GetAllAsync();
}

/// <summary>
/// Service for communicating with FileFlows server for variables
/// </summary>
public class VariableService: Service, IVariableService
{
    /// <summary>
    /// Gets or sets a function to load an instance of a IVariableService
    /// </summary>
    public static Func<IVariableService> Loader { get; set; }

    /// <summary>
    /// Loads an instance of the variable service
    /// </summary>
    /// <returns>an instance of the variable service</returns>
    public static IVariableService Load()
    {
        if (Loader == null)
            return new VariableService();
        return Loader.Invoke();
    }
    
    /// <summary>
    /// Gets all variables in the system
    /// </summary>
    /// <returns>all variables in the system</returns>
    public async Task<List<Variable>> GetAllAsync()
    {
        try
        {
            var result = await HttpHelper.Get<List<Variable>>($"{ServiceBaseUrl}/api/variable");
            if (result.Success == false)
                throw new Exception("Failed to load variables: " + result.Body);
            return result.Data;
        }
        catch (Exception ex)
        {
            Logger.Instance?.WLog("Failed to get variables => " + ex.Message);
            return null;
        }
    }
}