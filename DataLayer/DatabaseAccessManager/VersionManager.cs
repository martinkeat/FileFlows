using System.Reflection;
using System.Text.Json;
using FileFlows.DataLayer.Converters;
using FileFlows.DataLayer.DatabaseConnectors;
using FileFlows.DataLayer.Helpers;
using FileFlows.DataLayer.Models;
using FileFlows.Plugin;
using FileFlows.Shared;
using FileFlows.Shared.Attributes;
using FileFlows.Shared.Json;
using FileFlows.Shared.Models;

namespace FileFlows.DataLayer;

/// <summary>
/// Manager for Version
/// </summary>
internal class VersionManager: BaseManager
{
    /// <summary>
    /// Initializes a new instance of the Version manager
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="dbType">the type of database</param>
    /// <param name="dbConnector">the database connector</param>
    public VersionManager(ILogger logger, DatabaseType dbType, IDatabaseConnector dbConnector) : base(logger, dbType, dbConnector)
    {
    }

    /// <summary>
    /// Get the version number
    /// </summary>
    /// <returns>a version number</returns>
    public async Task<Version?> Get()
    {
        try
        {
            using var db = await DbConnector.GetDb();
            var version = db.Db.ExecuteScalar<string?>($"select {Wrap("Version")} from {Wrap("FileFlows")}");
            if (string.IsNullOrEmpty(version))
                return null;
            if (Version.TryParse(version, out Version? v))
                return v;
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }
    
    /// <summary>
    /// Sets the version
    /// </summary>
    /// <param name="version">the version</param>
    public async Task Set(string version)
    {
        using var db = await DbConnector.GetDb();
        if ((await db.Db.ExecuteAsync($"update {Wrap("FileFlows")} set {Wrap("Version")} = @0", version)) >
            0)
            return;
        await db.Db.ExecuteAsync($"insert into {Wrap("FileFlows")}({Wrap("Version")}) values (@0)", version);
    }
}