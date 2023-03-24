using LibraryFileService = FileFlows.Server.Services.LibraryFileService;
using Microsoft.AspNetCore.Mvc;
using FileFlows.Shared.Models;
using FileFlows.Server.Helpers;
using System.Runtime.InteropServices;
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
    public IEnumerable<ProcessingNode> GetAll()
    {
        var nodes = new NodeService().GetAll()
            .OrderBy(x => x.Address == Globals.InternalNodeName ? 0 : 1)
            .ThenBy(x => x.Name);
        var internalNode = nodes.FirstOrDefault(x => x.Uid == Globals.InternalNodeUid);
        if(internalNode != null)
        {
            bool update = false;
            if (internalNode.Version != Globals.Version.ToString())
            {
                internalNode.Version = Globals.Version.ToString();
                update = true;
            }

            if (internalNode.OperatingSystem == Shared.OperatingSystemType.Unknown)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    internalNode.OperatingSystem = Shared.OperatingSystemType.Windows;
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    internalNode.OperatingSystem = Shared.OperatingSystemType.Mac;
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    internalNode.OperatingSystem = Shared.OperatingSystemType.Linux;
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
                    internalNode.OperatingSystem = Shared.OperatingSystemType.Linux;

                if (internalNode.OperatingSystem != Shared.OperatingSystemType.Unknown)
                    update = true;
            }
            if(update)
                new NodeService().Update(internalNode);
        }
#if (DEBUG)
        // set this to linux so we can test the full UI
        if (internalNode != null)
            internalNode.OperatingSystem = Shared.OperatingSystemType.Linux;
