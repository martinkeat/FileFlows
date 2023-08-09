using FileFlows.Shared;
using System.Dynamic;
using FileFlows.Shared.Models;

/// <summary>
/// A flow template
/// </summary>
class FlowTemplate
{
    /// <summary>
    /// Gets or sets the name of the template
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Gets or sets the group of this template
    /// </summary>
    public string Group { get; set; }

    /// <summary>
    /// Gets or sets the minimum version this template supports
    /// </summary>
    public Version? MinimumVersion { get; set; }

    /// <summary>
    /// Gets or sets the tags for this template
    /// </summary>
    public List<string> Tags { get; set; }
    
    /// <summary>
    /// Gets or sets the author of the template
    /// </summary>
    public string Author { get; set; }
    
    /// <summary>
    /// Gets or sets the description of the template
    /// </summary>
    public string Description { get; set; }
    
    /// <summary>
    /// Gets or sets the parts in this template
    /// </summary>
    public List<FlowTemplatePart> Parts { get; set; }
    
    /// <summary>
    /// Gets or sets the fields in this template
    /// </summary>
    public List<TemplateField> Fields { get; set; }
    
    /// <summary>
    /// Gets or sets if this template should be saved or go to the editor
    /// </summary>
    public bool Save { get; set; }
    
    /// <summary>
    /// Gets or sets the Flow Type
    /// </summary>
    public FlowType Type { get; set; }
    
    /// <summary>
    /// Gets or sets if this flow should have skip tree shaking applied after being used in a template
    /// </summary>
    public bool SkipTreeShaking { get; set; }
}

/// <summary>
/// A Part belonging to a flow template
/// </summary>
class FlowTemplatePart
{
    /// <summary>
    /// Gets or sets the node 
    /// </summary>
    public string Node { get; set; }
    
    /// <summary>
    /// Gets or sets the UID of the part
    /// </summary>
    public Guid Uid { get; set; }
    
    /// <summary>
    /// Gets or sets the model
    /// </summary>
    public ExpandoObject Model { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the part
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the number of outputs this part has
    /// </summary>
    public int? Outputs { get; set; }
    
    /// <summary>
    /// Gets or sets the x coordinates on the canvas of this part
    /// </summary>
    public int? xPos { get; set; }
    
    /// <summary>
    /// Gets or sets the y coordinates on the canvas of this part
    /// </summary>
    public int? yPos { get; set; }

    /// <summary>
    /// Gets or sets the output connections of this part
    /// </summary>
    public List<FlowTemplateConnection> Connections { get; set; }
}


/// <summary>
/// A connection to a flow part
/// </summary>
class FlowTemplateConnection
{
    /// <summary>
    /// Gets or sets the input nude index
    /// </summary>
    public int Input { get; set; }
    
    /// <summary>
    /// Gets or sets the output node index
    /// </summary>
    public int Output { get; set; }
    
    /// <summary>
    /// Gets or sets the node that contains the Input
    /// </summary>
    public Guid Node { get; set; }
}