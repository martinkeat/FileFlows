using System.Text.RegularExpressions;
using FileFlows.Plugin;
using FileFlows.Server.Controllers;
using FileFlows.Server.Database.Managers;
using FileFlows.Shared.Models;
using NPoco;

namespace FileFlows.Server.Helpers;

/// <summary>
/// Database helper for communicating with the database
/// </summary>
public class DbHelper
{
    private static Server.Database.Managers.DbManager Manager;
    
    /// <summary>
    /// Gets or sets if the server is using a SQLite database
    /// </summary>
    public static bool IsSqlLite { get; set; }

    /// <summary>
    /// Initializes the DbHelper with an instance of a DbManager
    /// </summary>
    internal static Task<bool> Initialize()
    {
        DatabaseFactory.ColumnSerializer = new JsonColumnSerializer();
        
        string connstring = AppSettings.Instance.DatabaseConnection;
        if (string.IsNullOrWhiteSpace(connstring) == false)
        {
            string logConnection = Regex.Replace(connstring, "(?<=(Password=))[^;]+(;|$)", "#PASSWORD#");
            Logger.Instance.ILog("Initializing Database: " + logConnection);
        }

        Manager = DbManager.GetManager(connstring);
        return Manager.CreateDb();
    }

    internal static DbManager GetDbManager() => Manager;

    /// <summary>
    /// Gets if the database manager should use a memory cache
    /// </summary>
    internal static bool UseMemoryCache => Manager.UseMemoryCache;


    /// <summary>
    /// Select a list of objects
    /// </summary>
    /// <typeparam name="T">the type of objects to select</typeparam>
    /// <returns>a list of objects</returns>
    public static Task<IEnumerable<T>> Select<T>() where T : FileFlowObject, new() => Manager.Select<T>();

    /// <summary>
    /// Selects types from the database
    /// </summary>
    /// <param name="where">a where clause for the select</param>
    /// <param name="arguments">the arguments for the select</param>
    /// <typeparam name="T">the type of object to select</typeparam>
    /// <returns>a list of objects</returns>
    public static Task<IEnumerable<T>> Select<T>(string where, params object[] arguments)
        where T : FileFlowObject, new()
        => Manager.Select<T>(where, arguments);

    /// <summary>
    /// Get names of types
    /// </summary>
    /// <param name="andWhere">and where clause</param>
    /// <param name="args">arguments for where clause</param>
    /// <typeparam name="T">the type to select</typeparam>
    /// <returns>a list of names</returns>
    public static Task<IEnumerable<string>> GetNames<T>(string andWhere = "", params object[] args) =>
        Manager.GetNames<T>(andWhere, args);

    /// <summary>
    /// Get names of types indexed by their UID
    /// </summary>
    /// <param name="andWhere">and where clause</param>
    /// <param name="args">arguments for where clause</param>
    /// <typeparam name="T">the type to select</typeparam>
    /// <returns>a list of names</returns>
    public static Task<Dictionary<Guid, string>> GetIndexedNames<T>(string andWhere = "", params object[] args) =>
        Manager.GetIndexedNames<T>(andWhere, args);

    /// <summary>
    /// Checks to see if a name is in use
    /// </summary>
    /// <param name="uid">the Uid of the item</param>
    /// <param name="name">the name of the item</param>
    /// <returns>true if name is in use</returns>
    public static Task<bool> NameInUse<T>(Guid uid, string name) => Manager.NameInUse<T>(uid, name);

    /// <summary>
    /// Select a single instance of a type
    /// </summary>
    /// <typeparam name="T">The type to select</typeparam>
    /// <returns>a single instance</returns>
    public static Task<T> Single<T>() where T : FileFlowObject, new()
        => Manager.Single<T>();

    /// <summary>
    /// Select a single DbObject
    /// </summary>
    /// <param name="uid">The UID of the object</param>
    /// <returns>a single instance</returns>
    internal static Task<DbObject> SingleDbo(Guid uid) => Manager.SingleDbo(uid);
    
    /// <summary>
    /// Selects a single instance
    /// </summary>
    /// <param name="uid">the UID of the item to select</param>
    /// <typeparam name="T">the type of item to select</typeparam>
    /// <returns>a single instance</returns>
    public static Task<T> Single<T>(Guid uid) where T : FileFlowObject, new()
        => Manager.Single<T>(uid);

