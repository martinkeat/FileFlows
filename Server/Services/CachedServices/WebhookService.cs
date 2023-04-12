using FileFlows.ServerShared.Services;
using FileFlows.Shared.Models;
using HttpMethod = FileFlows.Shared.HttpMethod;

namespace FileFlows.Server.Services;

/// <summary>
/// Service for communicating with FileFlows server for Webhooks
/// </summary>
public class WebhookService : CachedService<Webhook>
{
    /// <summary>
    /// Webhooks do not need to update the configuration
    /// as they do not effect configuration on a Flow Runner
    /// </summary>
    public override bool IncrementsConfiguration => false;

    /// <summary>
    /// Finds a webhook by its method an the route
    /// </summary>
    /// <param name="method">the method</param>
    /// <param name="route">the route</param>
    /// <returns>a webhook if found, otherwise null</returns>
    public Webhook? FindWebhook(HttpMethod method, string route)
    {
        if (string.IsNullOrEmpty(route))
            return null;
        
        return this.Data.FirstOrDefault(x =>
            x.Method == method && string.Equals(x.Route, route, StringComparison.InvariantCulture));
    }
}