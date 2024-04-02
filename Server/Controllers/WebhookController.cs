using System.Text.RegularExpressions;
using FileFlows.Plugin;
using FileFlows.Server.Authentication;
using FileFlows.Server.Helpers;
using FileFlows.Server.Services;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using HttpMethod = FileFlows.Shared.HttpMethod;

namespace FileFlows.Server.Controllers;

/// <summary>
/// Controller for webhooks
/// </summary>
[Route("/api/webhook")]
[FileFlowsAuthorize(UserRole.Admin)]
public class WebhookController : Controller
{
    /// <summary>
    /// Get all webhooks configured in the system
    /// </summary>
    /// <returns>A list of all configured webhooks</returns>
    [HttpGet]
    public async Task<IEnumerable<Webhook>> GetAll()
    {
        if(LicenseHelper.IsLicensed(LicenseFlags.Webhooks) == false)
            return new List<Webhook>();
        return (await new ScriptController().GetAllByType(ScriptType.Webhook))
            .Select(x => FromScript(x))
            .Where(x => x != null);
    }

    /// <summary>
    /// Gets a webhook from a script
    /// </summary>
    /// <param name="script">the script</param>
    /// <returns>the webhook</returns>
    private Webhook? FromScript(Script script)
    {
        if (string.IsNullOrWhiteSpace(script?.CommentBlock))
            return null;

        Webhook webhook = new()
        {
            CommentBlock = script.CommentBlock,
            Code = script.Code,
            Name = script.Name,
            Path = script.Path,
            Repository = script.Repository,
            Revision = script.Revision,
            Type = script.Type,
            Uid = script.Uid,
            LatestRevision = script.LatestRevision,
            UsedBy = script.UsedBy
        };

        if(webhook.Code.StartsWith("// path: "))
            webhook.Code = webhook.Code.Substring(webhook.Code.IndexOf('\n') + 1).Trim();

        CommentBlock cblock = new(webhook.CommentBlock);
        webhook.Code = webhook.Code.Replace(webhook.CommentBlock, string.Empty);

        webhook.Method = cblock.GetValue("method") == "POST" ? HttpMethod.Post : HttpMethod.Get;
        webhook.Route = cblock.GetValue("route");
        if (string.IsNullOrWhiteSpace(webhook.Route))
        {
            Logger.Instance.WLog("Webhook: No route found for webhook: " + webhook.Name);
            return null;
        }

        return webhook;
    }

    /// <summary>
    /// Get webhook
    /// </summary>
    /// <param name="name">The name of the webhook</param>
    /// <returns>The webhook instance</returns>
    [HttpGet("{name}")]
    public async Task<Webhook?> Get(string name)
    {
        if(LicenseHelper.IsLicensed(LicenseFlags.Webhooks) == false)
            return null;
        var script = await new ScriptController().Get(name, ScriptType.Webhook);
        return script != null ? FromScript(script) : null;
    }

    /// <summary>
    /// Saves a webhook
    /// </summary>
    /// <param name="webhook">The webhook to save</param>
    /// <returns>The saved instance</returns>
    [HttpPost]
    public async Task<Webhook> Save([FromBody] Webhook webhook)
    {
        if(LicenseHelper.IsLicensed(LicenseFlags.Webhooks) == false || string.IsNullOrWhiteSpace(webhook.Name))
            return null;
        var existing = await Get(webhook.Uid?.EmptyAsNull() ?? webhook.Name);
        CommentBlock comments;
        if (existing != null)
        {
            if (existing.HasChanged(webhook) == false)
                return existing;
            comments = new(existing.CommentBlock);
        }
        else
        {
            comments = new(webhook.CommentBlock);
        }
        comments.AddOrUpdate("name", webhook.Name);
        comments.AddOrUpdate("route", webhook.Route);
        comments.AddOrUpdate("method", webhook.Method.ToString().ToUpperInvariant());
        webhook.CommentBlock = comments.ToString();
        
        // string code = 
        
        // new WebhookService().Update(webhook);
        return webhook;
    }

    /// <summary>
    /// Delete webhooks from the system
    /// </summary>
    /// <param name="model">A reference model containing UIDs to delete</param>
    /// <returns>an awaited task</returns>
    [HttpDelete]
    public void Delete([FromBody] ReferenceModel<string> model)
    {
        if(LicenseHelper.IsLicensed(LicenseFlags.Webhooks) == false)
            return;
        new ScriptController().Delete(model, ScriptType.Webhook);
    }
}