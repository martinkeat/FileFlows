using FileFlows.Plugin;
using FileFlows.Shared.Models;
using Jint;

namespace FileFlows.Server.Services;

/// <summary>
/// Service to convert a file name to a display name
/// </summary>
public class FileDisplayNameService
{
    /// <summary>
    /// If no script is loaded and should not be called
    /// </summary>
    private static bool NoScript = true;
    /// <summary>
    /// The engine to execute the script
    /// </summary>
    private static Engine jsGetDisplayName;
    
    /// <summary>
    /// Initializes the service
    /// </summary>
    public static void Initialize()
    {
        string code = null;
        try
        {
            var service = new ScriptService();
            bool exists = service.Exists(Globals.FileDisplayNameScript, ScriptType.System);
            if (exists)
            {
                var script = service.Get(Globals.FileDisplayNameScript, ScriptType.System).Result;
                code = script.Code;
            }
        }
        catch(Exception)
        {
            jsGetDisplayName = null;
            NoScript = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            jsGetDisplayName = null;
            NoScript = true;
            return;
        }

        try
        {
            jsGetDisplayName= new Engine().Execute(code);
            NoScript = false;
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog($"Error in {Globals.FileDisplayNameScript} script: " + ex.Message);
            NoScript = true;
        }
    }

    /// <summary>
    /// Gets the display name for the file
    /// </summary>
    /// <param name="libFile">the library file</param>
    /// <returns>the display name</returns>
    public static string GetDisplayName(LibraryFile libFile)
    {
        return GetDisplayName(libFile.Name, libFile.RelativePath, libFile.LibraryName);
    }
    
    /// <summary>
    /// Gets the display name for the file
    /// </summary>
    /// <param name="name">the original name</param>
    /// <param name="relativePath">the relative path</param>
    /// <param name="libraryName">the name of the library</param>
    /// <returns>the display name</returns>
    public static string GetDisplayName(string name, string relativePath, string libraryName)
    {
        if (NoScript || jsGetDisplayName == null)
            return relativePath?.EmptyAsNull() ?? name;
        try
        {
            lock (jsGetDisplayName)
            {
                return jsGetDisplayName.Invoke("getDisplayName", name, relativePath, libraryName)?.ToString() ?? name;
            }
        }
        catch (Exception ex)
        {
            Logger.Instance.ILog("Error getting display name: " + ex.Message);
            return relativePath;
        }
    }
}