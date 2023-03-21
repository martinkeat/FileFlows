using System.Reflection;
using System.Text.RegularExpressions;
using FileFlows.Server.Database.Managers;
using FileFlows.Server.Helpers;
using FileFlows.Shared.Json;
using FileFlows.Shared.Models;
using Jint.Runtime.Debugger;
using NPoco;

namespace FileFlows.Server.Services;

/// <summary>
/// Service for communicating with FileFlows server for library files
/// </summary>
public partial class LibraryFileService
{   

    private const string LIBRARY_JOIN =
        " left join DbObject obj on obj.Type = 'FileFlows.Shared.Models.Library' and LibraryFile.LibraryUid = obj.Uid ";

    private void Database_Log(DateTime start, string message)
    {
        var time = DateTime.Now.Subtract(start);
        if (time > new TimeSpan(0, 0, 10))
            Logger.Instance.ELog($"Took '{time}' to " + message);
        else if (time > new TimeSpan(0, 0, 3))
            Logger.Instance.WLog($"Took '{time}' to " + message);
        else if (time > new TimeSpan(0, 0, 1))
            Logger.Instance.DLog($"Took '{time}' to " + message);
    }

    private static async Task<FlowDbConnection> GetDbWithMappings()
    {
        var db = await DbHelper.GetDbManager().GetDb();
        db.Db.Mappers.Add(new CustomDbMapper());
        return db;
    }

    private async Task<T> Database_Get<T>(string sql, params object[] args)
    {
        T result;
        DateTime dt = DateTime.Now;
        using (var db = await GetDbWithMappings())
        {
            DateTime dt2 = DateTime.Now;
            try
            {
                result = await db.Db.FirstOrDefaultAsync<T>(sql, args);
            }
            catch (Exception ex)
            {
                Logger.Instance.ELog("Failed getting SQL: " + sql + ", error: " + ex.Message);
                return default;
            }
            Database_Log(dt2, "get (actual): " + Regex.Replace(sql, @"\s\s+", " ").Trim());
        }
        Database_Log(dt, "get: " + Regex.Replace(sql, @"\s\s+", " ").Trim());
        return result;
    }
    
    private async Task<List<T>> Database_Fetch<T>(string sql, params object[] args)
    {
        List<T> results;
        DateTime dt = DateTime.Now;
        try
        {
            using (var db = await GetDbWithMappings())
            {        
                DateTime dt2 = DateTime.Now;
                results = await db.Db.FetchAsync<T>(sql, args);
                Database_Log(dt2, "fetch (actual): " + Regex.Replace(sql, @"\s\s+", " ").Trim());
            }
        }
        finally
        {
            Database_Log(dt,"fetch: " + Regex.Replace(sql, @"\s\s+", " ").Trim());
        }

        return results;
    }

    private async Task Database_Execute(string sql, params object[] args)
    {
        DateTime dt = DateTime.Now;
        int effected;
        using (var db = await GetDbWithMappings())
        {
            DateTime dt2 = DateTime.Now;
            effected = await db.Db.ExecuteAsync(sql, args);
            Logger.Instance.DLog($"Took '{(DateTime.Now - dt2)}' to execute (actual)[{effected}]: " + Regex.Replace(sql, @"\s\s+", " ").Trim());
        }
        Logger.Instance.DLog($"Took '{(DateTime.Now - dt)}' to execute [{effected}]: " + Regex.Replace(sql, @"\s\s+", " ").Trim());
    }
    private async Task<T> Database_ExecuteScalar<T>(string sql, params object[] args)
    {
        T result;
        DateTime dt = DateTime.Now;
        using (var db = await GetDbWithMappings())
        {
            DateTime dt2 = DateTime.Now;
            result = await db.Db.ExecuteScalarAsync<T>(sql, args);
            Database_Log(dt2, "execute (actual): " + Regex.Replace(sql, @"\s\s+", " ").Trim());
        }
        Database_Log(dt, "execute: " + Regex.Replace(sql, @"\s\s+", " ").Trim());
        return result;
    }

