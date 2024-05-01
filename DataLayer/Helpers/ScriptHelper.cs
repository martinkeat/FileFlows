using System.Reflection;
using FileFlows.Shared;

namespace FileFlows.DataLayer.Helpers;

/// <summary>
/// Helper to load scripts from the assembly
/// </summary>
class ScriptHelper
{

    /// <summary>
    /// Reads in a embedded SQL script
    /// </summary>
    /// <param name="dbType">The type of database this script is for</param>
    /// <param name="script">The script name</param>
    /// <param name="clean">if set to true, empty lines and comments will be removed</param>
    /// <returns>the sql script</returns>
    public static string GetSqlScript(string dbType, string script, bool clean = false)
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"FileFlows.DataLayer.Scripts.{dbType}.{script}";

            using Stream? stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                return string.Empty;
            using StreamReader reader = new StreamReader(stream);
            string resource = reader.ReadToEnd();

            if (clean)
                return SqlHelper.CleanSql(resource);
            return resource;
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog($"Failed getting embedded SQL script '{dbType}.{script}': {ex.Message}");
            return string.Empty;
        }
    }
}