#endif
        return nodes.OrderBy(x => x.Name.ToLowerInvariant());
    }

    /// <summary>
    /// Get processing node
    /// </summary>
    /// <param name="uid">The UID of the processing node</param>
    /// <returns>The processing node instance</returns>
    [HttpGet("{uid}")]
    public ProcessingNode Get(Guid uid) => 
        new NodeService().GetByUid(uid);

    /// <summary>
    /// Saves a processing node
    /// </summary>
    /// <param name="node">The node to save</param>
    /// <returns>The saved instance</returns>
    [HttpPost]
    public ProcessingNode Save([FromBody] ProcessingNode node)
    {
        // see if we are updating the internal node
        if(node.Libraries?.Any() == true)
        {
            // remove any removed libraries and update any names
            // TODO: update LibraryController to use LibraryService
            var libraries = new LibraryService().GetAll().ToDictionary(x => x.Uid, x => x.Name);
            node.Libraries = node.Libraries.Where(x => libraries.ContainsKey(x.Uid)).Select(x => new Plugin.ObjectReference
            {
                Uid = x.Uid,
                Name = libraries[x.Uid],
                Type = typeof(Library).FullName
            }).DistinctBy(x => x.Uid).ToList();
        }

        if(node.Uid == Globals.InternalNodeUid)
        {
            var internalNode = GetAll().FirstOrDefault(x => x.Uid == Globals.InternalNodeUid);
            if(internalNode != null)
            {
                internalNode.Schedule = node.Schedule;
                internalNode.FlowRunners = node.FlowRunners;
                internalNode.Enabled = node.Enabled;
                internalNode.TempPath = node.TempPath;
                internalNode.DontChangeOwner = node.DontChangeOwner;
                internalNode.DontSetPermissions = node.DontSetPermissions;
                internalNode.Permissions = node.Permissions;
                internalNode.AllLibraries = node.AllLibraries;
                internalNode.MaxFileSizeMb = node.MaxFileSizeMb;
                if (string.IsNullOrWhiteSpace(node.PreExecuteScript))
                    internalNode.PreExecuteScript = null;
                else
                    internalNode.PreExecuteScript = node.PreExecuteScript;
                
                internalNode.Libraries = node.Libraries;
                CheckLicensedNodes(internalNode.Uid, internalNode.Enabled);
                //var processingNode = await Update(internalNode, checkDuplicateName: true, useCache:true);
                
                new NodeService().Update(internalNode);
                return internalNode;
            }
            else
            {
                // internal but doesnt exist
                node.Address = Globals.InternalNodeName;
                node.Name = Globals.InternalNodeName;
                node.AllLibraries = ProcessingLibraries.All;
                node.Mappings = null; // no mappings for internal
            }
        }
        //var result = await Update(node, checkDuplicateName: true, useCache:true);
        new NodeService().Update(node);
        CheckLicensedNodes(node.Uid, node.Enabled);
        // LibraryFileService.RefreshProcessingNodes();
        return node;
    }

    /// <summary>
    /// Delete processing nodes from the system
    /// </summary>
    /// <param name="model">A reference model containing UIDs to delete</param>
    /// <returns>an awaited task</returns>
    [HttpDelete]
    public async Task Delete([FromBody] ReferenceModel<Guid> model)
    {
        var internalNode =  this.GetAll()
            .FirstOrDefault(x => x.Address == Globals.InternalNodeName)?.Uid ?? Guid.Empty;
        if (model.Uids.Contains(internalNode))
            throw new Exception("ErrorMessages.CannotDeleteInternalNode");
        await new NodeService().Delete(model.Uids);
    }

    /// <summary>
    /// Set state of a processing node
    /// </summary>
    /// <param name="uid">The UID of the processing node</param>
    /// <param name="enable">Whether or not this node is enabled and will process files</param>
    /// <returns>an awaited task</returns>
    [HttpPut("state/{uid}")]
    public ProcessingNode SetState([FromRoute] Guid uid, [FromQuery] bool? enable)
    {
        var service = new NodeService();
        var node = service.GetByUid(uid);
        if (node == null)
            throw new Exception("Node not found.");
        if (enable != null && node.Enabled != enable.Value)
        {
            node.Enabled = enable.Value;
            service.Update(node);
        }
        CheckLicensedNodes(uid, enable == true);
        return node;
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

        var service = new NodeService();
        var node = await service.GetByAddressAsync(address);
        if (node == null)
            return node;

        if (string.IsNullOrEmpty(version) == false && node.Version != version)
        {
            node.Version = version;
            service.Update(node);
        }
        else
        {
            // this updates the "LastSeen"
            await UpdateLastSeen(node.Uid);
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
        var service = new NodeService();
        var data = service.GetAll();
        var existing = data.FirstOrDefault(x => x.Address.ToLowerInvariant() == address.ToLowerInvariant());
        if (existing != null)
        {
            existing.SignalrUrl = "flow";
            return existing;
        }
        var settings = await new SettingsController().Get();
        // doesnt exist, register a new node.
        var variables = new VariableService().GetAll();
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
        service.Update(node);
        node.SignalrUrl = "flow";
        CheckLicensedNodes(Guid.Empty, false);
        return node;
    }

    /// <summary>
    /// Ensure the user does not exceed their licensed node count
    /// </summary>
    /// <param name="nodeUid">optional UID of a node that should be checked first</param>
    /// <param name="enabled">optional status of the node state</param>
    private void CheckLicensedNodes(Guid nodeUid, bool enabled)
    {
        var licensedNodes = LicenseHelper.GetLicensedProcessingNodes();
        var nodes = GetAll();
        int current = 0;
        var service = new NodeService();
        foreach (var node in nodes.OrderBy(x => x.Uid == nodeUid ? 1 : 2).ThenBy(x => x.Name))
        {
            if (node.Uid == nodeUid)
                node.Enabled = enabled;
            
            if (node.Enabled)
            {
                if (current >= licensedNodes)
                {
                    node.Enabled = false;
                    service.Update(node);
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
    public ProcessingNode RegisterPost([FromBody] RegisterModel model)
    {
        if (string.IsNullOrWhiteSpace(model?.Address))
            throw new ArgumentNullException(nameof(model.Address));
        if (string.IsNullOrWhiteSpace(model?.TempPath))
            throw new ArgumentNullException(nameof(model.TempPath));

        var address = model.Address.ToLowerInvariant().Trim();
        var service = new NodeService();
        var data = service.GetAll();
        var existing = data.FirstOrDefault(x => x.Address.ToLowerInvariant() == address);
        if (existing != null)
        {
            if(existing.FlowRunners != model.FlowRunners || existing.TempPath != model.TempPath || existing.Enabled != model.Enabled)
            {
                existing.FlowRunners = model.FlowRunners;
                existing.TempPath = model.TempPath;
                existing.Enabled = model.Enabled;
                existing.OperatingSystem = model.OperatingSystem;
                existing.Version = model.Version;
                service.Update(existing);
            }
            existing.SignalrUrl = "flow";
            return existing;
        }
        // doesnt exist, register a new node.
        var variables = new VariableService().GetAll();

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
            Enabled = model.Enabled,
            FlowRunners = model.FlowRunners,
            TempPath = model.TempPath,
            OperatingSystem = model.OperatingSystem,
            Version = model.Version,
            Schedule = new string('1', 672),
            AllLibraries = ProcessingLibraries.All,
            Mappings = model.Mappings?.Select(x => new KeyValuePair<string, string>(x.Server, x.Local))?.ToList() ??
                       variables?.Select(x => new
                           KeyValuePair<string, string>(x.Value, "")
                       )?.ToList() ?? new()
        };
        service.Update(node);
        node.SignalrUrl = "flow";
        return node;
    }

    /// <summary>
    /// Updates the last seen to now for a node
    /// </summary>
    /// <param name="uid">The node to update</param>
    internal async Task UpdateLastSeen(Guid uid)
    {
        var service = new NodeService();
        var node = service.GetByUid(uid);
        node.LastSeen = DateTime.Now;
        await DbHelper.UpdateNodeLastSeen(uid);
    }
}

