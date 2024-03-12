using Microsoft.AspNetCore.Mvc;
using FileFlows.Shared.Models;
using FileFlows.Server.Helpers;
using FileFlows.Server.Services;
using FileFlows.ServerShared.Models;

namespace FileFlows.Server.Controllers;

/// <summary>
/// Processing node controller
/// </summary>
[Route("/api/node")]
public class NodeController : Controller
{
    /// <summary>
    /// Gets a list of all processing nodes in the system
    /// </summary>
    /// <returns>a list of processing node</returns>
    [HttpGet]
    public async Task<IEnumerable<ProcessingNode>> GetAll()
    {
        var service = ServiceLoader.Load<NodeService>();
        var nodes = (await service.GetAllAsync())
            .OrderBy(x => x.Address == Globals.InternalNodeName ? 0 : 1)
            .ThenBy(x => x.Name)
            .ToList();
        
#if (DEBUG)
        var internalNode = nodes.FirstOrDefault(x => x.Uid == Globals.InternalNodeUid);
        // set this to linux so we can test the full UI
        if (internalNode != null)
            internalNode.OperatingSystem = OperatingSystemType.Linux;
#endif

        foreach (var node in nodes)
        {
            if (node.Enabled == false)
                node.Status = ProcessingNodeStatus.Disabled;
            else if (TimeHelper.InSchedule(node.Schedule) == false)
                node.Status = ProcessingNodeStatus.OutOfSchedule;
            else if (node.Version != Globals.Version)
                node.Status = ProcessingNodeStatus.VersionMismatch;
            else if (node.LastSeen < DateTime.UtcNow.AddMinutes(-5))
                node.Status = ProcessingNodeStatus.Offline;
            else if (FlowRunnerService.Executors.Any(x => x.Value.NodeUid == node.Uid))
                node.Status = ProcessingNodeStatus.Processing;
            else
                node.Status = ProcessingNodeStatus.Idle;
        }
        
        return nodes.OrderBy(x => x.Name.ToLowerInvariant());
    }

    /// <summary>
    /// Gets an overview of the nodes
    /// </summary>
    /// <returns>the response</returns>
    [HttpGet("overview")]
    public async Task<IActionResult> Overview()
    {
        var data = (await GetAll()).OrderBy(x => x.Enabled ? 1 : 2).ThenBy(x => x.Uid == Globals.InternalNodeUid ? 1 : 2)
            .ThenBy(x => x.Name)
            .Select(x => new
            {
                Name = x.Uid == Globals.InternalNodeUid ? "Internal" : x.Name,
                Status = (int) x.Status,
                StatusText = $"Enums.{nameof(ProcessingNodeStatus)}.{x.Status}"
            });
        return Ok(data);
    }

    /// <summary>
    /// Get processing node
    /// </summary>
    /// <param name="uid">The UID of the processing node</param>
    /// <returns>The processing node instance</returns>
    [HttpGet("{uid}")]
    public Task<ProcessingNode?> Get(Guid uid) 
        => ServiceLoader.Load<NodeService>().GetByUidAsync(uid);

    /// <summary>
    /// Saves a processing node
    /// </summary>
    /// <param name="node">The node to save</param>
    /// <returns>The saved instance</returns>
    [HttpPost]
    public async Task<IActionResult> Save([FromBody] ProcessingNode node)
    {
        // see if we are updating the internal node
        var service = ServiceLoader.Load<NodeService>();
        if(node.Libraries?.Any() == true)
        {
            // remove any removed libraries and update any names
            var libraries = (await ServiceLoader.Load<LibraryService>().GetAllAsync()).ToDictionary(x => x.Uid, x => x.Name);
            node.Libraries = node.Libraries.Where(x => libraries.ContainsKey(x.Uid)).Select(x => new Plugin.ObjectReference
            {
                Uid = x.Uid,
                Name = libraries[x.Uid],
                Type = typeof(Library).FullName
            }).DistinctBy(x => x.Uid).ToList();
        }
        

        if(node.Uid == Globals.InternalNodeUid)
        {
            Logger.Instance.ILog("Updating internal processing node");
            var internalNode = (await GetAll()).FirstOrDefault(x => x.Uid == Globals.InternalNodeUid);
            if(internalNode != null)
            {
                internalNode.Schedule = node.Schedule;
                internalNode.FlowRunners = node.FlowRunners;
                internalNode.Enabled = node.Enabled;
                internalNode.Priority = node.Priority;
                internalNode.TempPath = node.TempPath;
                internalNode.DontChangeOwner = node.DontChangeOwner;
                internalNode.DontSetPermissions = node.DontSetPermissions;
                internalNode.Permissions = node.Permissions;
                internalNode.AllLibraries = node.AllLibraries;
                internalNode.MaxFileSizeMb = node.MaxFileSizeMb;
                internalNode.Variables = node.Variables ?? new();
                internalNode.ProcessFileCheckInterval = node.ProcessFileCheckInterval;
                if (string.IsNullOrWhiteSpace(node.PreExecuteScript))
                    internalNode.PreExecuteScript = null;
                else
                    internalNode.PreExecuteScript = node.PreExecuteScript;
                
                internalNode.Libraries = node.Libraries;
                internalNode = await service.Update(internalNode);
                CheckLicensedNodes(internalNode.Uid, internalNode.Enabled);
                
                await RevisionIncrement();
                return Ok(internalNode);
            }
            
            // internal but doesnt exist
            Logger.Instance.ILog("Internal processing node does not exist, creating.");
            node.Address = Globals.InternalNodeName;
            node.Name = Globals.InternalNodeName;
            node.AllLibraries = ProcessingLibraries.All;
            node.Mappings = null; // no mappings for internal
            node.Variables ??= new();
            node = await service.Update(node);
            CheckLicensedNodes(node.Uid, node.Enabled);
            await RevisionIncrement();
            return Ok(node);
        }
        else
        {
            Logger.Instance.ILog("Updating external processing node: " + node.Name);
            var existing = await service.GetByUidAsync(node.Uid);
            if (existing == null)
                return BadRequest("Node not found");
            node.Variables ??= new();
            node = await service.Update(node);
            Logger.Instance.ILog("Updated external processing node: " + node.Name);
            CheckLicensedNodes(node.Uid, node.Enabled);
            await RevisionIncrement();
            return Ok(node);
        }
    }
    