    /// <summary>
    /// Selects a single instance by its name
    /// </summary>
    /// <param name="name">the name of the item to select</param>
    /// <typeparam name="T">the type of object to select</typeparam>
    /// <returns>a single instance</returns>
    public static Task<T> SingleByName<T>(string name) where T : FileFlowObject, new()
        => Manager.SingleByName<T>(name);

    /// <summary>
    /// Updates the last modified date of an object
    /// </summary>
    /// <param name="uid">the UID of the object to update</param>
    internal static Task UpdateLastModified(Guid uid)
        => Manager.UpdateLastModified(uid);

    /// <summary>
    /// This will batch insert many objects into thee database
    /// </summary>
    /// <param name="items">Items to insert</param>
    internal static Task AddMany(FileFlowObject[] items) => Manager.AddMany(items);

    /// <summary>
    /// Selects a single item from the database
    /// </summary>
    /// <param name="andWhere">the and where clause</param>
    /// <param name="args">any parameters to the select statement</param>
    /// <typeparam name="T">the type of object to select</typeparam>
    /// <returns>an single instance</returns>
    public static Task<T> Single<T>(string andWhere, params object[] args) where T : FileFlowObject, new()
        => Manager.Single<T>(andWhere, args);


    /// <summary>
    /// Updates an object
    /// </summary>
    /// <param name="obj">the object to update</param>
    /// <typeparam name="T">the object type</typeparam>
    /// <returns>the updated object</returns>
    public static Task<T> Update<T>(T obj) where T : FileFlowObject, new()
        => Manager.Update<T>(obj);


    /// <summary>
    /// Delete items from a database
    /// </summary>
    /// <param name="uids">the UIDs of the items to delete</param>
    /// <typeparam name="T">The type of objects being deleted</typeparam>
    public static Task Delete<T>(params Guid[] uids) where T : FileFlowObject
        => Manager.Delete<T>(uids);


    /// <summary>
    /// Delete items from a database
    /// </summary>
    /// <param name="uids">the UIDs of the items to delete</param>
    public static Task Delete(params Guid[] uids) => Manager.Delete(uids);

    /// <summary>
    /// Delete items from a database
    /// </summary>
    /// <param name="andWhere">and where clause</param>
    /// <param name="args">arguments for where clause</param>
    /// <typeparam name="T">the type to delete</typeparam>
    public static Task Delete<T>(string andWhere = "", params object[] args) => Manager.Delete<T>(andWhere, args);

    /// <summary>
    /// Executes SQL against the database
    /// </summary>
    /// <param name="sql">the SQL to execute</param>
    /// <param name="args">arguments for where clause</param>
    /// <returns>the rows effected</returns>
    internal static Task<int> Execute(string sql = "", params object[] args) => Manager.Execute(sql, args);
    
    /// <summary>
    /// Logs a message to the database
    /// </summary>
    /// <param name="clientUid">The UID of the client, use Guid.Empty for the server</param>
    /// <param name="type">the type of log message</param>
    /// <param name="message">the message to log</param>
    public static Task Log(Guid clientUid, LogType type, string message) => Manager.Log(clientUid, type, message);
    /// <summary>
    /// Prune old logs from the database
    /// </summary>
    /// <param name="maxLogs">the maximum number of log messages to keep</param>
    public static Task PruneOldLogs(int maxLogs) => Manager.PruneOldLogs(maxLogs);

    /// <summary>
    /// Searches the log using the given filter
    /// </summary>
    /// <param name="filter">the search filter</param>
    /// <returns>the messages found in the log</returns>
    public static Task<IEnumerable<DbLogMessage>> SearchLog(LogSearchModel filter) => Manager.SearchLog(filter);
    
    /// <summary>
    /// Gets the failure flow for a particular library
    /// </summary>
    /// <param name="libraryUid">the UID of the library</param>
    /// <returns>the failure flow</returns>
    public static Task<Flow> GetFailureFlow(Guid libraryUid)
        => Manager.GetFailureFlow(libraryUid);

