using FileFlows.Server.Authentication;
using FileFlows.Server.Services;
using FileFlows.Server.Utils;
using FileFlows.ServerShared.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileFlows.Server.Controllers.RemoteControllers;

/// <summary>
/// Log controller
/// </summary>
[Route("/remote/log")]
[FileFlowsApiAuthorize]
[ApiExplorerSettings(IgnoreApi = true)]
public class LogController : Controller
{
    
    private readonly Dictionary<string, Guid> ClientUids = new (); 
        

    /// <summary>
    /// Logs a message to the server
    /// </summary>
    /// <param name="message">The log message to log</param>
    [HttpPost]
    public async Task Log([FromBody] LogServiceMessage message)
    {
        if (message == null)
            return;
        if (string.IsNullOrEmpty(message.NodeAddress))
            return;

        if(ClientUids.TryGetValue(message.NodeAddress.ToLower(), out Guid clientUid) == false)
        {
            var nodes = await new NodeService().GetAllAsync();
            foreach (var node in nodes)
            {
                if (string.Equals(node.Address, message.NodeAddress, StringComparison.CurrentCultureIgnoreCase))
                    clientUid = node.Uid;
                if (string.IsNullOrEmpty(node.Address) == false &&
                    ClientUids.ContainsKey(node.Address.ToLower()) == false)
                {
                    ClientUids.Add(node.Address.ToLower(), node.Uid);
                }
            }
        }

        if (clientUid == Guid.Empty)
        {
            Logger.Instance.ILog($"Failed to find client '{message.NodeAddress}', could not log message");
            return;
        }

        if (Logger.Instance.TryGetLogger(out DatabaseLogger logger))
        {
            await logger.Log(clientUid, message.Type, message.Arguments);
        }
    }

}