    /// <summary>
    /// Increments the configuration revision
    /// </summary>
    /// <returns>an awaited task</returns>
    private Task RevisionIncrement()
        => new SettingsService().RevisionIncrement();

    /// <summary>
    /// Delete processing nodes from the system
    /// </summary>
    /// <param name="model">A reference model containing UIDs to delete</param>
    /// <returns>an awaited task</returns>
    [HttpDelete]
    public async Task Delete([FromBody] ReferenceModel<Guid> model)
    {
        var internalNode =  (await GetAll())
            .FirstOrDefault(x => x.Address == Globals.InternalNodeName)?.Uid ?? Guid.Empty;
        if (model.Uids.Contains(internalNode))
            throw new Exception("ErrorMessages.CannotDeleteInternalNode");
        await ServiceLoader.Load<NodeService>().Delete(model.Uids);
    }

    /// <summary>
    /// Set state of a processing node
    /// </summary>
    /// <param name="uid">The UID of the processing node</param>
    /// <param name="enable">Whether or not this node is enabled and will process files</param>
    /// <returns>an awaited task</returns>
    [HttpPut("state/{uid}")]
    public async Task<IActionResult> SetState([FromRoute] Guid uid, [FromQuery] bool? enable)
    {
        var service = ServiceLoader.Load<NodeService>();
        var node = await service.GetByUidAsync(uid);
        if (node == null)
            return BadRequest("Node not found.");
        if (enable != null && node.Enabled != enable.Value)
        {
            node.Enabled = enable.Value;
            node = await service.Update(node);
        }
        CheckLicensedNodes(uid, enable == true);
        return Ok(node);
    }

    /// <summary>
    /// Get processing node by address
    /// </summary>
    /// <param name="address">The address</param>
    /// <param name="version">The version of the node</param>
    /// <returns>If found, the processing node</returns>
    [HttpGet("by-address/{address}")]
    public async Task<ProcessingNode> GetByAddress([FromRoute] string address, [FromQuery] string version)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentNullException(nameof(address));

        var service = ServiceLoader.Load<NodeService>();
        var node = await service.GetByAddressAsync(address);
        if (node == null)
            return node;

        if (string.IsNullOrEmpty(version) == false && node.Version != version)
        {
            node.Version = version;
            node = await service.Update(node);
        }
        else
        {
            // this updates the "LastSeen"
            await service.UpdateLastSeen(node.Uid);
        }