    /// <summary>
    /// Gets an item from the database by it's name
    /// </summary>
    /// <param name="name">the name of the object</param>
    /// <typeparam name="T">the type to fetch</typeparam>
    /// <returns>the object if found</returns>
    public static Task<T> GetByName<T>(string name) where T : FileFlowObject, new()
        => Manager.GetByName<T>(name);
   
    
    /// <summary>
    /// Records a statistic
    /// </summary>
    /// <param name="statistic">the statistic to record</param>
    public static Task RecordStatistic(Statistic statistic) => Manager.RecordStatistic(statistic);
    

    /// <summary>
    /// Gets statistics by name
    /// </summary>
    /// <returns>the matching statistics</returns>
    public static Task<IEnumerable<Statistic>> GetStatisticsByName(string name) => Manager.GetStatisticsByName(name);
    
    /// <summary>
    /// Checks if the database has any of the type
    /// </summary>
    /// <typeparam name="T">The type</typeparam>
    /// <param name="where">additional where clause</param>
    /// <returns>true if has any of the time</returns>
    public static async Task<bool> HasAny<T>(string where = "") where T : FileFlowObject
    {
        var manager = GetDbManager();
        string sql = $"select count(uid) from DbObject where Type = '{typeof(T).FullName}'";
        if (string.IsNullOrWhiteSpace(where) == false)
            sql += " and " + where;
        int count = await manager.ExecuteScalar<int>(sql);
        return count > 0;
    }

    /// <summary>
    /// Updates the last seen of a node
    /// </summary>
    /// <param name="uid">the UID of the node</param>
    public static Task UpdateNodeLastSeen(Guid uid) => Manager.UpdateNodeLastSeen(uid);
    

    /// <summary>
    /// Updates a value in the json data of a DbObject
    /// </summary>
    /// <param name="uid">the UID of the object</param>
    /// <param name="property">The name of the property</param>
    /// <param name="value">the value to update</param>
    /// <returns>>the awaited task</returns>
    public static Task UpdateJsonProperty(Guid uid, string property, object value)
        => Manager.UpdateJsonProperty(uid, property, value);
    
    /// <summary>
    /// Restores defaults from the database if they have been removed
    /// </summary>
    public static void RestoreDefaults()
    {
        var manager = GetDbManager();
        var variables = manager.Fetch<string>("select name from DbObject where Type = 'FileFlows.Shared.Models.Variable'").Result.ToList();

        var ffmpeg = variables.FirstOrDefault(x => x.ToLowerInvariant() == "ffmpeg");
        if (ffmpeg == null)
        {
            // doesnt exist, insert it
            try
            {
                manager.Update(new Variable()
                {
                    Name = "ffmpeg",
                    Value = Globals.IsWindows
                        ? Path.Combine(DirectoryHelper.BaseDirectory, @"Tools\ffmpeg.exe")
                        : "/usr/local/bin/ffmpeg"
                }).Wait();
            }
            catch (Exception ex)
            {
                Logger.Instance.ELog("Error inserting ffmpeg: " + ex.Message);
            }
        }
        else if (ffmpeg != "ffmpeg")
        {
            // not lower case
            manager.Execute("update DbObject set Name = 'ffmpeg' where Name like 'ffmpeg' and Type = 'FileFlows.Shared.Models.Variable'", null).Wait();
        }

        string programFiles = Environment.ExpandEnvironmentVariables("%ProgramW6432%");
        
        foreach (var variable in new[]
                 {
                     ("ffprobe", Globals.IsWindows ? Path.Combine(DirectoryHelper.BaseDirectory, @"Tools\ffprobe.exe") : "/usr/local/bin/ffprobe"), 
                     ("unrar", Globals.IsWindows ? Path.Combine(programFiles, "WinRAR", "UnRAR.exe") : "unrar"), 
                     ("rar", Globals.IsWindows ? Path.Combine(programFiles, "WinRAR", "Rar.exe") : "rar"), 
                     ("7zip", Globals.IsWindows ? Path.Combine(programFiles, "7-Zip", "7z.exe") : "7z")
                 })
        {
            if (variables.Contains(variable.Item1))
                continue;
            // doesnt exist, insert it
            try
            {
                manager.Update(new Variable()
                {
                    Name = variable.Item1,
                    Value = variable.Item2
                }).Wait();
            }
            catch (Exception ex)
            {
                Logger.Instance.ELog($"Error inserting '{variable.Item1}: " + ex.Message);
            }
        }
    }
}