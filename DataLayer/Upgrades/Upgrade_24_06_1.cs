using System.Text.Json;
using System.Text.RegularExpressions;
using FileFlows.DataLayer.DatabaseConnectors;
using FileFlows.DataLayer.Models;
using FileFlows.Plugin;
using FileFlows.ScriptExecution;
using FileFlows.ServerShared.Helpers;
using FileFlows.Shared;
using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.DataLayer.Upgrades;

/// <summary>
/// Upgrades for 24.06.1
/// </summary>
public class Upgrade_24_06_1
{
    Regex rgxComments = new Regex(@"\/\*\*(?:.|[\r\n])*?\*\/");

    private static readonly Dictionary<string, Guid> ScriptUids = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Add MKV extension to files without an extension", new Guid("bf5d95be-8fb7-4dc2-91da-9efcdcd086db") },
        { "Delete Empty Folders", new Guid("5dfc086e-a1e8-4c55-b3a7-d81c1ef41c90") },
        { "SABnzbd - Pause Downloads", new Guid("086f8a37-5e0c-4ee9-a033-44153198e9bf") },
        { "Delete Rogue _UNPACK_ Directories", new Guid("fadc8bec-cb54-4207-8606-ffa1c35f08e2") },
        { "Nvidia - Encoder Check", new Guid("55de29ea-749d-4c64-88ca-497fdcd9a1be") },
        { "Gotify - Notify File Processed", new Guid("91d6f012-d885-43b4-abc9-121a62f1da1d") },
        { "Gotify - FileFlows Server Update Available", new Guid("dcc90331-a51d-4399-9cdf-eb0e74f48158") },
        { "Gotify - FileFlows Server Updating", new Guid("81b67ae5-fc97-44e9-a36d-619bd965af89") },
        { "Gotify - Notify File Processing Failed", new Guid("4cbf36fe-cb45-4325-8d58-d369ebe774bb") },
        { "Apprise - Notify File Processing Failed", new Guid("4d0d9e73-ba63-401b-8ce8-c35249fa53b5") },
        { "Apprise - FileFlows Server Update Available", new Guid("964cd7c4-a763-4be8-b5f4-f81568d67950") },
        { "Apprise - FileFlows Server Updating", new Guid("9b3451e1-a40c-4dcb-8640-e477f477686f") },
        { "Apprise - Notify File Processed", new Guid("16ab40ef-acab-48f1-ba82-281daba3d035") },
        { "Apprise", new Guid("3aef04da-13b1-494e-8ea4-075432d86bd7") },
        { "Gotify", new Guid("ef1e6fad-2313-4851-a11e-4156c489b04d") },
        { "Sonarr", new Guid("0f5836c0-d20b-4740-9824-f81b5200ec3d") },
        { "Language", new Guid("d5b3078e-9999-4fef-b2eb-26385a2183ff") },
        { "SABnzbd", new Guid("fa7c89e1-cd9e-48d7-9354-79ee7147ccb3") },
        { "FileFlows API", new Guid("92aeca79-6dce-410f-bf26-87b56871be0e") },
        { "Radarr", new Guid("88e66e7d-f835-4620-9616-9beaa4ee42dc") },
        { "Folder - Copy Folder", new Guid("8efa6316-8108-4c1a-8aeb-49bb1c9f8323") },
        { "File - Older Than", new Guid("fbdd7b81-5112-43eb-a371-6aaf9b42977d") },
        { "Video - Denoise Non-Linear Means", new Guid("b4ed740e-b851-4336-b412-a63ddc64ca0b") },
        { "Video - Has 5.1 Audio", new Guid("c87f9e27-6587-490b-a51e-f65e7542b891") },
        { "Video - VMAF", new Guid("deabeae0-5233-4a8a-8663-893386695abc") },
        { "Video - Upscale Anime (Video2x) [Docker]", new Guid("ebbea155-596c-4be9-93bf-24858f6b0765") },
        { "Video - Inject HDR10\u002B metadata", new Guid("594f7f1e-2a8e-4858-b83b-38d971bf48d1") },
        { "Video - Set Stream Titles", new Guid("2b38c39e-8536-4df8-9878-684e3f02fed8") },
        { "Video - Export All Subtitles", new Guid("fc30c661-4901-4474-8487-619a6257494b") },
        { "Video - FPS greater than", new Guid("d4a4b12e-b9d5-47d1-b9d6-06f359cb0b2d") },
        { "Video - Upscale Anime (Dandere2x) [Docker]", new Guid("a688f844-988f-4541-90a6-0fc189fea1c4") },
        { "Video - Has 2.0 Audio", new Guid("f9928be4-23e3-43d8-aa5f-6e0d5a8fc736") },
        { "Video - Downscale greater than 1080p", new Guid("62b01a65-b602-49cb-ac81-8583ead54ca7") },
        { "Video - Blocklist sonarr or radarr", new Guid("8eb58ddf-f355-4442-8101-d6fd81a1b927") },
        { "Video - Bitrate greater than", new Guid("c6c1a4b6-5f50-4b7d-81bd-e8d38af2b698") },
        { "Video - Devedse - InstallFfmpegBtbN", new Guid("8c0bbcf8-90c0-44a3-8195-5a6814864063") },
        { "Video - Resolution", new Guid("d5962d9f-6200-4441-a0af-1c47f5ac8101") },
        { "Video - Deblocking", new Guid("bd6992b2-b123-4acd-a054-f6029121a415") },
        { "Video - Devedse - InstallAbAv1", new Guid("e9d2cce8-938c-4849-b9c1-22157d95211f") },
        { "Video - Has Audio", new Guid("c57f5849-0df1-4882-b668-62145bda1304") },
        { "Video - Dynamic range", new Guid("d6d1bde2-0a1c-46d1-a9b2-a46d12eeffb7") },
        { "Video - Devedse - RunAbAv1", new Guid("4b2d95ff-0b20-4be2-b945-e6efd8099feb") },
        { "Video - Set Original Language from MovieDB", new Guid("3a3909c7-aded-45d7-8340-b27e76589b02") },
        { "Video - Delete Non-Original Language Audio", new Guid("d3d80753-4c85-4202-af33-ba73e585c771") },
        { "Video - Audio bitrate per channel", new Guid("f25d9fc6-adfa-4c53-bf03-cc8fd6a98e9c") },
        { "Video - Downscale greater than 720p", new Guid("9191f554-ecd5-4ef4-b83b-054f2878b09f") },
        { "7Zip - Compress to Zip", new Guid("e45d1199-8528-4031-ad52-66c4f0fb5f8e") },
        { "NVIDIA - Below Encoder Limit", new Guid("071ef14f-46db-4e21-b438-30ac56b37cc4") },
        { "Image - Convert (ImageMagick)", new Guid("61aa25f3-74ed-44f6-a097-89149aea62c3") },
        { "Radarr - Rename", new Guid("bd6f02c8-e650-4916-bcae-46f382d20388") },
        { "Radarr - Get Original Language", new Guid("3915f110-4b07-4e11-b7b9-50de3f5a1255") },
        { "Sonarr - Rename", new Guid("5ac44abd-cfe9-4a84-904b-9424908509de") },
        { "Sonarr - Get Original Language", new Guid("51cf3c4f-f4a3-45e2-a083-6629397aab90") },
    };
    
    /// <summary>
    /// Run the upgrade
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="dbType">the database type</param>
    /// <param name="connectionString">the database connection string</param>
    /// <returns>the upgrade result</returns>
    public Result<bool> Run(ILogger logger, DatabaseType dbType, string connectionString)
    {
        var connector = DatabaseConnectorLoader.LoadConnector(logger, dbType, connectionString);
        using var db = connector.GetDb(true).Result;

        ImportScripts(logger, connector, db);
        return true;
    }

    /// <summary>
    /// Imports the script
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="connector">the DB connector</param>
    /// <param name="db">the DB connection</param>
    private void ImportScripts(ILogger logger, IDatabaseConnector connector, DatabaseConnection db)
    {
        Dictionary<string, Guid> systemScripts = new();
        Dictionary<string, Guid> flowScripts = new();
        foreach (var type in new[] { ScriptType.Flow, ScriptType.System, ScriptType.Shared })
        {
            var scripts = GetAllScriptsInPath(logger, type);
            foreach (Script script in scripts)
            {
                var dbo = FileFlowsObjectManager.ConvertToDbObject(script);
                dbo.DateModified = DateTime.UtcNow;
                dbo.DateCreated = DateTime.UtcNow;
                if (ScriptUids.TryGetValue(script.Name, out var uid))
                    dbo.Uid = uid;
                else
                    dbo.Uid = Guid.NewGuid();
                
                if (type == ScriptType.System)
                    systemScripts[script.Name] = dbo.Uid;
                else if (type == ScriptType.Flow)
                    flowScripts[script.Name] = dbo.Uid;
                
                db.Db.Insert(dbo);
            }
        }

        UpdateNodes(logger, connector, db, systemScripts);
        UpdateTasks(logger, connector, db, systemScripts);
        UpdateFlows(logger, connector, db, flowScripts);
    }

    /// <summary>
    /// Updates the flows to change from script:name to script:uid
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="connector">the connector</param>
    /// <param name="db">the database</param>
    /// <param name="flowScripts">the mapped system scripts</param>
    private void UpdateFlows(ILogger logger, IDatabaseConnector connector, DatabaseConnection db, Dictionary<string, Guid> flowScripts)
    {
        var dboObjects = db.Db.Fetch<DbObject>($"where {connector.WrapFieldName("Type")} = 'FileFlows.Shared.Models.Flow'");
        foreach (var dbo in dboObjects)
        {
            var original = dbo.Data;
            foreach (var script in flowScripts)
            {
                var jsonName = JsonSerializer.Serialize($"Script:{script.Key}");
                var jsonValue = JsonSerializer.Serialize($"Script:{script.Value}");
                dbo.Data = dbo.Data.Replace(jsonName, jsonValue);
            }

            if (original == dbo.Data)
                continue;
            db.Db.Update(dbo);
        }
    }

    /// <summary>
    /// Updates the nodes, and set the pre-execute script
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="connector">the connector</param>
    /// <param name="db">the database</param>
    /// <param name="systemScripts">the mapped system scripts</param>
    private void UpdateNodes(ILogger logger, IDatabaseConnector connector, DatabaseConnection db, Dictionary<string, Guid> systemScripts)
    {
        var dboObjects = db.Db.Fetch<DbObject>($"where {connector.WrapFieldName("Type")} = 'FileFlows.Shared.Models.ProcessingNode'");
        foreach (var dbo in dboObjects)
        {
            var actual = FileFlowsObjectManager.Convert<ProcessingNode>(dbo);
            if (actual == null)
                continue;
            // update any script references
            if (string.IsNullOrWhiteSpace(actual.PreExecuteScript) == false && systemScripts.TryGetValue(actual.PreExecuteScript, out var script))
            {
                logger.ILog($"Updating pre-execute script reference on node '{actual.Name}': '{actual.PreExecuteScript}' to '{script}");
                actual.PreExecuteScript = script.ToString();
            }
            else
            {
                actual.PreExecuteScript = null; // make this null so it can be converted to a Guid? otherwise "" will throw a conversion error
            }
                
            var converted = FileFlowsObjectManager.ConvertToDbObject(actual);
            dbo.Data = converted.Data;
            db.Db.Update(dbo);
        }
    }

    /// <summary>
    /// Updates the tasks and sets the script
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="connector">the connector</param>
    /// <param name="db">the database</param>
    /// <param name="systemScripts">the mapped system scripts</param>
    private void UpdateTasks(ILogger logger, IDatabaseConnector connector, DatabaseConnection db, Dictionary<string, Guid> systemScripts)
    {
        var dboObjects = db.Db.Fetch<DbObject>($"where {connector.WrapFieldName("Type")} = 'FileFlows.Shared.Models.FileFlowsTask'");
        foreach (var dbo in dboObjects)
        {
            var actual = FileFlowsObjectManager.Convert<FileFlowsTask>(dbo);
            // update any script references
            if (actual != null && systemScripts.TryGetValue(actual.Script!, out var script))
            {
                logger.ILog($"Updating script reference in task '{actual.Name}': '{actual.Script}' to '{script}");
                actual.Script = script.ToString();
                var converted = FileFlowsObjectManager.ConvertToDbObject(actual);
                dbo.Data = converted.Data;
                db.Db.Update(dbo);
            }
        }
    }

    /// <summary>
    /// Gets all scripts by type
    /// </summary>
    /// <param name="logger">the logger to use</param>
    /// <param name="type">the type of scripts to get</param>
    /// <returns>a list of all scripts of the given type</returns>
    public List<Script> GetAllScriptsInPath(ILogger logger, ScriptType type)
    {
        List<Script> scripts = new();
        string dir = Path.Combine(DirectoryHelper.DataDirectory, "Scripts", type.ToString());
        if(Directory.Exists(dir) == false)
            return scripts;
        try
        {
            foreach (var file in new DirectoryInfo(dir).GetFiles("*.js", SearchOption.AllDirectories))
            {
                var script = GetScript(file, type);
                if (script.Failed(out string error))
                {
                    logger.WLog($"Failed to parse script '{file}': {error}");
                    continue;
                }
                scripts.Add(script.Value);
            }

            return scripts;
        }
        catch (Exception ex)
        {
            logger.WLog($"Error getting scripts '{type}']: {ex.Message}");
            return scripts;
        }
    }
    
    
    /// <summary>
    /// Gets the script
    /// </summary>
    /// <param name="file">the file to get the script from</param>
    /// <param name="type">the type of script this is</param>
    /// <returns>the script</returns>
    private Result<Script> GetScript(FileInfo file, ScriptType type)
    {
        var name = file.Name.Replace(".js", "");
        var code = File.ReadAllText(file.FullName);
        bool repository = code.StartsWith("// path:");
        string? path = null;
        if (repository)
        {
            var lines = code.Replace("\r\n", "\n").Split('\n');
            path = lines.First()["// path:".Length..].Trim();
            code = string.Join('\n', lines.Skip(1)).Trim();
        }

        var result = new ScriptParser().Parse(name, code, type);
        if (result.IsFailed)
        {
            // basic script
            return new Script()
            {
                Name = name,
                Type = type,
                Code = code
            };
        }

        var model = result.Value!;
        
        return new Script
        {
            Name = name,
            Repository = repository,
            Type = type,
            Path = path,
            Revision = model.Revision,
            Description = model.Description,
            Code = model.Code,
            MinimumVersion = model.MinimumVersion,
            Parameters = model.Parameters,
            Author = model.Author,
            Outputs = model.Outputs
        };
    }

    public class ProcessingNode : FileFlowObject
    {
        /// <summary>
        /// Gets or sets the temporary path used by this node
        /// </summary>
        public string? TempPath { get; set; }

        /// <summary>
        /// Gets or sets the address this node is located at, hostname or ip address
        /// </summary>
        public string? Address { get; set; }

        /// <summary>
        /// Gets or sets when the node was last seen
        /// </summary>
        public DateTime LastSeen { get; set; }

        /// <summary>
        /// Gets or sets if this node is enabled
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the number of seconds to check for a new file to process
        /// </summary>
        public int ProcessFileCheckInterval { get; set; }

        /// <summary>
        /// Gets or sets the priority of the processing node
        /// Higher the value, the higher the priority 
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Gets or sets the type of operating system this node is running on
        /// </summary>
        public OperatingSystemType OperatingSystem { get; set; }

        /// <summary>
        /// Gets or sets the architecture type
        /// </summary>
        public ArchitectureType Architecture { get; set; }

        /// <summary>
        /// Gets or sets the FileFlows version of this node
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// Gets or sets a script to execute before a runner can start
        /// </summary>
        public string? PreExecuteScript { get; set; }

        /// <summary>
        /// Gets or sets the number of flow runners this node can run simultaneously 
        /// </summary>
        public int FlowRunners { get; set; }

        /// <summary>
        /// Gets or sets the SignalrUrl this node uses
        /// </summary>
        public string? SignalrUrl { get; set; }

        /// <summary>
        /// Gets or sets the mappings for this node
        /// </summary>
        public List<KeyValuePair<string, string>>? Mappings { get; set; }

        /// <summary>
        /// Gets or sets the variables for this node
        /// </summary>
        public List<KeyValuePair<string, string>>? Variables { get; set; }

        /// <summary>
        /// Gets or sets the schedule for this node
        /// </summary>
        public string? Schedule { get; set; }

        /// <summary>
        /// Gets or sets if the owner should not be changed
        /// </summary>
        public bool DontChangeOwner { get; set; }

        /// <summary>
        /// Gets or sets if permissions should not be set
        /// </summary>
        public bool DontSetPermissions { get; set; }

        /// <summary>
        /// Gets or sets the permissions to set
        /// </summary>
        public string? Permissions { get; set; }

        /// <summary>
        /// Gets or sets the permissions to set for folders
        /// </summary>
        public int? PermissionsFolders { get; set; }

        /// <summary>
        /// Gets or sets if this node can process all libraries
        /// </summary>
        public ProcessingLibraries AllLibraries { get; set; }

        /// <summary>
        /// Gets or sets the libraries this node can process
        /// </summary>
        public List<ObjectReference>? Libraries { get; set; }

        /// <summary>
        /// Gets or sets the maximum file size this node can process
        /// </summary>
        public int MaxFileSizeMb { get; set; }
    }

    /// <summary>
    /// A task that runs at a configured schedule
    /// </summary>
    public class FileFlowsTask : FileFlowObject
    {
        /// <summary>
        /// Gets or sets the script this task will execute
        /// </summary>
        public string? Script { get; set; }

        /// <summary>
        /// Gets or sets the type of task
        /// </summary>
        public TaskType Type { get; set; }

        /// <summary>
        /// Gets or sets the schedule this script runs at
        /// </summary>
        public string? Schedule { get; set; }
    
        /// <summary>
        /// Gets or sets when the task was last run
        /// </summary>
        public DateTime LastRun { get; set; }

        /// <summary>
        /// Gets or sets the recent run history
        /// </summary>
        public Queue<FileFlowsTaskRun> RunHistory { get; set; } = new Queue<FileFlowsTaskRun>(10);
    }

}