        node.SignalrUrl = "flow";
        return node;
    }

    /// <summary>
    /// Register a processing node.  If already registered will return existing instance
    /// </summary>
    /// <param name="address">The address of the processing node</param>
    /// <returns>The processing node instance</returns>
    [HttpGet("register")]
    public async Task<ProcessingNode> Register([FromQuery]string address)
    {
        if(string.IsNullOrWhiteSpace(address))
            throw new ArgumentNullException(nameof(address));

        address = address.Trim();
        var service = ServiceLoader.Load<NodeService>();
        var data = await service.GetAllAsync();
        var existing = data.FirstOrDefault(x => x.Address.ToLowerInvariant() == address.ToLowerInvariant());
        if (existing != null)
        {
            existing.SignalrUrl = "flow";
            return existing;
        }

        // doesnt exist, register a new node.
        var variables = await ServiceLoader.Load<VariableService>().GetAllAsync();
        bool isSystem = address == Globals.InternalNodeName;
        var node = new ProcessingNode
        {
            Name = address,
            Address = address,
            Enabled = isSystem, // default to disabled so they have to configure it first
            FlowRunners = 1,
            AllLibraries = ProcessingLibraries.All,
            Schedule = new string('1', 672),
            Mappings = isSystem
                ? null
                : variables.Select(x => new
                    KeyValuePair<string, string>(x.Value, string.Empty)
                ).ToList()
        };
        node = await service.Update(node);
        node.SignalrUrl = "flow";
        CheckLicensedNodes(Guid.Empty, false);
        return node;
    }

    /// <summary>
    /// Ensure the user does not exceed their licensed node count
    /// </summary>
    /// <param name="nodeUid">optional UID of a node that should be checked first</param>
    /// <param name="enabled">optional status of the node state</param>
    private async Task CheckLicensedNodes(Guid nodeUid, bool enabled)
    {
        var licensedNodes = LicenseHelper.GetLicensedProcessingNodes();
        var service = ServiceLoader.Load<NodeService>();
        var nodes = await service.GetAllAsync();
        int current = 0;
        foreach (var node in nodes.OrderBy(x => x.Uid == nodeUid ? 1 : 2).ThenBy(x => x.Name))
        {
            if (node.Uid == nodeUid && enabled != node.Enabled)
            {
                Logger.Instance.ILog($"Changing processing node '{node.Name}' state from '{node.Enabled}' to '{enabled}'");
                node.Enabled = enabled;
                var result = await service.Update(node);
                if (result.Failed(out string error))
                    Logger.Instance.ELog($"Failed updating node '{node.Name}': {error}");
            }

            if (node.Enabled)
            {
                if (current >= licensedNodes)
                {
                    node.Enabled = false;
                    service.Update(node);
                    Logger.Instance.ILog($"Disabled processing node '{node.Name}' due to license restriction");
                }
                else
                {
                    ++current;
                }
            }
        }
    }


    /// <summary>
    /// Register a processing node.  If already registered will return existing instance
    /// </summary>
    /// <param name="model">The register model containing information about the processing node being registered</param>
    /// <returns>The processing node instance</returns>
    [HttpPost("register")]
    public async Task<ProcessingNode> RegisterPost([FromBody] RegisterModel model)
    {
        if (string.IsNullOrWhiteSpace(model?.Address))
            throw new ArgumentNullException(nameof(model.Address));
        if (string.IsNullOrWhiteSpace(model?.TempPath))
            throw new ArgumentNullException(nameof(model.TempPath));

        var address = model.Address.ToLowerInvariant().Trim();
        var service = ServiceLoader.Load<NodeService>();
        var data = await service.GetAllAsync();
        var existing = data.FirstOrDefault(x => x.Address.ToLowerInvariant() == address);
        if (existing != null)
        {
            if(existing.Version != model.Version) // existing.TempPath != model.TempPath)
            {
                //existing.FlowRunners = model.FlowRunners;
                //existing.Enabled = model.Enabled;
                //existing.TempPath = model.TempPath;
                //existing.OperatingSystem = model.OperatingSystem;
                existing.Architecture = model.Architecture;
                existing.OperatingSystem = model.OperatingSystem;
                existing.Version = model.Version;
                existing = await service.Update(existing);
            }
            existing.SignalrUrl = "flow";
            return existing;
        }
        // doesnt exist, register a new node.
        var variables = await ServiceLoader.Load<VariableService>().GetAllAsync();

        if(model.Mappings?.Any() == true)
        {
            var ffmpegTool = variables.FirstOrDefault(x => x.Name.ToLower() == "ffmpeg");
            if (ffmpegTool != null)
            {
                // update ffmpeg with actual location
                var mapping = model.Mappings.FirstOrDefault(x => x.Server.ToLower() == "ffmpeg");
                if(mapping != null)
                {
                    mapping.Server = ffmpegTool.Value;
                }
            }
        }

        var node = new ProcessingNode
        {
            Name = address,
            Address = address,
            //Enabled = model.Enabled,
            //FlowRunners = model.FlowRunners,
            Enabled = false,
            FlowRunners = 1,
            TempPath = model.TempPath,
            OperatingSystem = model.OperatingSystem,
            Architecture = model.Architecture,
            Version = model.Version,
            Schedule = new string('1', 672),
            AllLibraries = ProcessingLibraries.All,
            Mappings = model.Mappings?.Select(x => new KeyValuePair<string, string>(x.Server, x.Local))?.ToList() ??
                       variables?.Select(x => new
                           KeyValuePair<string, string>(x.Value, "")
                       )?.ToList() ?? new()
        };
        node = await service.Update(node);
        node.SignalrUrl = "flow";
        return node;
    }
}

