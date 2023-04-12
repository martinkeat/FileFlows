using FileFlows.Server.Helpers;
using FileFlows.Server.Services;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace FileFlows.Server.Controllers;

/// <summary>
/// Controller for webhooks
/// </summary>
[Route("/api/webhook")]
public class WebhookController : Controller
{
    /// <summary>
    /// Get all webhooks configured in the system
    /// </summary>
    /// <returns>A list of all configured webhooks</returns>
    [HttpGet]
    public IEnumerable<Webhook> GetAll()
    {
        if(LicenseHelper.IsLicensed() == false)
            return new List<Webhook>();
        return  new WebhookService().GetAll().OrderBy(x => x.Name.ToLowerInvariant());
    }

    /// <summary>
    /// Get webhook
    /// </summary>
    /// <param name="uid">The UID of the webhook to get</param>
    /// <returns>The webhook instance</returns>
    [HttpGet("{uid}")]
    public Webhook Get(Guid uid)
    {
        if(LicenseHelper.IsLicensed() == false)
            return null;
        return new WebhookService().GetByUid(uid);
    }

    /// <summary>
    /// Get a webhook by its name, case insensitive
    /// </summary>
    /// <param name="name">The name of the webhook</param>
    /// <returns>The webhook instance if found</returns>
    [HttpGet("name/{name}")]
    public Webhook? GetByName(string name)
    {
        if (LicenseHelper.IsLicensed() == false)
            return null;
        return new WebhookService().GetByName(name);
    }

    /// <summary>
    /// Saves a webhook
    /// </summary>
    /// <param name="webhook">The webhook to save</param>
    /// <returns>The saved instance</returns>
    [HttpPost]
    public Webhook Save([FromBody] Webhook webhook)
    {
        if(LicenseHelper.IsLicensed() == false)
            return null;
        new WebhookService().Update(webhook);
        return webhook;
    }

    /// <summary>
    /// Delete webhooks from the system
    /// </summary>
    /// <param name="model">A reference model containing UIDs to delete</param>
    /// <returns>an awaited task</returns>
    [HttpDelete]
    public Task Delete([FromBody] ReferenceModel<Guid> model)
    {
        if(LicenseHelper.IsLicensed() == false)
            return Task.CompletedTask;
        return new WebhookService().Delete(model.Uids);
    }
}