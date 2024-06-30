namespace FileFlows.Shared.Models;

/// <summary>
/// Definition for a report
/// </summary>
public class ReportDefinition : IUniqueObject<Guid>
{
    /// <summary>
    /// Gets or 
    /// </summary>
    public Guid Uid { get; set; }
    
    /// <summary>
    /// Gets or sets the name
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Gets or sets the description
    /// </summary>
    public string Description { get; set; }
    
    /// <summary>
    /// Gets or sets the icon
    /// </summary>
    public string Icon { get; set; }
    /// <summary>
    /// Gets if this report supports a period selection
    /// </summary>
    public bool PeriodSelection { get; set; }
    /// <summary>
    /// Gets the flow selection for this report
    /// </summary>
    public ReportSelection FlowSelection { get; set; }
    
    /// <summary>
    /// Gets the library selection for this report
    /// </summary>
    public ReportSelection LibrarySelection { get; set; }
    
    /// <summary>
    /// Gets or sets the fields in this template
    /// </summary>
    public List<TemplateField> Fields { get; set; }
}

/// <summary>
/// Report selection
/// </summary>
public enum ReportSelection
{
    /// <summary>
    /// None, do not show this selection on this report
    /// </summary>
    None,
    /// <summary>
    /// One of these must be selected
    /// </summary>
    One,
    /// <summary>
    /// Any of these can be selected
    /// </summary>
    Any,
    /// <summary>
    /// Any of these can be selected, but at least one MUST be selected
    /// </summary>
    AnyRequired
}