    private static readonly string[] LibraryFileUpdateColums = typeof(LibraryFile).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x =>
    {
        return x.GetCustomAttribute<IgnoreAttribute>() == null;
    }).Select(x => x.Name).ToArray();

    private void Database_Update(LibraryFile o)
        => AddOrUpdate(true, o).Wait();

    private async Task Database_Insert(LibraryFile o)
    {
        try
        {
            await AddOrUpdate(false, o);
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog($"Failed insert object: " + ex.Message);
            throw;
        }
    }

    static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions()
    {
        Converters = { new TimeSpanConverter() }
    };
    
    
    /// <summary>
    /// Adds or updates a library file
    /// </summary>
    /// <param name="update">true if updating, otherwise false to add</param>
    /// <param name="file">the library file</param>
    private async Task AddOrUpdate(bool update, LibraryFile file)
    {
        DateTime dt = DateTime.Now;
        using var db = await GetDbWithMappings();
        DateTime dt2 = DateTime.Now;
        
        file.Fingerprint ??= string.Empty;
        file.FinalFingerprint ??= string.Empty;
        file.FlowName ??= string.Empty;
        file.NodeName ??= string.Empty;
        file.OutputPath ??= string.Empty;
        file.LibraryName ??= string.Empty;
        file.RelativePath ??= string.Empty;
        file.DuplicateName ??= string.Empty;
        
        string sql;
        string strOriginalMetadata, strFinalMetadata, strExecutedNodes;
        if (update)
        {
            strOriginalMetadata = JsonEncode(file.OriginalMetadata);
            strFinalMetadata = JsonEncode(file.FinalMetadata);
            strExecutedNodes = JsonEncode(file.ExecutedNodes);
            sql = "update LibraryFile set " +
                  "Name = @1, DateCreated = @2, DateModified=@3, RelativePath=@4,Status=@5," +
                  "ProcessingOrder=@6,Fingerprint=@7,FinalFingerprint=@8,IsDirectory=@9,Flags=@10,OriginalSize=@11,FinalSize=@12," +
                  "CreationTime=@13,LastWriteTime=@14,HoldUntil=@15,ProcessingStarted=@16,ProcessingEnded=@17,LibraryUid=@18," +
                  "LibraryName=@19,FlowUid=@20,FlowName=@21,DuplicateUid=@22,DuplicateName=@23,NodeUid=@24,NodeName=@25,WorkerUid=@26," +
                  "OutputPath=@27,NoLongerExistsAfterProcessing=@28,OriginalMetadata=@29,FinalMetadata=@30,ExecutedNodes=@31 " +
                  "where Uid = @0";
        }
        else
        {
            if(file.Uid == Guid.Empty)
                file.Uid = Guid.NewGuid();;
            
            strOriginalMetadata = string.Empty;
            strFinalMetadata = string.Empty;
            strExecutedNodes = string.Empty;
            sql = "insert into LibraryFile(Uid,Name,DateCreated,DateModified,RelativePath,Status," +
                  "ProcessingOrder,Fingerprint,FinalFingerprint,IsDirectory,Flags,OriginalSize,FinalSize," +
                  "CreationTime,LastWriteTime,HoldUntil,ProcessingStarted,ProcessingEnded,LibraryUid," +
                  "LibraryName,FlowUid,FlowName,DuplicateUid,DuplicateName,NodeUid,NodeName,WorkerUid," +
                  "OutputPath,NoLongerExistsAfterProcessing,OriginalMetadata,FinalMetadata,ExecutedNodes)" +
                  " values (@0,@1,@2,@3,@4,@5," +
                  "@6,@7,@8,@9,@10,@11,@12," +
                  "@13,@14,@15,@16,@17,@18," +
                  "@19,@20,@21,@22,@23,@24,@25,@26," +
                  "@27,@28,@29,@30,@31)";
        }

        try
        {
            await db.Db.ExecuteAsync(sql,
                file.Uid.ToString(), file.Name, file.DateCreated.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                file.DateModified.ToString("yyyy-MM-dd HH:mm:ss.fff"), file.RelativePath, (int)file.Status,
                file.Order, file.Fingerprint, file.FinalFingerprint, file.IsDirectory ? 1 : 0, (int)file.Flags,
                file.OriginalSize,
                file.FinalSize,

                file.CreationTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                file.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                file.HoldUntil.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                file.ProcessingStarted.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                file.ProcessingEnded.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                file.LibraryUid?.ToString() ?? string.Empty,

                file.LibraryName, file.FlowUid?.ToString() ?? string.Empty, file.FlowName,
                file.DuplicateUid?.ToString() ?? string.Empty, file.DuplicateName,
                file.NodeUid?.ToString() ?? string.Empty,
                file.NodeName, file.WorkerUid?.ToString() ?? string.Empty,

                file.OutputPath, file.NoLongerExistsAfterProcessing ? 1 : 0, strOriginalMetadata, strFinalMetadata,
                strExecutedNodes
            );
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog($"Error {(update ? "updating" :"adding")} library file: {ex.Message}");
            throw;
        }

        Database_Log(dt2,(update ? "update": "insert") + " object (actual)");
        Database_Log(dt, update ? "updated object" : "insert object");
    }

    /// <summary>
    /// JSON encodes an object for the database
    /// </summary>
    /// <param name="o">the object to encode</param>
    /// <returns>the JSON encoded object</returns>
    private static string JsonEncode(object? o)
    {
        if (o == null)
            return string.Empty;
        return JsonSerializer.Serialize(o, JsonOptions);
    }
}