using System.ComponentModel.DataAnnotations;

namespace FileFlows.Shared.Models;

/// <summary>
/// Webhook for FileFlows
/// </summary>
public class Webhook: FileFlowObject
{
    /// <summary>
    /// Gets or sets the route of this webhook
    /// </summary>
    [Required]
    [RegularExpression(@"^[a-zA-Z0-9\-._+]+$")]
    public string Route { get; set; }
    
    /// <summary>
    /// Gets or sets the code to run for this webhook
    /// </summary>
    [Required]
    public string Code { get; set; }
    
    /// <summary>
    /// Gets or sets the HTTP Method
    /// </summary>
    public HttpMethod Method { get; set; }
}